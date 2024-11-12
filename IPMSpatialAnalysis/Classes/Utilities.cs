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

        
    }
}
