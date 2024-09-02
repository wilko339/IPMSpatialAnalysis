using System;
using System.Drawing;

namespace IPMSpatialAnalysis.Classes
{
    /// <summary>
    /// Static class containing some useful methods and enums.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Enum storing the statistical aggregation methods used
        /// for calculating the voxel value.
        /// </summary>
        public enum AggregationMethod
        {
            Mean,
            StandardDeviation,
            Skewness,
            Sum,
            Count
        }

        /// <summary>
        /// Enum for data fields in the machine data.
        /// </summary>
        public enum DataField
        {
            Photodiode1,
            Photodiode2
        }

        /// <summary>
        /// Returns a new colour, interpolated from the two provided colours.
        /// </summary>
        /// <param name="c1">Low end colour.</param>
        /// <param name="c2">High end colour.</param>
        /// <param name="factor">Blending factor (0-1) between the colours.</param>
        /// <returns>The new colour.</returns>
        public static Color Lerp2(Color c1, Color c2, double factor)
        {
            int r = (int)(c1.R + (c2.R - c1.R) * factor);
            int g = (int)(c1.G + (c2.G - c1.G) * factor);
            int b = (int)(c1.B + (c2.B - c1.B) * factor);
            int a = (int)(c1.A + (c2.A - c1.A) * factor);
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Interpolates between three provided colours to return a new colour.
        /// </summary>
        /// <param name="c1">Low end colour.</param>
        /// <param name="c2">Mid point colour.</param>
        /// <param name="c3">High end colour.</param>
        /// <param name="factor">Blending factor (0-1) between the colours.</param>
        /// <returns>The new colour.</returns>
        public static Color Lerp3(Color c1, Color c2, Color c3, double factor)
        {
            // Clamp the factor to between 0-1.
            factor = Math.Min(1, Math.Max(factor, 0));

            if (factor < 0.5)
            {
                return Lerp2(c1, c2, factor * 2);
            }
            else
            {
                return Lerp2(c2, c3, (factor - 0.5) * 2);
            }
        }
    }
}
