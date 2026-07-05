// Copyright (C) 2018-2021, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using System.ComponentModel;
using LibReplanetizer.Models.Animations;
using System.Collections.Generic;
using System.IO;
using static LibReplanetizer.DataFunctions;

namespace LibReplanetizer.Models
{
    public class Type10CollisionEntry1
    {
        public ushort[] vals = new ushort[16];

        public Type10CollisionEntry1() { }

        public Type10CollisionEntry1(byte[] data, int offset)
        {
            for (int i = 0; i < 16; i++)
                vals[i] = ReadUshort(data, offset + i * 2);
        }

        public byte[] Serialize()
        {
            byte[] outbytes = new byte[32];
            for (int i = 0; i < 16; i++)
                WriteUshort(outbytes, i * 2, vals[i]);
            return outbytes;
        }
    }

    public class Type10CollisionEntry2
    {
        public ushort[] vals = new ushort[2];

        public Type10CollisionEntry2() { }

        public Type10CollisionEntry2(byte[] data, int offset)
        {
            for (int i = 0; i < 2; i++)
                vals[i] = ReadUshort(data, offset + i * 2);
        }

        public byte[] Serialize()
        {
            byte[] outbytes = new byte[4];
            for (int i = 0; i < 2; i++)
                WriteUshort(outbytes, i * 2, vals[i]);
            return outbytes;
        }
    }

    public class Type10CollisionEntry3
    {
        public ushort[] vals = new ushort[4];

        public Type10CollisionEntry3() { }

        public Type10CollisionEntry3(byte[] data, int offset)
        {
            for (int i = 0; i < 4; i++)
                vals[i] = ReadUshort(data, offset + i * 2);
        }

        public byte[] Serialize()
        {
            byte[] outbytes = new byte[8];
            for (int i = 0; i < 4; i++)
                WriteUshort(outbytes, i * 2, vals[i]);
            return outbytes;
        }
    }

    public class Type10Collision
    {
        public int meta { get; set; }
        public int length1 { get; set; }
        public int length2 { get; set; }
        public int length3 { get; set; }

        public List<Type10CollisionEntry1> data1 { get; set; }
        public List<Type10CollisionEntry2> data2 { get; set; }
        public List<Type10CollisionEntry3> data3 { get; set; }

        public Type10Collision()
        {
            data1 = new List<Type10CollisionEntry1>();
            data2 = new List<Type10CollisionEntry2>();
            data3 = new List<Type10CollisionEntry3>();
        }

        public Type10Collision(byte[] data)
        {
            meta = ReadInt(data, 0x00);
            length1 = ReadInt(data, 0x04);
            length2 = ReadInt(data, 0x08);
            length3 = ReadInt(data, 0x0C);

            data1 = new List<Type10CollisionEntry1>();
            for (int i = 0; i < length1; i += 32)
                data1.Add(new Type10CollisionEntry1(data, 0x10 + i));

            data2 = new List<Type10CollisionEntry2>();
            for (int i = 0; i < length2; i += 4)
                data2.Add(new Type10CollisionEntry2(data, 0x10 + length1 + i));

            data3 = new List<Type10CollisionEntry3>();
            for (int i = 0; i < length3; i += 8)
                data3.Add(new Type10CollisionEntry3(data, 0x10 + length1 + length2 + i));
        }

        public int GetLength()
        {
            return 0x10 + length1 + length2 + length3;
        }

        public byte[] Serialize()
        {
            length1 = data1.Count * 32;
            length2 = data2.Count * 4;
            length3 = data3.Count * 8;

            byte[] outbytes = new byte[GetLength()];
            WriteInt(outbytes, 0x00, meta);
            WriteInt(outbytes, 0x04, length1);
            WriteInt(outbytes, 0x08, length2);
            WriteInt(outbytes, 0x0C, length3);

            int offset = 0x10;
            foreach (var entry in data1)
            {
                entry.Serialize().CopyTo(outbytes, offset);
                offset += 32;
            }
            foreach (var entry in data2)
            {
                entry.Serialize().CopyTo(outbytes, offset);
                offset += 4;
            }
            foreach (var entry in data3)
            {
                entry.Serialize().CopyTo(outbytes, offset);
                offset += 8;
            }

            return outbytes;
        }
    }

    public class MobyModel : MetalModel
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        const int VERTELEMENTSIZE = 0x28;
        const int METALVERTELEMENTSIZE = 0x20;
        const int TEXTUREELEMENTSIZE = 0x10;
        const int MESHHEADERSIZE = 0x20;
        const int HEADERSIZE = 0x48;

