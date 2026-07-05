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

        public Bangle(FileStream fs, int baseOffset, int headerOffset)
        {
            byte[] meshHeader = ReadBlock(fs, baseOffset + headerOffset, MESHHEADERSIZE);

            int texCount = ReadInt(meshHeader, 0x00);
            int metalTexCount = ReadInt(meshHeader, 0x04);
            int texBlockPointer = baseOffset + ReadInt(meshHeader, 0x08);
            int metalTexBlockPointer = baseOffset + ReadInt(meshHeader, 0x0C);
            int vertPointer = baseOffset + ReadInt(meshHeader, 0x10);
            int indexPointer = baseOffset + ReadInt(meshHeader, 0x14);
            ushort vertexCount = ReadUshort(meshHeader, 0x18);
            ushort metalVertCount = ReadUshort(meshHeader, 0x1a);

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

        public byte[] Serialize()
        {
            return Serialize(0, 0);
        }

        public byte[] Serialize(int headerOffset, int alignment)
        {
            byte[] vertexBytes = SerializeVertices();
            byte[] metalVertexBytes = SerializeMetalVertices();
            byte[] faceBytes = GetFaceBytes();
            byte[] metalIndexBytes = SerializeMetalIndices();

            int textureConfigOffset = GetLength(headerOffset + MESHHEADERSIZE, alignment);
            int metalTextureConfigOffset = GetLength(textureConfigOffset + textureConfig.Count * TEXTUREELEMENTSIZE, alignment);

            int file80 = 0;
            if (vertexBuffer.Length != 0)
                file80 = DistToFile80(metalTextureConfigOffset + metalTextureConfig.Count * TEXTUREELEMENTSIZE);

            int vertOffset = GetLength(metalTextureConfigOffset + metalTextureConfig.Count * TEXTUREELEMENTSIZE + file80, alignment);
            int metalVertOffset = vertOffset + vertexBytes.Length;
            int faceOffset = GetLength(metalVertOffset + metalVertexBytes.Length, alignment);
            int metalIndexOffset = faceOffset + faceBytes.Length;
            int bangleLength = GetLength(metalIndexOffset + metalIndexBytes.Length, alignment) - headerOffset;

            byte[] outBytes = new byte[bangleLength];

            vertexBytes.CopyTo(outBytes, vertOffset - headerOffset);
            metalVertexBytes.CopyTo(outBytes, metalVertOffset - headerOffset);
            faceBytes.CopyTo(outBytes, faceOffset - headerOffset);
            metalIndexBytes.CopyTo(outBytes, metalIndexOffset - headerOffset);

            // Mesh header stores offsets relative to the parent model base.
            WriteInt(outBytes, 0x00, textureConfig.Count);
            WriteInt(outBytes, 0x04, metalTextureConfig.Count);
            if (textureConfig.Count != 0)
                WriteInt(outBytes, 0x08, textureConfigOffset);
            if (metalTextureConfig.Count != 0)
                WriteInt(outBytes, 0x0C, metalTextureConfigOffset);
            if (vertexBuffer.Length != 0)
                WriteInt(outBytes, 0x10, vertOffset);
            if (faceBytes.Length != 0)
                WriteInt(outBytes, 0x14, faceOffset);
            WriteShort(outBytes, 0x18, (short) (vertexBytes.Length / VERTELEMENTSIZE));
            WriteShort(outBytes, 0x1A, (short) (metalVertexBytes.Length / 0x20));

            for (int i = 0; i < textureConfig.Count; i++)
            {
                int offset = textureConfigOffset - headerOffset + i * TEXTUREELEMENTSIZE;
                WriteInt(outBytes, offset + 0x00, textureConfig[i].id);
                WriteInt(outBytes, offset + 0x04, textureConfig[i].start);
                WriteInt(outBytes, offset + 0x08, textureConfig[i].size);
                WriteInt(outBytes, offset + 0x0C, textureConfig[i].mode);
            }

            for (int i = 0; i < metalTextureConfig.Count; i++)
            {
                int offset = metalTextureConfigOffset - headerOffset + i * TEXTUREELEMENTSIZE;
                WriteInt(outBytes, offset + 0x00, metalTextureConfig[i].id);
                WriteInt(outBytes, offset + 0x04, metalTextureConfig[i].start);
                WriteInt(outBytes, offset + 0x08, metalTextureConfig[i].size);
                WriteInt(outBytes, offset + 0x0C, metalTextureConfig[i].mode);
            }

            return outBytes;
        }
    }
}
