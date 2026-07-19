// Copyright (C) 2018-2025, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using NLog;
using OpenTK.Mathematics;
using static LibReplanetizer.DataFunctions;
using static LibReplanetizer.Serializers.SerializerFunctions;

namespace LibReplanetizer.Models
{
    public class Bangle : MetalModel
    {
        private static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        const int VERTELEMENTSIZE = 0x28;
        const int TEXTUREELEMENTSIZE = 0x10;
        const int MESHHEADERSIZE = 0x20;

        [Category("Unknowns"), DisplayName("Other Buffer")]
        public List<byte> otherBuffer { get; set; } = new List<byte>();

        [Category("Unknowns"), DisplayName("Other Texture Configurations")]
        public List<TextureConfig> otherTextureConfigs { get; set; } = new List<TextureConfig>();

        [Category("Unknowns"), DisplayName("Other Index Buffer")]
        public List<ushort> otherIndexBuffer { get; set; } = new List<ushort>();
        private int unk1C;
        private byte[] unkBytes = new byte[0x10];

        public Bangle(FileStream fs, int baseOffset, int headerOffset, int unkDataOffset)
        {
            byte[] meshHeader = ReadBlock(fs, baseOffset + headerOffset, MESHHEADERSIZE);

            int texCount = ReadInt(meshHeader, 0x00);
            int metalTexCount = ReadInt(meshHeader, 0x04);
            int texBlockPointer = baseOffset + ReadInt(meshHeader, 0x08);
            int metalTexBlockPointer = baseOffset + ReadInt(meshHeader, 0x0C);
            int vertPointer = baseOffset + ReadInt(meshHeader, 0x10);
            int indexPointer = baseOffset + ReadInt(meshHeader, 0x14);
            ushort vertexCount = ReadUshort(meshHeader, 0x18);
            ushort metalVertCount = ReadUshort(meshHeader, 0x1A);
            unk1C = ReadInt(meshHeader, 0x1C);

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

            unkBytes = ReadBlock(fs, baseOffset + unkDataOffset, 0x10);
        }

        public int WriteBytes(FileStream fs, int mobyHeaderOffset, int unkDataOffset)
        {
            int headerOffset = SeekReserve(fs, MESHHEADERSIZE);

            int textureConfigOffset = SeekReserve(fs, textureConfig.Count * TEXTUREELEMENTSIZE, 0x01);
            int metalTextureConfigOffset = SeekReserve(fs, metalTextureConfig.Count * TEXTUREELEMENTSIZE, 0x01);

            int vertOffset = SeekWrite(fs, SerializeVertices(), 0x80);
            int metalVertOffset = SeekWrite(fs, SerializeMetalVertices(), 0x01);
            int faceOffset = SeekWrite(fs, GetFaceBytes(), 0x10);
            int metalIndexOffset = SeekWrite(fs, SerializeMetalIndices(), 0x01);

            byte[] headerBytes = new byte[MESHHEADERSIZE];
            WriteInt(headerBytes, 0x00, textureConfig.Count);
            WriteInt(headerBytes, 0x04, metalTextureConfig.Count);
            WriteInt(headerBytes, 0x08, GetRelativeOffset(textureConfigOffset, mobyHeaderOffset));
            WriteInt(headerBytes, 0x0C, GetRelativeOffset(metalTextureConfigOffset, mobyHeaderOffset));
            WriteInt(headerBytes, 0x10, GetRelativeOffset(vertOffset, mobyHeaderOffset));
            WriteInt(headerBytes, 0x14, GetRelativeOffset(faceOffset, mobyHeaderOffset));
            WriteShort(headerBytes, 0x18, (short) vertexCount);
            WriteShort(headerBytes, 0x1A, (short) metalVertexCount);
            WriteInt(headerBytes, 0x1C, unk1C);

            WriteBytesAtOffset(fs, headerBytes, headerOffset);

            byte[] textureConfigBytes = new byte[textureConfig.Count * TEXTUREELEMENTSIZE];
            for (int i = 0; i < textureConfig.Count; i++)
            {
                WriteInt(textureConfigBytes, i * TEXTUREELEMENTSIZE + 0x00, textureConfig[i].id);
                WriteInt(textureConfigBytes, i * TEXTUREELEMENTSIZE + 0x04, textureConfig[i].start);
                WriteInt(textureConfigBytes, i * TEXTUREELEMENTSIZE + 0x08, textureConfig[i].size);
                WriteInt(textureConfigBytes, i * TEXTUREELEMENTSIZE + 0x0C, textureConfig[i].mode);
            }

            WriteBytesAtOffset(fs, textureConfigBytes, textureConfigOffset);

            byte[] metalTextureConfigBytes = new byte[metalTextureConfig.Count * TEXTUREELEMENTSIZE];
            for (int i = 0; i < metalTextureConfig.Count; i++)
            {
                WriteInt(metalTextureConfigBytes, i * TEXTUREELEMENTSIZE + 0x00, metalTextureConfig[i].id);
                WriteInt(metalTextureConfigBytes, i * TEXTUREELEMENTSIZE + 0x04, metalTextureConfig[i].start);
                WriteInt(metalTextureConfigBytes, i * TEXTUREELEMENTSIZE + 0x08, metalTextureConfig[i].size);
                WriteInt(metalTextureConfigBytes, i * TEXTUREELEMENTSIZE + 0x0C, metalTextureConfig[i].mode);
            }

            WriteBytesAtOffset(fs, metalTextureConfigBytes, metalTextureConfigOffset);

            WriteBytesAtOffset(fs, unkBytes, unkDataOffset);

            return headerOffset;
        }
    }
}
