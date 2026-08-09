// Copyright (C) 2018-2023, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using LibReplanetizer;
using LibReplanetizer.LevelObjects;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using LibReplanetizer.Models;
using Replanetizer.Renderer;
using Replanetizer.Utils;
using LibReplanetizer.Models.Animations;

namespace Replanetizer.Renderer
{

    /*
     * A container to store IBO and VBO references for a Model
     */
    public class AnimationRenderer : Renderer
    {
        private static readonly int ALLOCATED_LIGHTS = 20;

        private Moby? mob;
        private MobyModel? mobyModelStandalone;

        private int loadedModelID = -1;
        private MobyModel? loadedModel;
        private bool loadedModelHasMeshData = false;

        private bool emptyModel = true;


        private int light { get; set; }
        private Rgb24 ambient { get; set; }
        private float renderDistance { get; set; }
        private static Vector4 SELECTED_COLOR = new Vector4(2.0f, 2.0f, 2.0f, 1.0f);

        private bool selected;
        private float blendDistance = 0.0f;

        private List<Texture> textures;
        private Dictionary<Texture, GLTexture> textureIds;
        private ShaderTable shaderTable;

        // The Ratchet moby does not contain its own animations.
        private List<Animation>? ratchetAnimations = null;

        private int currentFrameID = 0;
        private int currentAnimationID = 0;
        private Frame? currentFrame = null;
        private Frame? previousFrame = null;
        private float frameBlend = 0.0f;
        private readonly ModelGPUDataCache gpuDataCache;
        private ModelGPUData? gpuData;

        public AnimationRenderer(ShaderTable shaderTable, List<Texture> textures, Dictionary<Texture, GLTexture> textureIds, List<Animation>? ratchetAnimations = null, ModelGPUDataCache? gpuDataCache = null)
        {
            this.shaderTable = shaderTable;
            this.textureIds = textureIds;
            this.textures = textures;
            this.ratchetAnimations = ratchetAnimations;
            this.gpuDataCache = gpuDataCache ?? new ModelGPUDataCache();
        }

        public void ChangeTextures(List<Texture> textures, Dictionary<Texture, GLTexture>? textureIds = null)
        {
            this.textures = textures;

            if (textureIds != null)
            {
                this.textureIds = textureIds;
            }
        }

        public override void Include<T>(T obj)
        {
            mob = null;
            mobyModelStandalone = null;

            if (obj is Moby moby)
            {
                mob = moby;
                UpdateVars();
                return;
            }

            if (obj is MobyModel mobyModel)
            {
                mobyModelStandalone = mobyModel;
                UpdateVars();
                return;
            }

            throw new NotImplementedException();
        }

        public override void Include<T>(List<T> list) => throw new NotImplementedException();

        private void DeleteBuffers()
        {
            gpuDataCache.Release(gpuData);
            gpuData = null;
            loadedModelID = -1;
            loadedModel = null;
            loadedModelHasMeshData = false;
            emptyModel = true;
        }

        private static bool HasAnimationMeshData(MobyModel? model)
        {
            return model != null && model.GetIndices().Length > 0 && model.boneCount > 0;
        }

        public bool IsValid()
        {
            UpdateVars();
            return (emptyModel) ? false : true;
        }

        /// <summary>
        /// Generates IBO and VBO.
        /// </summary>
        private void GenerateBuffers()
        {
            DeleteBuffers();

            if (mob == null && mobyModelStandalone == null)
            {
                emptyModel = true;
                return;
            }

            MobyModel? mobyModel = (MobyModel?) mob?.model ?? mobyModelStandalone;

            if (mob != null)
            {
                loadedModelID = mob.modelID;
            }
            else if (mobyModelStandalone != null)
            {
                loadedModelID = mobyModelStandalone.id;
            }

            if (mobyModel == null)
            {
                return;
            }

            loadedModel = mobyModel;
            loadedModelHasMeshData = HasAnimationMeshData(mobyModel);

            if (!loadedModelHasMeshData)
                return;

            // This is a camera object that only exist at runtime and blocks vision in interactive mode.
            // We simply don't draw it.
            if (loadedModelID == 0x3EF)
            {
                loadedModelID = -1;
                loadedModel = null;
                loadedModelHasMeshData = false;
                emptyModel = true;
                mob = null;
                return;
            }

            emptyModel = false;

            gpuData = gpuDataCache.Acquire(mobyModel, ModelGPULayout.Animated);
        }