        [Category("Attributes"), DisplayName("Bone Count")]
        public byte boneCount { get; set; }
        [Category("Attributes"), DisplayName("Low Poly Bone Count")]
        public byte lpBoneCount { get; set; }            // Low poly bone count
        public byte count3 { get; set; }
        public byte count4 { get; set; }
        [Category("Attributes"), DisplayName("Low Poly Render Distance")]
        public byte lpRenderDist { get; set; }            // Low poly render distance
        public byte count8 { get; set; }

        [Category("Culling Parameters"), DisplayName("Position X")]
        public float cullingX { get; set; }
        [Category("Culling Parameters"), DisplayName("Position Y")]
        public float cullingY { get; set; }
        [Category("Culling Parameters"), DisplayName("Position Z")]
        public float cullingZ { get; set; }
        [Category("Culling Parameters"), DisplayName("Radius")]
        public float cullingRadius { get; set; }

        public uint color2 { get; set; }               // RGBA color
        public uint unk6 { get; set; }

        public ushort vertexCount2 { get; set; }

        [Category("Attributes"), DisplayName("Animations")]
        public List<Animation> animations { get; set; } = new List<Animation>();
        [Category("Attributes"), DisplayName("Sounds")]
        public List<ModelSound> modelSounds { get; set; } = new List<ModelSound>();
        [Category("Attributes"), DisplayName("Attachments")]
        public List<Attachment> attachments { get; set; } = new List<Attachment>();
        [Category("Attributes"), DisplayName("Index Attachments")]
        public List<byte> indexAttachments { get; set; } = new List<byte>();
        [Category("Attributes"), DisplayName("Bone Matrices")]
        public List<BoneMatrix> boneMatrices { get; set; } = new List<BoneMatrix>();
        [Category("Attributes"), DisplayName("Bone Datas")]
        public List<BoneData> boneDatas { get; set; } = new List<BoneData>();
        [Category("Attributes"), DisplayName("Bangles")]
        public List<Bangle> bangles { get; set; } = new List<Bangle>();

        public Skeleton? skeleton = null;
        [Category("Attributes"), DisplayName("Is Model")]
        public bool isModel { get; set; } = true;

        public override int GetSubModelCount() { return bangles.Count; }
        public override Model? GetSubModel(int index) { return (index < bangles.Count) ? (Model?) bangles[index] : null; }

        // Unparsed sections
        public Type10Collision? collisionData = null;                  // Hitbox

        private void GetMeshData(FileStream fs, int headerSize, int headerPointer, int baseOffset)
        {
            byte[] meshHeader = ReadBlock(fs, baseOffset + headerPointer, headerSize);

            int texCount = ReadInt(meshHeader, 0x00);
            int metalTexCount = ReadInt(meshHeader, 0x04);
            int texBlockPointer = baseOffset + ReadInt(meshHeader, 0x08);
            int metalTexBlockPointer = baseOffset + ReadInt(meshHeader, 0x0C);
            int vertPointer = baseOffset + ReadInt(meshHeader, 0x10);
            int indexPointer = baseOffset + ReadInt(meshHeader, 0x14);
            ushort vertexCount = ReadUshort(meshHeader, 0x18);
            ushort metalVertCount = ReadUshort(meshHeader, 0x1a);

            vertexCount2 = ReadUshort(meshHeader, 0x1C);     //These vertices are not affected by color2

            int faceCount = 0;
            if (texBlockPointer > 0)
            {
                textureConfig = GetTextureConfigs(fs, texBlockPointer, texCount, TEXTUREELEMENTSIZE);
                faceCount = GetFaceCount();
            }

            int metalFaceCount = 0;
            if (metalTexBlockPointer > 0)
            {
                metalTextureConfig = GetTextureConfigs(fs, metalTexBlockPointer, metalTexCount, TEXTUREELEMENTSIZE);
                metalFaceCount = GetMetalFaceCount();
            }

            if (vertexCount > 0)
            {
                (vertexBuffer, vertexBoneWeights, vertexBoneIds) = GetVertices(fs, vertPointer, vertexCount, VERTELEMENTSIZE);
            }

            int metalVertPointer = vertPointer + vertexCount * VERTELEMENTSIZE;
            if (metalVertCount > 0)
            {
                (metalVertexBuffer, metalVertexBoneWeights, metalVertexBoneIds) = GetMetalVertices(fs, metalVertPointer, metalVertCount);
            }

            if (faceCount > 0)
            {
                indexBuffer = GetIndices(fs, indexPointer, faceCount);
            }

            int metalIndexPointer = indexPointer + faceCount * sizeof(ushort);
            if (metalFaceCount > 0)
            {
                metalIndexBuffer = GetIndices(fs, metalIndexPointer, metalFaceCount);
            }
        }


