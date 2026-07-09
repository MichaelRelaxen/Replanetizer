// Copyright (C) 2018-2021, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using LibReplanetizer.Headers;
using LibReplanetizer.LevelObjects;
using LibReplanetizer.Models;
using LibReplanetizer.Models.Animations;
using NLog.Time;
using System.Collections.Generic;
using System.IO;
using static LibReplanetizer.DataFunctions;
using static LibReplanetizer.Serializers.SerializerFunctions;

namespace LibReplanetizer.Serializers
{
    public class EngineSerializer
    {
        private string? enginePath;

        public void Save(Level level, string directory)
        {
            enginePath = Path.Join(directory, "engine.ps3");
            ReplanetizerFileStream fs = new ReplanetizerFileStream(enginePath, FileMode.Create, FileAccess.Write);

            switch (level.game.num)
            {
                case 1:
                    SaveRC1(level, fs);
                    break;
                case 2:
                case 3:
                    SaveRC23(level, fs);
                    break;
                case 4:
                    SaveRC4(level, fs);
                    break;
            }

            fs.Close();
        }

        private void SaveRC1(Level level, FileStream fs)
        {
            fs.Seek(0x90, SeekOrigin.Begin);

            EngineHeader engineHeader = new EngineHeader();

            engineHeader.game = level.game;
            engineHeader.uiElementPointer = SeekWrite(fs, WriteUiElements(level.uiElements, (int) fs.Position));
            engineHeader.skyboxPointer = SeekWrite(fs, level.skybox.Serialize((int) fs.Position));
            engineHeader.terrainPointer = SeekWrite(fs, WriteTfrags(level.terrainEngine, (int) fs.Position, level.game));
            engineHeader.renderDefPointer = SeekWrite(fs, level.renderDefBytes);
            engineHeader.collisionPointer = SeekWrite(fs, level.collisionEngine.Serialize());
            engineHeader.mobyModelPointer = SeekWrite(fs, WriteMobies(level.mobyModels, (int) fs.Position));
            engineHeader.playerAnimationPointer = SeekWrite(fs, WritePlayerAnimations(level.playerAnimations, (int) fs.Position));
            engineHeader.gadgetPointer = SeekWrite(fs, WriteWeapons(level.gadgetModels, (int) fs.Position));
            engineHeader.tieModelPointer = SeekWrite(fs, WriteTieModels(level.tieModels, (int) fs.Position));
            engineHeader.tiePointer = WriteTies(fs, level.ties);
            engineHeader.shrubModelPointer = SeekWrite(fs, WriteShrubModels(level.shrubModels, (int) fs.Position));
            engineHeader.shrubPointer = SeekWrite(fs, WriteShrubs(level.shrubs));
            engineHeader.textureConfigMenuPointer = SeekWrite(fs, WriteTextureConfigMenus(level.textureConfigMenus));
            engineHeader.texture2dPointer = SeekWrite(fs, level.billboardBytes);
            engineHeader.soundConfigPointer = SeekWrite(fs, level.soundConfigBytes);
            engineHeader.lightPointer = SeekWrite(fs, WriteLights(level.lights));
            engineHeader.lightConfigPointer = SeekWrite(fs, WriteLightConfig(level.lightConfig));
            engineHeader.texturePointer = SeekWrite(fs, WriteTextures(level.textures));
            // Counts
            engineHeader.tieModelCount = level.tieModels.Count;
            engineHeader.tieCount = level.ties.Count;
            engineHeader.shrubModelCount = level.shrubModels.Count;
            engineHeader.shrubCount = level.shrubs.Count;
            engineHeader.gadgetCount = level.gadgetModels.Count;
            engineHeader.textureCount = level.textures.Count;
            engineHeader.lightCount = level.lights.Count;
            engineHeader.textureConfigMenuCount = level.textureConfigMenus.Count;

            // Seek to the beginning and write the header now that we have all the pointers
            byte[] head = engineHeader.Serialize();
            fs.Seek(0, SeekOrigin.Begin);
            fs.Write(head, 0, head.Length);
        }

