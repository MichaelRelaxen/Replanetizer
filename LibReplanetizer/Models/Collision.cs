// Copyright (C) 2018-2021, The Replanetizer Contributors.
// Replanetizer is free software: you can redistribute it
// and/or modify it under the terms of the GNU General Public
// License as published by the Free Software Foundation,
// either version 3 of the License, or (at your option) any later version.
// Please see the LICENSE.md file for more details.

using LibReplanetizer.LevelObjects;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static LibReplanetizer.DataFunctions;

namespace LibReplanetizer.Models
{
    [StructLayout(LayoutKind.Explicit)]
    struct FloatColor
    {
        [FieldOffset(0)]
        public byte r;
        [FieldOffset(1)]
        public byte g;
        [FieldOffset(2)]
        public byte b;
        [FieldOffset(3)]
        public byte a;

        [FieldOffset(0)]
        public float value;
    }

    public class Collision : Model, IRenderable
    {
        public uint[] indBuff = { };
        public uint[] colorBuff = { };

        public Collision(FileStream fs, int collisionPointer)
        {
            // RaC 1 title screen has no collision
            if (collisionPointer == 0) return;

            byte[] headBlock = ReadBlock(fs, collisionPointer, 0x08);
            int standardCollisionStart = ReadInt(headBlock, 0x00);
            int heroCollisionStart = ReadInt(headBlock, 0x04);

            var vertexList = new List<float>();
            var indexList = new List<uint>();
            uint totalVertexCount = 0;

            if (standardCollisionStart > 0)
                ParseStandardCollision(fs, collisionPointer + standardCollisionStart, vertexList, indexList, ref totalVertexCount);

            if (heroCollisionStart > 0)
                ParseHeroCollision(fs, collisionPointer + heroCollisionStart, vertexList, indexList, ref totalVertexCount);

            vertexBuffer = vertexList.ToArray();
            indBuff = indexList.ToArray();
        }

        private void ParseStandardCollision(FileStream fs, int baseOffset, List<float> vertexList, List<uint> indexList, ref uint totalVertexCount)
        {
            byte[] headZBlock = ReadBlock(fs, baseOffset, 0x04);

            ushort zShift = ReadUshort(headZBlock, 0);
            ushort zCount = ReadUshort(headZBlock, 2);

            byte[] zBlock = ReadBlock(fs, baseOffset + 0x04, zCount * 0x04);

            FloatColor fc = new FloatColor { r = 255, g = 0, b = 255, a = 255 };

            for (int z = 0; z < zCount; z++)
            {
                int yOffset = ReadInt(zBlock, z * 0x04);
                if (yOffset == 0) continue;

                byte[] headYBlock = ReadBlock(fs, baseOffset + yOffset, 0x04);

                ushort yShift = ReadUshort(headYBlock, 0x00);
                ushort yCount = ReadUshort(headYBlock, 0x02);

                byte[] yBlock = ReadBlock(fs, baseOffset + yOffset + 0x04, yCount * 0x04);

                for (int y = 0; y < yCount; y++)
                {
                    int xOffset = ReadInt(yBlock, y * 0x04);
                    if (xOffset == 0) continue;

                    byte[] headXBlock = ReadBlock(fs, baseOffset + xOffset, 0x04);

                    ushort xShift = ReadUshort(headXBlock, 0x00);
                    ushort xCount = ReadUshort(headXBlock, 0x02);

                    byte[] xBlock = ReadBlock(fs, baseOffset + xOffset + 0x04, xCount * 0x04);

                    for (int x = 0; x < xCount; x++)
                    {
                        int vOffset = ReadInt(xBlock, x * 0x04);
                        if (vOffset == 0) continue;

                        byte[] headVBlock = ReadBlock(fs, baseOffset + vOffset, 0x04);

                        ushort faceCount = ReadUshort(headVBlock, 0x00);
                        byte vertexCount = headVBlock[0x02];
                        byte rCount = headVBlock[0x03];

                        byte[] dataBlock = ReadBlock(fs, baseOffset + vOffset + 0x04, faceCount * 0x04 + vertexCount * 0x0C + rCount);

                        byte[] collisionType = new byte[vertexCount];
                        for (int f = 0; f < faceCount; f++)
                        {
                            // Collision Type
                            int fOffset = (vertexCount * 0x0C) + (f * 0x04);

                            byte b0 = dataBlock[fOffset];
                            byte b1 = dataBlock[fOffset + 1];
                            byte b2 = dataBlock[fOffset + 2];
                            byte b3 = dataBlock[fOffset + 3];

                            collisionType[b0] = b3;
                            collisionType[b1] = b3;
                            collisionType[b2] = b3;

                            uint f1 = totalVertexCount + b0;
                            uint f2 = totalVertexCount + b1;
                            uint f3 = totalVertexCount + b2;
                            indexList.Add(f2);
                            indexList.Add(f1);
                            indexList.Add(f3);

                            if (f < rCount)
                            {
                                byte r = dataBlock[(vertexCount * 0x0C) + (faceCount * 0x04) + f];
                                uint f4 = totalVertexCount + r;
                                indexList.Add(f3);
                                indexList.Add(f1);
                                indexList.Add(f4);
                                collisionType[r] = b3;
                            }

                            // Vertices
                            for (int v = 0; v < vertexCount; v++)
                            {
                                float xPos = ReadFloat(dataBlock, v * 0x0c + 0x00) / 1024.0f;
                                float yPos = ReadFloat(dataBlock, v * 0x0c + 0x04) / 1024.0f;
                                float zPos = ReadFloat(dataBlock, v * 0x0c + 0x08) / 1024.0f;

                                xPos += 4 * (xShift + x + 0.5f);
                                yPos += 4 * (yShift + y + 0.5f);
                                zPos += 4 * (zShift + z + 0.5f);

                                vertexList.Add(xPos);
                                vertexList.Add(yPos);
                                vertexList.Add(zPos);

                                // Colorize different types of collision without knowing what they are
                                fc.r = (byte) ((collisionType[v] & 0x03) << 6);
                                fc.g = (byte) ((collisionType[v] & 0x0C) << 4);
                                fc.b = (byte) (collisionType[v] & 0xF0);

                                vertexList.Add(fc.value);
                                totalVertexCount++;
                            }
                        }
                    }
                }
            }
        }

