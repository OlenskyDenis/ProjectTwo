namespace ProjectTwo.Terrain.Core.Models
{
    using System;

    /// <summary>
    /// Represents a 2D matrix of sampled and normalized elevation values.
    /// </summary>
    public class HeightMap
    {
        public float[,] Values { get; }
        public int Width { get; }
        public int Height { get; }
        public float MinValue { get; }
        public float MaxValue { get; }

        public HeightMap(float[,] values, float minValue = 0f, float maxValue = 1f)
        {
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Width = values.GetLength(0);
            Height = values.GetLength(1);
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public float GetNormalizedValue(int x, int y)
        {
            if (x < 0) x = 0;
            if (x >= Width) x = Width - 1;
            if (y < 0) y = 0;
            if (y >= Height) y = Height - 1;

            return Values[x, y];
        }

        public float InterpolateValue(float normalizedX, float normalizedY)
        {
            float xPos = normalizedX * (Width - 1);
            float yPos = normalizedY * (Height - 1);

            int x0 = (int)xPos;
            int y0 = (int)yPos;
            int x1 = Math.Min(x0 + 1, Width - 1);
            int y1 = Math.Min(y0 + 1, Height - 1);

            float fx = xPos - x0;
            float fy = yPos - y0;

            float v00 = Values[x0, y0];
            float v10 = Values[x1, y0];
            float v01 = Values[x0, y1];
            float v11 = Values[x1, y1];

            float top = v00 * (1f - fx) + v10 * fx;
            float bottom = v01 * (1f - fx) + v11 * fx;

            return top * (1f - fy) + bottom * fy;
        }
    }
}