        public MobyModel() { }

        public MobyModel(FileStream fs, GameType game, short modelID, int offset)
        {
            id = modelID;
            if (offset == 0x00)
            {
                isModel = false;
                return;
            }

            // Header
            byte[] headBlock = ReadBlock(fs, offset, HEADERSIZE);

            int meshPointer = ReadInt(headBlock, 0x00);

            Utilities.DebugAssert(ReadInt(headBlock, 0x04) == 0, "Header[0x04] is not 0!");

            boneCount = headBlock[0x08];
            lpBoneCount = headBlock[0x09];

            if (boneCount == 0) boneCount = lpBoneCount;

            count3 = headBlock[0x0A];
            count4 = headBlock[0x0B];
            byte animationCount = headBlock[0x0C];
            byte soundCount = headBlock[0x0D];
            lpRenderDist = headBlock[0x0E];
            count8 = headBlock[0x0F];

            int collisionPointer = ReadInt(headBlock, 0x10);
            int boneMatrixPointer = ReadInt(headBlock, 0x14);
            int boneDataPointer = ReadInt(headBlock, 0x18);
            int attachmentPointer = ReadInt(headBlock, 0x1C);

            Utilities.DebugAssert(ReadInt(headBlock, 0x20) == 0, "Header[0x20] is not 0!");

            size = ReadFloat(headBlock, 0x24);
            int soundPointer = ReadInt(headBlock, 0x28);
            ushort banglesPointer = ReadUshort(headBlock, 0x2C);
            ushort corncobPointer = ReadUshort(headBlock, 0x2E);

            cullingX = ReadFloat(headBlock, 0x30);
            cullingY = ReadFloat(headBlock, 0x34);
            cullingZ = ReadFloat(headBlock, 0x38);
            cullingRadius = ReadFloat(headBlock, 0x3C);

            color2 = ReadUint(headBlock, 0x40);
            unk6 = ReadUint(headBlock, 0x44);

            // dynamically determine skeleton format
            // since rc4 actually has some rc3 models in it
            var skeletonGame = game;
            if (boneDataPointer > 0 && boneMatrixPointer > 0 && boneCount > 0)
            {
                if (((boneDataPointer - boneMatrixPointer) / boneCount) == 0x30)
                    skeletonGame = GameType.DL;
                else
                    skeletonGame = GameType.RaC3;
            }

            // Animation block
            byte[] animationPointerBlock = ReadBlock(fs, offset + HEADERSIZE, animationCount * 0x04);

            for (int i = 0; i < animationCount; i++)
            {
                animations.Add(new Animation(fs, game, offset, ReadInt(animationPointerBlock, i * 0x04), boneCount));
            }

            // Bangles
            if (banglesPointer > 0)
            {
                byte[] banglesHeader = ReadBlock(fs, offset + banglesPointer * 0x10, 0x04);

                Utilities.DebugAssert(ReadInt(banglesHeader, 0x00) == 0, "Header[0x00] is not 0!");

                // Wrench always loads 15 bangles
                byte banglesCount = 15;

                byte[] banglesIndices = ReadBlock(fs, offset + banglesPointer * 0x10 + 0x04, banglesCount * 0x04);

                for (int i = 0; i < banglesCount; i++)
                {
                    int bangleHeaderOffset = ReadInt(banglesIndices, i * 0x04);

                    if (bangleHeaderOffset > 0)
                    {
                        bangles.Add(new Bangle(fs, offset, bangleHeaderOffset));
                    }
                }
            }

            // Type 10 ( has something to do with collision )
            if (collisionPointer > 0)
            {
                byte[] type10Head = ReadBlock(fs, offset + collisionPointer, 0x10);
                int type10LengthA = ReadInt(type10Head, 0x04);
                int type10LengthB = ReadInt(type10Head, 0x08);
                int type10LengthC = ReadInt(type10Head, 0x0C);
                int type10Length = type10LengthA + type10LengthB + type10LengthC;

                byte[] type10Block = ReadBlock(fs, offset + collisionPointer, 0x10 + type10Length);
                collisionData = new Type10Collision(type10Block);
            }

            // Bone matrix

            if (boneMatrixPointer > 0)
            {
                byte[] boneMatrixBlock = ReadBlock(fs, offset + boneMatrixPointer, boneCount * 0x40);
                for (int i = 0; i < boneCount; i++)
                {
                    boneMatrices.Add(new BoneMatrix(skeletonGame, boneMatrixBlock, i));
                }
            }


            // Bone data

            if (boneDataPointer > 0)
            {
                byte[] boneDataBlock = ReadBlock(fs, offset + boneDataPointer, boneCount * 0x10);
                for (int i = 0; i < boneCount; i++)
                {
                    boneDatas.Add(new BoneData(skeletonGame, boneDataBlock, i));
                }
            }

            // Attachments
            if (attachmentPointer > 0)
            {
                int attachmentCount = ReadInt(ReadBlock(fs, offset + attachmentPointer, 4), 0);
                if (attachmentCount > 0)
                {
                    byte[] headerBlock = ReadBlock(fs, offset + attachmentPointer + 4, attachmentCount * 4);
                    for (int i = 0; i < attachmentCount; i++)
                    {
                        int attachmentOffset = ReadInt(headerBlock, i * 4);
                        attachments.Add(new Attachment(fs, offset + attachmentOffset));
                    }
                }
                else
                {
                    int attid = 0;
                    while (true)
                    {
                        byte val = ReadBlock(fs, offset + attachmentPointer + 4 + attid, 1)[0];
                        if (val == 0xff) break;
                        indexAttachments.Add(val);
                        attid++;
                    }
                }
            }

            // Sounds
            if (soundPointer > 0)
            {
                byte[] soundBlock = ReadBlock(fs, offset + soundPointer, soundCount * 0x20);
                for (int i = 0; i < soundCount; i++)
                {
                    modelSounds.Add(new ModelSound(soundBlock, i));
                }
            }

            // Mesh meta
            if (meshPointer > 0)
            {
                GetMeshData(fs, MESHHEADERSIZE, meshPointer, offset);
            }

            if (boneMatrices.Count > 0 && boneDatas.Count > 0)
            {
                skeleton = new Skeleton(boneMatrices[0], null);

                for (int i = 1; i < boneCount; i++)
                {
                    skeleton.InsertBone(boneMatrices[i], boneDatas[i].parent);
                }
            }
        }