        private void SaveRC23(Level level, FileStream fs)
        {
            fs.Seek(0x90, SeekOrigin.Begin);

            EngineHeader engineHeader = new EngineHeader
            {
                game = level.game,
                uiElementPointer = SeekWrite(fs, WriteUiElements(level.uiElements, (int) fs.Position)),
                terrainPointer = SeekWrite(fs, WriteTfrags(level.terrainEngine, (int) fs.Position, level.game)),
                renderDefPointer = SeekWrite(fs, level.renderDefBytes),
                collisionPointer = SeekWrite(fs, level.collisionEngine.Serialize()),
                tieModelPointer = SeekWrite(fs, WriteTieModels(level.tieModels, (int) fs.Position)),
                tiePointer = WriteTies(fs, level.ties),
                shrubModelPointer = SeekWrite(fs, WriteShrubModels(level.shrubModels, (int) fs.Position)),
                shrubPointer = SeekWrite(fs, WriteShrubs(level.shrubs)),
                textureConfigMenuPointer = SeekWrite(fs, WriteTextureConfigMenus(level.textureConfigMenus)),
                texture2dPointer = SeekWrite(fs, level.billboardBytes),
                mobyModelPointer = SeekWrite(fs, WriteMobies(level.mobyModels, (int) fs.Position)),
                soundConfigPointer = SeekWrite(fs, level.soundConfigBytes),
                playerAnimationPointer = SeekWrite(fs, WritePlayerAnimations(level.playerAnimations, (int) fs.Position)),
                skyboxPointer = SeekWrite(fs, level.skybox.Serialize((int) fs.Position)),
                lightPointer = SeekWrite(fs, WriteLights(level.lights)),
                lightConfigPointer = SeekWrite(fs, WriteLightConfig(level.lightConfig)),
                texturePointer = SeekWrite(fs, WriteTextures(level.textures)),
                // Counts
                tieModelCount = level.tieModels.Count,
                tieCount = level.ties.Count,
                shrubModelCount = level.shrubModels.Count,
                shrubCount = level.shrubs.Count,
                textureCount = level.textures.Count,
                lightCount = level.lights.Count,
                textureConfigMenuCount = level.textureConfigMenus.Count,
            };

            // Seek to the beginning and write the header now that we have all the pointers
            byte[] head = engineHeader.Serialize();
            fs.Seek(0, SeekOrigin.Begin);
            fs.Write(head, 0, head.Length);
        }

        private void SaveRC4(Level level, FileStream fs)
        {
            fs.Seek(0xA0, SeekOrigin.Begin);

            EngineHeader engineHeader = new EngineHeader
            {
                game = level.game,
                uiElementPointer = SeekWrite(fs, WriteUiElements(level.uiElements, (int) fs.Position)),
                unk8Pointer = SeekWrite(fs, level.unk8),
                mobyModelPointer = SeekWrite(fs, WriteMobies(level.mobyModels, (int) fs.Position)),
                soundConfigPointer = SeekWrite(fs, level.soundConfigBytes),
                unk9Pointer = SeekWrite(fs, level.unk9),
                terrainPointer = SeekWrite(fs, WriteTfrags(level.terrainEngine, (int) fs.Position, level.game)),
                renderDefPointer = SeekWrite(fs, level.renderDefBytes),
                collisionPointer = SeekWrite(fs, level.collisionEngine.Serialize()),
                shrubModelPointer = SeekWrite(fs, WriteShrubModels(level.shrubModels, (int) fs.Position)),
                shrubPointer = SeekWrite(fs, WriteShrubs(level.shrubs)),
                unk5Pointer = SeekWrite(fs, level.unk5),
                tieModelPointer = SeekWrite(fs, WriteTieModels(level.tieModels, (int) fs.Position)),
                tiePointer = WriteTies(fs, level.ties),
                unk4Pointer = SeekWrite(fs, level.unk4),
                textureConfigMenuPointer = SeekWrite(fs, WriteTextureConfigMenus(level.textureConfigMenus)),
                texture2dPointer = SeekWrite(fs, level.billboardBytes),
                skyboxPointer = SeekWrite(fs, level.skybox.Serialize((int) fs.Position)),
                lightPointer = SeekWrite(fs, WriteLights(level.lights)),
                lightConfigPointer = SeekWrite(fs, WriteLightConfig(level.lightConfig)),
                unk3Pointer = SeekWrite(fs, level.unk3),
                texturePointer = SeekWrite(fs, WriteTextures(level.textures)),
                // Counts
                tieModelCount = level.tieModels.Count,
                tieCount = level.ties.Count,
                shrubModelCount = level.shrubModels.Count,
                shrubCount = level.shrubs.Count,
                textureCount = level.textures.Count,
                lightCount = level.lights.Count,
                textureConfigMenuCount = level.textureConfigMenus.Count,
            };

            // Seek to the beginning and write the header now that we have all the pointers
            byte[] head = engineHeader.Serialize();
            fs.Seek(0, SeekOrigin.Begin);
            fs.Write(head, 0, head.Length);
        }

