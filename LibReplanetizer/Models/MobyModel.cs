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
using static LibReplanetizer.Serializers.SerializerFunctions;

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

        public int WriteBytes(FileStream fs)
        {
            int headerOffset = SeekReserve(fs, HEADERSIZE, 0x01);
            int animationOffsetsOffset = SeekReserve(fs, 0x04 * animations.Count, 0x01);

            // We need to reserve some room for Ratchet's menu animations
            // this is hardcoded as 0x1c in the ELF, thus we have to just check
            // if the id of the current model is 0 I.E Ratchet, and add this offset
            if (id == 0)
                SeekReserve(fs, 0x20 * 4, 0x01);
            else
                SeekReserve(fs, 0x20, 0x01);

            int meshDataOffset = SeekReserve(fs, (vertexCount > 0) ? 0x20 : 0);
            int textureConfigOffset = SeekReserve(fs, textureConfig.Count * 0x10, 0x01);
            int metalTextureConfigOffset = SeekReserve(fs, metalTextureConfig.Count * 0x10, 0x01);

            int vertOffset = SeekWrite(fs, SerializeVertices(), 0x80);
            int metalVertOffset = SeekWrite(fs, SerializeMetalVertices(), 0x01);
            int faceOffset = SeekWrite(fs, GetFaceBytes());
            int metalIndexOffset = SeekWrite(fs, SerializeMetalIndices(), 0x01);

            int banglesPointer = 0;
            if (bangles.Count > 0)
            {
                int maxBangles = 15;
                int bangleCount = bangles.Count < maxBangles ? bangles.Count : maxBangles;

                banglesPointer = SeekReserve(fs, 0x04 + maxBangles * 0x04, 0x01);

                byte[] banglesOffsetsBytes = new byte[0x04 + maxBangles * 0x04];

                for (int i = 0; i < bangleCount; i++)
                {
                    int bangleOffset = bangles[i].WriteBytes(fs);
                    WriteInt(banglesOffsetsBytes, 0x04 + i * 0x04, bangleOffset);
                }

                WriteBytesAtOffset(fs, banglesOffsetsBytes, banglesPointer);
            }

            int collisionDataOffset = (collisionData != null) ? SeekWrite(fs, collisionData.Serialize()) : 0;

            int soundOffset = 0;
            if (modelSounds.Count > 0)
            {
                soundOffset = SeekPast(fs, 0x10);
                for (int i = 0; i < modelSounds.Count; i++)
                    SeekWrite(fs, modelSounds[i].Serialize(), 0x01);
            }

            // Attachments
            int attachmentOffset = 0;
            if (attachments.Count > 0)
            {
                int attachmentHeaderSize = 0x04 + attachments.Count * 0x04;

                attachmentOffset = SeekReserve(fs, attachmentHeaderSize);
                byte[] attachmentHead = new byte[attachmentHeaderSize];

                WriteInt(attachmentHead, 0x00, attachments.Count);
                for (int i = 0; i < attachments.Count; i++)
                {
                    int attOffset = SeekWrite(fs, attachments[i].Serialize(), 0x01);
                    WriteInt(attachmentHead, 0x04 + i * 0x04, GetRelativeOffset(attOffset, headerOffset));
                }

                WriteBytesAtOffset(fs, attachmentHead, attachmentOffset);
            }
            else if (indexAttachments.Count > 0)
            {
                int attachmentSize = 0x04 + indexAttachments.Count + 0x01;
                byte[] attachmentBytes = new byte[attachmentSize];

                WriteInt(attachmentBytes, 0x00, 0);
                indexAttachments.CopyTo(attachmentBytes, 0x04);
                attachmentBytes[attachmentSize - 1] = 0xFF;

                attachmentOffset = SeekWrite(fs, attachmentBytes);
            }

            int boneMatrixOffset = 0;
            if (id != 1 && id != 2)
            {
                boneMatrixOffset = SeekPast(fs, 0x10);
                for (int i = 0; i < boneMatrices.Count; i++)
                    SeekWrite(fs, boneMatrices[i].Serialize(), 0x01);
            }

            int boneDataOffset = 0;
            if (id != 1 && id != 2)
            {
                boneDataOffset = SeekPast(fs, 0x10);
                for (int i = 0; i < boneDatas.Count; i++)
                    SeekWrite(fs, boneDatas[i].Serialize(), 0x01);
            }
            else
            {
                SeekPast(fs, 0x08);
            }

            // Header
            byte[] headerBytes = new byte[HEADERSIZE];
            WriteInt(headerBytes, 0x00, GetRelativeOffset(meshDataOffset, headerOffset));

            headerBytes[0x08] = boneCount;
            headerBytes[0x09] = lpBoneCount;
            headerBytes[0x0A] = count3;
            headerBytes[0x0B] = count4;
            headerBytes[0x0C] = (byte) animations.Count;
            headerBytes[0x0D] = (byte) modelSounds.Count;
            headerBytes[0x0E] = lpRenderDist;
            headerBytes[0x0F] = count8;

            WriteInt(headerBytes, 0x10, GetRelativeOffset(collisionDataOffset, headerOffset));
            WriteInt(headerBytes, 0x14, GetRelativeOffset(boneMatrixOffset, headerOffset));
            WriteInt(headerBytes, 0x18, GetRelativeOffset(boneDataOffset, headerOffset));
            WriteInt(headerBytes, 0x1C, GetRelativeOffset(attachmentOffset, headerOffset));

            WriteFloat(headerBytes, 0x24, size);
            WriteInt(headerBytes, 0x28, GetRelativeOffset(soundOffset, headerOffset));
            WriteUshort(headerBytes, 0x2C, (ushort) (banglesPointer / 0x10));

            // TODO: Serialize corncobs

            WriteFloat(headerBytes, 0x30, cullingX);
            WriteFloat(headerBytes, 0x34, cullingY);
            WriteFloat(headerBytes, 0x38, cullingZ);
            WriteFloat(headerBytes, 0x3C, cullingRadius);

            WriteUint(headerBytes, 0x40, color2);
            WriteUint(headerBytes, 0x44, unk6);

            WriteBytesAtOffset(fs, headerBytes, headerOffset);

            // Mesh Header
            if (meshDataOffset > 0)
            {
                byte[] meshDataBytes = new byte[0x20];
                WriteInt(meshDataBytes, 0x00, textureConfig.Count);
                WriteInt(meshDataBytes, 0x04, metalTextureConfig.Count);
                WriteInt(meshDataBytes, 0x08, GetRelativeOffset(textureConfigOffset, headerOffset));
                WriteInt(meshDataBytes, 0x0c, GetRelativeOffset(metalTextureConfigOffset, headerOffset));
                WriteInt(meshDataBytes, 0x10, GetRelativeOffset(vertOffset, headerOffset));
                WriteInt(meshDataBytes, 0x14, GetRelativeOffset(faceOffset, headerOffset));
                WriteShort(meshDataBytes, 0x18, (short) vertexCount);
                WriteShort(meshDataBytes, 0x1a, (short) metalVertexCount);
                WriteShort(meshDataBytes, 0x1C, (short) vertexCount2);

                WriteBytesAtOffset(fs, meshDataBytes, meshDataOffset);
            }

            // Texture Configs
            byte[] textureConfigBytes = new byte[textureConfig.Count * 0x10];
            for (int i = 0; i < textureConfig.Count; i++)
            {
                WriteInt(textureConfigBytes, i * 0x10 + 0x00, textureConfig[i].id);
                WriteInt(textureConfigBytes, i * 0x10 + 0x04, textureConfig[i].start);
                WriteInt(textureConfigBytes, i * 0x10 + 0x08, textureConfig[i].size);
                WriteInt(textureConfigBytes, i * 0x10 + 0x0C, textureConfig[i].mode);
            }

            WriteBytesAtOffset(fs, textureConfigBytes, textureConfigOffset);

            // Metal Texture Configs
            byte[] metalTextureConfigBytes = new byte[metalTextureConfig.Count * 0x10];
            for (int i = 0; i < metalTextureConfig.Count; i++)
            {
                WriteInt(metalTextureConfigBytes, i * 0x10 + 0x00, metalTextureConfig[i].id);
                WriteInt(metalTextureConfigBytes, i * 0x10 + 0x04, metalTextureConfig[i].start);
                WriteInt(metalTextureConfigBytes, i * 0x10 + 0x08, metalTextureConfig[i].size);
                WriteInt(metalTextureConfigBytes, i * 0x10 + 0x0C, metalTextureConfig[i].mode);
            }

            WriteBytesAtOffset(fs, metalTextureConfigBytes, metalTextureConfigOffset);

            // AnimationOffsets
            byte[] animationOffsetsBytes = new byte[0x04 * animations.Count];
            for (int i = 0; i < animations.Count; i++)
            {
                int animOffset = animations[i].WriteBytes(fs, headerOffset);
                WriteInt(animationOffsetsBytes, i * 0x04, GetRelativeOffset(animOffset, headerOffset));
            }

            WriteBytesAtOffset(fs, animationOffsetsBytes, animationOffsetsOffset);

            SeekPast(fs, 0x10);

            return headerOffset;
        }
    }
}