        private void ParseHeroCollision(FileStream fs, int baseOffset, List<float> vertexList, List<uint> indexList, ref uint totalVertexCount)
        {
            byte[] headBlock = ReadBlock(fs, baseOffset, 0x10);

            int groupCount = ReadInt(headBlock, 0x00);

            byte[] groupHeaderBlock = ReadBlock(fs, baseOffset + 0x10, groupCount * 0x10);

            for (int i = 0; i < groupCount; i++)
            {
                int entryOffset = i * 0x10;

                ushort triCount = ReadUshort(groupHeaderBlock, entryOffset + 0x08);
                ushort vertCount = ReadUshort(groupHeaderBlock, entryOffset + 0x0A);
                int dataOffset = (int) ReadUint(groupHeaderBlock, entryOffset + 0x0C);

                // Wrench has hero collision as blue, so I figured I'll just... use that color as well...
                FloatColor fc = new FloatColor { r = 0, g = 0, b = 255, a = 255 };

                byte[] dataBlock = ReadBlock(fs, baseOffset + dataOffset, triCount * 0x04 + vertCount * 0x08);

                for (int v = 0; v < vertCount; v++)
                {
                    int vOff = v * 0x08;
                    vertexList.Add(ReadUshort(dataBlock, vOff + 0x00) / 64.0f);
                    vertexList.Add(ReadUshort(dataBlock, vOff + 0x02) / 64.0f);
                    vertexList.Add(ReadUshort(dataBlock, vOff + 0x04) / 64.0f);
                    vertexList.Add(fc.value);
                }

                for (int t = 0; t < triCount; t++)
                {
                    int tOff = vertCount * 0x08 + t * 0x04;
                    indexList.Add(totalVertexCount + dataBlock[tOff + 0x01]);
                    indexList.Add(totalVertexCount + dataBlock[tOff + 0x00]);
                    indexList.Add(totalVertexCount + dataBlock[tOff + 0x02]);
                }

                totalVertexCount += vertCount;
            }
        }
    }
}