        /// <summary>
        /// Updates the light and ambient variables which can then be used to update the shader. Check if the modelID
        /// has changed and update the buffers if necessary.
        /// </summary>
        private void UpdateVars()
        {
            int modelID = -1;

            if (mob != null)
            {
                light = Math.Max(0, Math.Min(ALLOCATED_LIGHTS, mob.light)); ;
                ambient = mob.color;
                renderDistance = (mob.drawDistance > 0.0f) ? mob.drawDistance : float.MaxValue;
                modelID = mob.modelID;
            }
            else if (mobyModelStandalone != null)
            {
                light = -1;
                ambient = Color.FromRgb(255, 255, 255).ToPixel<Rgb24>();
                modelID = mobyModelStandalone.id;
            }

            MobyModel? currentModel = (MobyModel?) mob?.model ?? mobyModelStandalone;
            bool currentModelHasMeshData = HasAnimationMeshData(currentModel);

            if (loadedModelID != modelID
                || loadedModel != currentModel
                || loadedModelHasMeshData != currentModelHasMeshData)
            {
                previousFrame = null;
                GenerateBuffers();
            }
        }

        /// <summary>
        /// Takes a textureConfig mode as input and sets the transparency mode based on that.
        /// </summary>
        private void SetTransparencyMode(TextureConfig config)
        {
            shaderTable.animationShader.SetUniform1(UniformName.useTransparency, (config.IgnoresTransparency()) ? 0 : 1);
        }

        private void SetTextureMode(TextureConfig config)
        {
            shaderTable.animationShader.SetUniform1(UniformName.useTexture, (config.id >= 0) ? 1 : 0);
        }

        /// <summary>
        /// Takes a textureConfig as input and sets the texture wrap modes based on that.
        /// </summary>
        private void SetTextureWrapMode(TextureConfig conf, GLTexture tex)
        {
            /*
             * There is an issue with opaque edges in some transparent objects
             * This can easily be observed on RaC 1 Kerwan where you have these ugly edges on some trees and the bottom
             * of the fading out buildings.
             */

            TextureWrapMode wrapS, wrapT;

            switch (conf.wrapModeS)
            {
                case TextureConfig.WrapMode.ClampEdge:
                    wrapS = TextureWrapMode.ClampToEdge;
                    break;
                case TextureConfig.WrapMode.Repeat:
                default:
                    wrapS = TextureWrapMode.Repeat;
                    break;
            }

            switch (conf.wrapModeT)
            {
                case TextureConfig.WrapMode.ClampEdge:
                    wrapT = TextureWrapMode.ClampToEdge;
                    break;
                case TextureConfig.WrapMode.Repeat:
                default:
                    wrapT = TextureWrapMode.Repeat;
                    break;
            }

            tex.SetWrapModes(wrapS, wrapT);
        }


        /// <summary>
        /// Sets an internal variable to true if the corresponding modelObject is equal to
        /// the selectedObject in which case an outline will be rendered.
        /// </summary>
        private void Select(LevelObject selectedObject)
        {
            selected = mob == selectedObject;
        }

        /// <summary>
        /// Sets an internal variable to true if the corresponding modelObject is a member
        /// of selectedObjects in which case an outline will be rendered.
        /// </summary>
        private void Select(ICollection<LevelObject> selectedObjects)
        {
            if (mob == null) return;

            selected = selectedObjects.Contains(mob);
        }

        /// <summary>
        /// Returns true if the object is to be culled.
        /// Mobies and shrubs are culled by their drawDistance.
        /// Ties, terrain and shrubs are culled by frustum culling.
        /// </summary>
        private bool ComputeCulling(Camera camera, bool distanceCulling, bool visibleCulling)
        {
            if (emptyModel) return true;
            if (mobyModelStandalone != null) return false;
            if (mob == null) return true;

            if (visibleCulling && mob.memory != null)
            {
                if (mob.memory.visible == 0)
                    return true;
            }

            if (distanceCulling)
            {
                float dist = (mob.position - camera.position).Length;

                float blendScale = 32.0f;

                blendDistance = MathF.Max((dist - renderDistance) / blendScale, 0.0f);

                if (dist > renderDistance + blendScale)
                {
                    return true;
                }
            }
            else
            {
                blendDistance = 0.0f;
            }

            return false;
        }