        private byte[] WriteLightConfig(LightConfig config)
        {
            return config.Serialize();
        }

        private byte[] WriteUiElements(List<UiElement> uiElements, int fileOffset)
        {
            short offset = 0;
            var spriteIds = new List<int>();
            var elemBytes = new byte[uiElements.Count * 8];

            for (int i = 0; i < uiElements.Count; i++)
            {
                WriteShort(elemBytes, i * 8 + 0x00, uiElements[i].id);
                if (uiElements[i].id == -1) continue;
                WriteShort(elemBytes, i * 8 + 0x02, (short) uiElements[i].sprites.Count);
                WriteShort(elemBytes, i * 8 + 0x04, offset);

                spriteIds.AddRange(uiElements[i].sprites);

                offset += (short) uiElements[i].sprites.Count;
            }

            var spriteBytes = new byte[spriteIds.Count * 4];
            for (int i = 0; i < spriteIds.Count; i++)
            {
                WriteInt(spriteBytes, i * 4, spriteIds[i]);
            }

            int elemStart = fileOffset + 0x10;
            int spriteStart = GetLength(elemStart + elemBytes.Length);

            var headBytes = new byte[0x10];
            WriteShort(headBytes, 0x00, (short) uiElements.Count);
            WriteShort(headBytes, 0x02, (short) spriteIds.Count);
            WriteInt(headBytes, 0x04, elemStart);
            WriteInt(headBytes, 0x08, spriteStart);

            var outBytes = new byte[headBytes.Length + GetLength(elemBytes.Length) + GetLength(spriteBytes.Length)];
            headBytes.CopyTo(outBytes, 0);
            elemBytes.CopyTo(outBytes, 0x10);
            spriteBytes.CopyTo(outBytes, 0x10 + GetLength(elemBytes.Length));

            return outBytes;
        }

        private byte[] WriteMobies(List<Model> mobyModels, int offset)
        {
            var headBytes = new byte[mobyModels.Count * 0x08 + 0x04];
            var bodBytes = new List<byte>();

            offset = AlignAddressUp(offset);

            WriteInt(headBytes, 0x00, mobyModels.Count);
            offset += headBytes.Length;

            for (int i = 0; i < mobyModels.Count; i++)
            {
                int mobyIDOffset = 0x04 + i * 0x08;

                WriteShort(headBytes, mobyIDOffset + 0x02, mobyModels[i].id);

                MobyModel g = (MobyModel) mobyModels[i];
                if (!g.isModel)
                    continue;

                WriteInt(headBytes, mobyIDOffset + 0x04, offset);
                byte[] bodyByte = g.Serialize(offset);
                bodBytes.AddRange(bodyByte);
                offset += bodyByte.Length;
            }

            byte[] bytes = new byte[headBytes.Length + bodBytes.Count];

            headBytes.CopyTo(bytes, 0x00);
            bodBytes.CopyTo(bytes, headBytes.Length);

            return bytes;
        }


        private byte[] WriteWeapons(List<Model> weaponModels, int offset)
        {
            var headBytes = new byte[weaponModels.Count * 0x10];
            var bodyBytes = new List<byte>();

            int headLength = GetLength(headBytes.Length);
            offset += headLength;

            for (int i = 0; i < weaponModels.Count; i++)
            {
                WriteInt(headBytes, i * 0x10, weaponModels[i].id);

                MobyModel g = (MobyModel) weaponModels[i];
                if (!g.isModel)
                    continue;

                byte[] bodyByte = g.Serialize(offset);
                WriteInt(headBytes, i * 0x10 + 4, offset);
                WriteInt(headBytes, i * 0x10 + 8, bodyByte.Length);
                bodyBytes.AddRange(bodyByte);
                offset += bodyByte.Length;

            }

            var outBuff = new byte[headLength + bodyBytes.Count];
            headBytes.CopyTo(outBuff, 0);
            bodyBytes.CopyTo(outBuff, headLength);

            return outBuff;
        }

        private int WriteTies(FileStream fs, List<Tie> ties)
        {
            int headerSize = ties.Count * 0x70;

            byte[] headBytes = new byte[headerSize];

            int headerOffset = SeekWrite(fs, headBytes);

            for (int i = 0; i < ties.Count; i++)
            {
                byte[] colByte = ties[i].colorBytes;

                int colorBytesOffset = SeekWrite(fs, colByte, 0x80);

                ties[i].ToByteArray(colorBytesOffset).CopyTo(headBytes, i * 0x70);
            }

            WriteBytesAtOffset(fs, headBytes, headerOffset);

            return headerOffset;
        }