        /*
         * RaC 2 and 3 armor files contain only the mesh
         */
        public static MobyModel GetArmorMobyModel(FileStream fs, int modelPointer)
        {
            MobyModel model = new MobyModel();

            model.GetMeshData(fs, MESHHEADERSIZE, modelPointer, 0);

            return model;
        }

        public byte[] Serialize(int offset)
        {
            // Sometimes the mobys offset is not 0x10 aligned with the file,
            // but the internal offsets are supposed to be
            int alignment = 0x10 - (offset % 0x10);
            if (alignment == 0x10) alignment = 0;

            // We need to reserve some room for Ratchet's menu animations
            // this is hardcoded as 0x1c in the ELF, thus we have to just check
            // if the id of the current model is 0 I.E Ratchet, and add this offset
            int stupidOffset = 0;
            if (id == 0)
            {
                stupidOffset = 0x20 * 4;
            }

            byte[] vertexBytes = SerializeVertices();
            byte[] metalVertexBytes = SerializeMetalVertices();
            byte[] faceBytes = GetFaceBytes();
            byte[] metalIndexBytes = SerializeMetalIndices();

            //sounds
            byte[] soundBytes = new byte[modelSounds.Count * 0x20];
            for (int i = 0; i < modelSounds.Count; i++)
            {
                byte[] soundByte = modelSounds[i].Serialize();
                soundByte.CopyTo(soundBytes, i * 0x20);
            }

            //boneMatrix
            byte[] boneMatrixBytes = new byte[boneMatrices.Count * 0x40];
            for (int i = 0; i < boneMatrices.Count; i++)
            {
                byte[] boneMatrixByte = boneMatrices[i].Serialize();
                boneMatrixByte.CopyTo(boneMatrixBytes, i * 0x40);
            }

            //boneData
            byte[] boneDataBytes = new byte[boneDatas.Count * 0x10];
            for (int i = 0; i < boneDatas.Count; i++)
            {
                byte[] boneDataByte = boneDatas[i].Serialize();
                boneDataByte.CopyTo(boneDataBytes, i * 0x10);
            }

            int hack = 0;
            if (id > 2) hack = 0x20;
            int meshDataOffset = GetLength(HEADERSIZE + animations.Count * 4 + stupidOffset + hack, alignment);
            int textureConfigOffset = GetLength(meshDataOffset + 0x20, alignment);
            int metalTextureConfigOffset = GetLength(textureConfigOffset + textureConfig.Count * 0x10, alignment);

            int file80 = 0;
            if (vertexBuffer.Length != 0)
                file80 = DistToFile80(offset + metalTextureConfigOffset + metalTextureConfig.Count * 0x10);
            int vertOffset = GetLength(metalTextureConfigOffset + metalTextureConfig.Count * 0x10 + file80, alignment);
            int metalVertOffset = vertOffset + vertexBytes.Length;
            int faceOffset = GetLength(metalVertOffset + metalVertexBytes.Length, alignment);
            int metalIndexOffset = faceOffset + faceBytes.Length;

            byte[] bangleBytes = [];
            int banglesPointer = 0;
            if (bangles.Count > 0)
            {
                int maxBangles = 15;
                int bangleCount = bangles.Count < maxBangles ? bangles.Count : maxBangles;

                banglesPointer = GetLength(metalIndexOffset + metalIndexBytes.Length, alignment);
                int bangleDataOffset = GetLength(banglesPointer + 0x04 + maxBangles * 0x04, alignment);
                List<(int Offset, byte[] Data)> serializedBangles = new List<(int Offset, byte[] Data)>();

                for (int i = 0; i < bangleCount; i++)
                {
                    byte[] bangleData = bangles[i].Serialize(bangleDataOffset, alignment);
                    serializedBangles.Add((bangleDataOffset, bangleData));
                    bangleDataOffset = GetLength(bangleDataOffset + bangleData.Length, alignment);
                }

                bangleBytes = new byte[(bangleDataOffset - banglesPointer)];
                for (int i = 0; i < serializedBangles.Count; i++)
                {
                    WriteInt(bangleBytes, 0x04 + i * 0x04, serializedBangles[i].Offset);
                    serializedBangles[i].Data.CopyTo(bangleBytes, serializedBangles[i].Offset - banglesPointer);
                }
            }

            int collisionDataOffset = GetLength(metalIndexOffset + metalIndexBytes.Length + bangleBytes.Length, alignment);
            int collisionDataLength = collisionData != null ? collisionData.GetLength() : 0;
            int soundOffset = GetLength(collisionDataOffset + collisionDataLength, alignment);
            int attachmentOffset = GetLength(soundOffset + soundBytes.Length, alignment);

            List<byte> attachmentBytes = new List<byte>();
            if (attachments.Count > 0)
            {
                byte[] attachmentHead = new byte[4 + attachments.Count * 4];
                WriteInt(attachmentHead, 0, attachments.Count);
                int attOffset = attachmentOffset + 4 + attachments.Count * 4;
                for (int i = 0; i < attachments.Count; i++)
                {
                    WriteInt(attachmentHead, 4 + i * 4, attOffset);
                    byte[] attBytes = attachments[i].Serialize();
                    attachmentBytes.AddRange(attBytes);
                    attOffset += attBytes.Length;
                }
                attachmentBytes.InsertRange(0, attachmentHead);
            }
            else if (indexAttachments.Count > 0)
            {
                attachmentBytes.AddRange([0, 0, 0, 0]);
                attachmentBytes.AddRange(indexAttachments);
                attachmentBytes.Add(0xff);
            }

            int boneMatrixOffset = GetLength(attachmentOffset + attachmentBytes.Count, alignment);
            int boneDataOffset = GetLength(boneMatrixOffset + boneMatrixBytes.Length, alignment);
            int animationOffset = GetLength(boneDataOffset + boneDataBytes.Length, alignment);
            int newAnimationOffset = animationOffset;
            List<byte> animByteList = new List<byte>();

            List<int> animOffsets = new List<int>();

            foreach (Animation anim in animations)
            {
                if (anim.frames.Count != 0)
                {
                    animOffsets.Add(newAnimationOffset);
                    byte[] anima = anim.Serialize(newAnimationOffset, offset);
                    animByteList.AddRange(anima);
                    newAnimationOffset += anima.Length;
                }
                else
                {
                    animOffsets.Add(0);
                }
            }

            int modelLength = newAnimationOffset;
            byte[] outbytes = new byte[modelLength];


            // Header
            if (vertexBuffer.Length != 0)
                WriteInt(outbytes, 0x00, meshDataOffset);

            outbytes[0x08] = boneCount;
            outbytes[0x09] = lpBoneCount;
            outbytes[0x0A] = count3;
            outbytes[0x0B] = count4;
            outbytes[0x0C] = (byte) animations.Count;
            outbytes[0x0D] = (byte) modelSounds.Count;
            outbytes[0x0E] = lpRenderDist;
            outbytes[0x0F] = count8;

            if (collisionData != null)
                WriteInt(outbytes, 0x10, collisionDataOffset);

            if (id != 1 && id != 2)
            {
                WriteInt(outbytes, 0x14, boneMatrixOffset);
                WriteInt(outbytes, 0x18, boneDataOffset);
            }

            if (attachments.Count != 0 || indexAttachments.Count != 0)
                WriteInt(outbytes, 0x1C, attachmentOffset);


            //null
            WriteFloat(outbytes, 0x24, size);
            if (modelSounds.Count != 0)
                WriteInt(outbytes, 0x28, soundOffset);

            if (banglesPointer != 0)
                WriteUshort(outbytes, 0x2C, (ushort) (banglesPointer / 0x10));

            // TODO: Serialize corncobs

            WriteFloat(outbytes, 0x30, cullingX);
            WriteFloat(outbytes, 0x34, cullingY);
            WriteFloat(outbytes, 0x38, cullingZ);
            WriteFloat(outbytes, 0x3C, cullingRadius);

            WriteUint(outbytes, 0x40, color2);
            WriteUint(outbytes, 0x44, unk6);

            for (int i = 0; i < animations.Count; i++)
            {
                WriteInt(outbytes, HEADERSIZE + i * 0x04, animOffsets[i]);
            }

            vertexBytes.CopyTo(outbytes, vertOffset);
            metalVertexBytes.CopyTo(outbytes, metalVertOffset);
            faceBytes.CopyTo(outbytes, faceOffset);
            metalIndexBytes.CopyTo(outbytes, metalIndexOffset);
            bangleBytes.CopyTo(outbytes, banglesPointer);

            if (collisionData != null)
                collisionData.Serialize().CopyTo(outbytes, collisionDataOffset);

            soundBytes.CopyTo(outbytes, soundOffset);
            attachmentBytes.CopyTo(outbytes, attachmentOffset);
            boneMatrixBytes.CopyTo(outbytes, boneMatrixOffset);
            boneDataBytes.CopyTo(outbytes, boneDataOffset);
            animByteList.CopyTo(outbytes, animationOffset);


            // Mesh header
            WriteInt(outbytes, meshDataOffset + 0x00, textureConfig.Count);
            WriteInt(outbytes, meshDataOffset + 0x04, metalTextureConfig.Count);
            if (textureConfig.Count != 0)
                WriteInt(outbytes, meshDataOffset + 0x08, textureConfigOffset);
            if (metalTextureConfig.Count != 0)
                WriteInt(outbytes, meshDataOffset + 0x0c, metalTextureConfigOffset);
            if (vertexBuffer.Length != 0)
                WriteInt(outbytes, meshDataOffset + 0x10, vertOffset);
            if (faceBytes.Length != 0)
                WriteInt(outbytes, meshDataOffset + 0x14, faceOffset);
            WriteShort(outbytes, meshDataOffset + 0x18, (short) (vertexBytes.Length / VERTELEMENTSIZE));
            WriteShort(outbytes, meshDataOffset + 0x1a, (short) (metalVertexBytes.Length / METALVERTELEMENTSIZE));
            WriteShort(outbytes, meshDataOffset + 0x1C, (short) (vertexCount2));

            for (int i = 0; i < textureConfig.Count; i++)
            {
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x00, textureConfig[i].id);
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x04, textureConfig[i].start);
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x08, textureConfig[i].size);
                WriteInt(outbytes, textureConfigOffset + i * 0x10 + 0x0C, textureConfig[i].mode);
            }

            for (int i = 0; i < metalTextureConfig.Count; i++)
            {
                WriteInt(outbytes, metalTextureConfigOffset + i * 0x10 + 0x00, metalTextureConfig[i].id);
                WriteInt(outbytes, metalTextureConfigOffset + i * 0x10 + 0x04, metalTextureConfig[i].start);
                WriteInt(outbytes, metalTextureConfigOffset + i * 0x10 + 0x08, metalTextureConfig[i].size);
                WriteInt(outbytes, metalTextureConfigOffset + i * 0x10 + 0x0C, metalTextureConfig[i].mode);
            }

            return outbytes;
        }
    }
}
