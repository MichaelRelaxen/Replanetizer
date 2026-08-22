using System.IO;
using static LibReplanetizer.DataFunctions;

namespace LibReplanetizer.LevelObjects
{
    public class PrecipitationMap
    {
        public const int HEADERSIZE = 0x10;

        public int rowStride { get; set; }
        public int numColumns { get; set; }
        public float lowerBound { get; set; }
        public float upperBound { get; set; }
        private byte[] rawRasterHeights;
        private float[] rasterHeigths;

        public PrecipitationMap(FileStream fs, int offset)
        {
            byte[] headBlock = ReadBlock(fs, offset, HEADERSIZE);

            rowStride = ReadInt(headBlock, 0x00);
            numColumns = ReadInt(headBlock, 0x04);
            lowerBound = ReadFloat(headBlock, 0x08);
            upperBound = ReadFloat(headBlock, 0x0C);

            rawRasterHeights = ReadBlock(fs, offset + HEADERSIZE, rowStride * numColumns);

            rasterHeigths = new float[rowStride * numColumns];
            for (int i = 0; i < rowStride * numColumns; i++)
            {
                float normalizedHeight = rawRasterHeights[i] / 255.0f;
                rasterHeigths[i] = lowerBound + (upperBound - lowerBound) * (1.0f - normalizedHeight);
            }
        }

        public float GetHeight(int x, int y)
        {
            return rasterHeigths[rowStride * y + x];
        }

        public byte[] Serialize()
        {
            byte[] bytes = new byte[HEADERSIZE + rawRasterHeights.Length];

            WriteInt(bytes, 0x00, rowStride);
            WriteInt(bytes, 0x04, numColumns);
            WriteFloat(bytes, 0x08, lowerBound);
            WriteFloat(bytes, 0x0C, upperBound);
            System.Array.Copy(rawRasterHeights, 0, bytes, HEADERSIZE, rawRasterHeights.Length);

            return bytes;
        }
    }
}