        private byte[] WriteShrubs(List<Shrub> shrubs)
        {
            var outBytes = new byte[shrubs.Count * 0x70];
            for (int i = 0; i < shrubs.Count; i++)
            {
                shrubs[i].ToByteArray().CopyTo(outBytes, i * 0x70);
            }

            return outBytes;
        }

        private byte[] WriteLights(List<Light> lights)
        {
            var outBytes = new byte[lights.Count * 0x40];
            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].Serialize().CopyTo(outBytes, i * 0x40);
            }

            return outBytes;
        }

        private byte[] WriteTextureConfigMenus(List<int> textureConfigMenus)
        {
            var outBytes = new byte[textureConfigMenus.Count * 0x4];
            for (int i = 0; i < textureConfigMenus.Count; i++)
            {
                WriteInt(outBytes, i * 4, textureConfigMenus[i]);
            }

            return outBytes;
        }

        private byte[] WriteTieModels(List<Model> tiemodels, int offset)
        {
            offset += tiemodels.Count * 0x40;

            var headBytes = new byte[tiemodels.Count * 0x40];
            var bodyBytes = new List<byte>();

            for (int i = 0; i < tiemodels.Count; i++)
            {
                TieModel g = (TieModel) tiemodels[i];
                byte[] tieByte = g.SerializeHead(offset);
                byte[] bodBytes = g.SerializeBody(offset);
                bodyBytes.AddRange(bodBytes);
                offset += bodBytes.Length;
                tieByte.CopyTo(headBytes, i * 0x40);
            }

            var outBytes = new byte[headBytes.Length + bodyBytes.Count];
            headBytes.CopyTo(outBytes, 0);
            bodyBytes.CopyTo(outBytes, headBytes.Length);

            return outBytes;
        }

        private byte[] WriteShrubModels(List<Model> shrubmodels, int offset)
        {
            offset += shrubmodels.Count * 0x40;

            var headBytes = new byte[shrubmodels.Count * 0x40];
            var bodyBytes = new List<byte>();

            for (int i = 0; i < shrubmodels.Count; i++)
            {
                ShrubModel g = (ShrubModel) shrubmodels[i];
                byte[] tieByte = g.SerializeHead(offset);
                byte[] bodBytes = g.SerializeBody(offset);
                bodyBytes.AddRange(bodBytes);
                offset += bodBytes.Length;
                tieByte.CopyTo(headBytes, i * 0x40);
            }

            var outBytes = new byte[headBytes.Length + bodyBytes.Count];
            headBytes.CopyTo(outBytes, 0);
            bodyBytes.CopyTo(outBytes, headBytes.Length);

            return outBytes;
        }

        private byte[] WriteTextures(List<Texture> textures)
        {
            if (enginePath == null)
                throw new System.Exception("Cannot write textures without a path!");

            var vramBytes = new List<byte>();
            var outBytes = new byte[textures.Count * 0x24];

            for (int i = 0; i < textures.Count; i++)
            {
                Pad(vramBytes);

                int vramOffset = vramBytes.Count;

                textures[i].Serialize(vramOffset).CopyTo(outBytes, i * 0x24);
                vramBytes.AddRange(textures[i].data);
            }

            ReplanetizerFileStream fs = new ReplanetizerFileStream(Path.Join(Path.GetDirectoryName(enginePath), "vram.ps3"), FileMode.Create, FileAccess.Write);
            fs.Write(vramBytes.ToArray(), 0, vramBytes.Count);
            fs.Close();

            return outBytes;
        }

        private byte[] WritePlayerAnimations(List<Animation> animations, int animationOffset)
        {
            int offsetListLength = GetLength(animations.Count * 4);
            animationOffset += offsetListLength;

            var animByteList = new List<byte>();
            var animOffsets = new List<int>();

            foreach (Animation anim in animations)
            {
                if (anim.frames.Count != 0)
                {
                    animOffsets.Add(animationOffset);
                    byte[] anima = anim.Serialize(0, animationOffset);
                    animByteList.AddRange(anima);
                    animationOffset += anima.Length;
                }
                else
                {
                    animOffsets.Add(0);
                }
            }

            var outBytes = new byte[offsetListLength + animByteList.Count];
            for (int i = 0; i < animations.Count; i++)
            {
                WriteInt(outBytes, i * 0x04, animOffsets[i]);
            }
            animByteList.CopyTo(outBytes, offsetListLength);

            return outBytes;
        }
    }
}