        private void RenderModel(Model model, int modelVAO)
        {
            GL.BindVertexArray(modelVAO);

            //Bind textures one by one, applying it to the relevant vertices based on the index array
            foreach (TextureConfig conf in model.textureConfig)
            {
                if (conf.id >= 0 && conf.id < textures.Count)
                {
                    GLTexture tex = textureIds[textures[conf.id]];
                    tex.Bind();
                    SetTextureWrapMode(conf, tex);
                }
                else
                {
                    GLTexture.BindNull();
                }

                SetTransparencyMode(conf);
                SetTextureMode(conf);
                GL.DrawElements(PrimitiveType.Triangles, conf.size, DrawElementsType.UnsignedShort, conf.start * sizeof(ushort));
            }
        }

        public override void Render(RendererPayload payload)
        {
            if ((mob == null || mob.model == null || mob.memory == null) && mobyModelStandalone == null) return;

            UpdateVars();

            if (emptyModel || gpuData == null) return;

            if (ComputeCulling(payload.camera, payload.visibility.enableDistanceCulling, payload.visibility.enableVisibleCulling)) return;

            MobyModel? mobyModel = (MobyModel?) mob?.model ?? mobyModelStandalone;

            if (mobyModel == null) return;

            Select(payload.selection);

            shaderTable.animationShader.UseShader();

            Matrix4 modelToWorld = (mob != null) ? mob.modelMatrix : Matrix4.Identity;
            Matrix4 worldToView = payload.camera.GetWorldViewMatrix();
            Matrix4 viewMatrix = payload.camera.GetViewMatrix();
            shaderTable.animationShader.SetUniformMatrix4(UniformName.modelToWorld, ref modelToWorld);
            shaderTable.animationShader.SetUniformMatrix4(UniformName.worldToView, ref worldToView);
            shaderTable.animationShader.SetUniformMatrix4(UniformName.viewMatrix, ref viewMatrix);
            shaderTable.animationShader.SetUniform1(UniformName.useMetalShading, 0);
            shaderTable.animationShader.SetUniform1(UniformName.mainTexture, 0);
            shaderTable.animationShader.SetUniform1(UniformName.blueNoiseTexture, 1);
            shaderTable.animationShader.SetUniform1(UniformName.ssaaLevelLog, FramebufferRenderer.SSAA_LEVEL_LOG);
            shaderTable.animationShader.SetUniform1(UniformName.levelObjectNumber, (mob != null) ? mob.globalID : 0);
            if (selected)
            {
                shaderTable.animationShader.SetUniform4(UniformName.staticColor, SELECTED_COLOR);
            }
            else
            {
                shaderTable.animationShader.SetUniform4(UniformName.staticColor, ambient);
            }
            shaderTable.animationShader.SetUniform1(UniformName.lightIndex, light);
            shaderTable.animationShader.SetUniform1(UniformName.objectBlendDistance, blendDistance);

            GLTexture.blueNoiseTexture.Bind(1);

            int animationID = (mob != null && mob.memory != null) ? mob.memory.animationID : payload.forcedAnimationID;

            if (animationID != currentAnimationID)
            {
                currentAnimationID = animationID;
                currentFrameID = 0;
                frameBlend = 0.0f;
            }

            Matrix4[] boneMatrices = new Matrix4[mobyModel.boneCount];

            List<Animation> animations = (loadedModelID == 0 && ratchetAnimations != null && ratchetAnimations.Count > 0) ? ratchetAnimations : mobyModel.animations;

            Animation? anim = (animationID >= 0 && animationID < animations.Count) ? animations[animationID] : null;

            int animationFrame = (mob != null && mob.memory != null) ? mob.memory.animationFrame : currentFrameID;

            // For Example: RaC 1 bomb glove idles in the last frame of the animation despite the first one being selected.
            // TODO: Understand what is happening in these cases.
            if (anim != null && mob != null && mob.memory != null)
            {
                animationFrame--;
                if (animationFrame < 0)
                {
                    animationFrame += anim.frames.Count;
                }
            }

            Frame? frame = (anim != null && animationFrame >= 0 && animationFrame < anim.frames.Count) ? anim.frames[animationFrame] : null;

            if (anim != null && frame != null)
            {
                float frameSpeed = (anim.speed != 0.0f) ? anim.speed : frame.speed;
                frameBlend += payload.deltaTime * frameSpeed * 60.0f;

                // If frameSpeed is zero then no interpolation is used and thus always display exactly the current frame.
                if (frameSpeed == 0.0f)
                {
                    frameBlend = 1.0f;
                }
            }

            if (frame != currentFrame)
            {
                frameBlend = 0.0f;
                previousFrame = (currentFrame != null) ? currentFrame : frame;
                currentFrame = frame;
            }

            if (frame != null && previousFrame != null)
            {
                float blend = frameBlend;

                if (blend > 1.0f) blend = 1.0f;
                if (blend < 0.0f) blend = 0.0f;

                for (int i = 0; i < mobyModel.boneCount; i++)
                {
                    Matrix4 animationMatrix = previousFrame.GetRotationMatrix(i, frame, blend);
                    Vector3? scaling = previousFrame.GetScaling(i, frame, blend);
                    Vector3? translation = previousFrame.GetTranslation(i, frame, blend);

                    // Translations replace the bone data translation
                    Vector3 translationVector = (translation != null) ? (Vector3) translation : mobyModel.boneDatas[i].translation;

                    animationMatrix.M41 = translationVector.X;
                    animationMatrix.M42 = translationVector.Y;
                    animationMatrix.M43 = translationVector.Z;

                    if (scaling != null)
                    {
                        Vector3 s = (Vector3) scaling;
                        animationMatrix.M11 *= s.X;
                        animationMatrix.M12 *= s.X;
                        animationMatrix.M13 *= s.X;
                        animationMatrix.M21 *= s.Y;
                        animationMatrix.M22 *= s.Y;
                        animationMatrix.M23 *= s.Y;
                        animationMatrix.M31 *= s.Z;
                        animationMatrix.M32 *= s.Z;
                        animationMatrix.M33 *= s.Z;
                    }

                    Matrix4 parentMatrix = (i == 0) ? Matrix4.Identity : boneMatrices[mobyModel.boneDatas[i].parent];

                    boneMatrices[i] = animationMatrix * parentMatrix;
                }

                for (int i = 0; i < mobyModel.boneCount; i++)
                {
                    boneMatrices[i] = mobyModel.boneMatrices[i].GetInvBindMatrix(true) * boneMatrices[i];
                }
            }
            else
            {
                // Animation is not present in Replanetizer (like in Cutscenes)

                for (int i = 0; i < mobyModel.boneCount; i++)
                {
                    boneMatrices[i] = Matrix4.Identity;
                }
            }

            // This causes the animation in the model viewer to loop.
            while (frameBlend >= 1.0f && mob == null && anim != null)
            {
                frameBlend -= 1.0f;
                currentFrameID++;
                if (currentFrameID >= anim.frames.Count)
                {
                    currentFrameID = 0;
                }
            }

            shaderTable.animationShader.SetUniformMatrix4(UniformName.bones, mobyModel.boneCount, ref boneMatrices[0].Row0.X);

            RenderModel(mobyModel, gpuData.vao);

            int subModelCount = mobyModel.GetSubModelCount();
            for (int i = 0; i < subModelCount; i++)
            {
                if ((payload.visibility.subModelsMask & (1u << i)) == 0) continue;

                Model? subModel = mobyModel.GetSubModel(i);
                ModelGPUData? modelGPUData = gpuData.subModels[i];

                if (subModel == null || modelGPUData == null) continue;

                RenderModel(subModel, modelGPUData.vao);
            }

            GLUtil.CheckGlError("AnimationRenderer");
        }

        public override void Dispose()
        {
            DeleteBuffers();
        }
    }
}
