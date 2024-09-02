using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPMSpatialAnalysis.Classes
{
    /// <summary>
    /// This class handles the organisation of the data in the voxel structure. 
    /// </summary>
    public class VoxelStructure
    {
        #region Properties
        public ((double minx, double miny, double minz), (double maxx, double maxy, double maxz)) BoundingBox
        {
            get
            {
                return GetWorldBoundingBox();
            }
        }
        public double StandardDeviation => _standardDeviation;
        public double Mean => _mean;
        public (double, double, double) Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                _offset = value;
            }
        }

        public double Min => _min;

        public double Max => _max;

        public int Count => _nonNullCount;
        public Dictionary<(int, int, int), double> VoxelScalars
        {
            get { return GetNonNullVoxelScalars(); }
        }

        #endregion
        #region Constructors

        /// <summary>
        /// Default constructor. Initialises the _voxelStructure dictionary, 
        /// sets the _voxelSize to -1 (so you know it needs to be set), and sets
        /// the _offset to (0, 0, 0).
        /// </summary>
        public VoxelStructure()
        {
            _voxelStructure = new Dictionary<(int x, int y, int z), VoxelData>();
            _voxelSize = -1;
            _offset = (0, 0, 0);
        }

        /// <summary>
        /// Constructor that sets the _voxelSize to the given amount, and initialises
        /// the _voxelStructure dictionary, and sets the _offset to (0, 0, 0).
        /// </summary>
        /// <param name="voxelSize">The voxel size.</param>
        public VoxelStructure(double voxelSize)
        {
            _voxelStructure = new Dictionary<(int x, int y, int z), VoxelData>();
            _voxelSize = voxelSize;
            _offset = (0, 0, 0);
        }

        /// <summary>
        /// A copy constructor, copying the values of the given voxelStructure into the new instance. 
        /// </summary>
        /// <param name="voxelStructure">The VoxelStructure instance to copy.</param>
        public VoxelStructure(VoxelStructure voxelStructure)
        {
            _voxelSize = voxelStructure._voxelSize;
            _offset = voxelStructure._offset;

            // Deep copy of _voxelStructure dictionary
            _voxelStructure = new Dictionary<(int, int, int), VoxelData>(voxelStructure._voxelStructure.Count);
            foreach (var kvp in voxelStructure._voxelStructure)
            {
                _voxelStructure[kvp.Key] = new VoxelData(kvp.Value);
            }

            UpdateStatistics();
        }
        #endregion
        #region Public Methods

        /// <summary>
        /// Add multiple points to the voxel structure.
        /// </summary>
        /// <param name="points">IEnumerable containing point coordinates and scalar values.</param>
        public void AddPoints(IEnumerable<(double x, double y, double z, double scalar)> points)
        {
            lock (_voxelLock)
            {
                foreach (var point in points)
                {
                    AddPoint(point.x, point.y, point.z, point.scalar);
                }
            }
        }

        /// <summary>
        /// Add a single point to the voxel structure. 
        /// </summary>
        /// <param name="x">Point x coordinate.</param>
        /// <param name="y">Point y coordinate.</param>
        /// <param name="z">Point z coordinate.</param>
        /// <param name="scalarValue">Scalar value associated with point.</param>
        public void AddPoint(double x, double y, double z, double scalarValue)
        {
            var key = WorldToVoxel(x, y, z);
            if (!_voxelStructure.TryGetValue(key, out var voxelData))
            {
                voxelData = new VoxelData();
                _voxelStructure[key] = voxelData;
            }
            voxelData.Add(scalarValue);
        }

        /// <summary>
        /// Returns the corresponding voxel coordinate for a given world space coordinate.
        /// </summary>
        /// <param name="x">X coordinate in world space.</param>
        /// <param name="y">Y coordinate in world space.</param>
        /// <param name="z">Z coordinate in world space.</param>
        /// <returns>A tuple containing the x, y and z components of the voxel space coordinate.</returns>
        public (int, int, int) WorldToVoxel(double x, double y, double z)
        {
            int vx = (int)Math.Floor((x - _offset.x) / _voxelSize);
            int vy = (int)Math.Floor((y - _offset.y) / _voxelSize);
            int vz = (int)Math.Floor((z - _offset.z) / _voxelSize);

            return (vx, vy, vz);
        }

        /// <summary>
        /// Voxel field to world space transformation using individual coordinate components.
        /// </summary>
        /// <param name="x">Voxel space x coordinate.</param>
        /// <param name="y">Voxel space y coordinate.</param>
        /// <param name="z">Voxel space z coordinate.</param>
        /// <returns>A tuple containing the x, y, and z components of the world space coordinate for the given voxel.</returns>
        public (double, double, double) VoxelToWorld(int x, int y, int z)
        {
            double wx = x * _voxelSize + _voxelSize / 2 + _offset.x;
            double wy = y * _voxelSize + _voxelSize / 2 + _offset.y;
            double wz = z * _voxelSize + _voxelSize / 2 + _offset.z;

            return (wx, wy, wz);
        }

        /// <summary>
        /// Voxel field to world space transformation using a voxel key.
        /// </summary>
        /// <param name="voxelKey">The integer voxel key (in, int, int).</param>
        /// <returns>A tuple containing the x, y, and z components of the world space coordinate for the given voxel.</returns>
        public (double, double, double) VoxelToWorld((int x, int y, int z) voxelKey)
        {
            double wx = voxelKey.x * _voxelSize + _voxelSize / 2 + _offset.x;
            double wy = voxelKey.y * _voxelSize + _voxelSize / 2 + _offset.y;
            double wz = voxelKey.z * _voxelSize + _voxelSize / 2 + _offset.z;

            return (wx, wy, wz);
        }

        /// <summary>
        /// Clears all of the data within each voxel while leaving the overall structure intact.
        /// </summary>
        public void ClearData()
        {
            var keys = _voxelStructure.Keys.ToList();

            foreach (var voxel in keys)
            {
                _voxelStructure[voxel] = new VoxelData();
            }
        }

        /// <summary>
        /// Sets all voxel data scalars to the given value.
        /// </summary>
        /// <param name="v"></param>
        public void SetVoxelDataValues(double v)
        {
            var keys = _voxelStructure.Keys.ToList();

            foreach (var voxel in keys)
            {
                _voxelStructure[voxel] = new VoxelData(v);
            }
        }

        /// <summary>
        /// Sets the voxel specified by the key to the given value.
        /// </summary>
        /// <param name="key">The integer tuple voxel key.</param>
        /// <param name="value">The value to set.</param>
        public void SetVoxelDataValue((int, int, int) key, double value)
        {
            if (_voxelStructure.TryGetValue(key, out var voxelData))
            {
                voxelData.ScalarData = new List<double>();
                voxelData.Value = value;
            }
        }

        /// <summary>
        /// Calculates the voxel values using the given aggregationMethod and voxelRadius.
        /// </summary>
        /// <param name="aggregationMethod">The aggregation method to combine sample data.</param>
        /// <param name="voxelRadius">The voxel radius of data to include in the calculation.</param>
        public void CalculateVoxelData(Utilities.AggregationMethod aggregationMethod, int voxelRadius)
        {
            lock (_voxelLock)
            {
                foreach (var voxelKey in _voxelStructure.Keys)
                {
                    var scalarValue = CalculateVoxelScalar(voxelKey, aggregationMethod, voxelRadius);
                    _voxelStructure[voxelKey].Value = scalarValue;
                }
            }
            UpdateStatistics();
        }

        /// <summary>
        /// Recalculates the statistics of the overall voxel structure.
        /// </summary>
        public void UpdateStatistics()
        {
            if (_voxelStructure.Count < 1) return;
            var nonNulls = GetNonNullScalarValues();

            if (nonNulls.Count == 0) return;

            _min = nonNulls.Min();
            _max = nonNulls.Max();

            _sum = nonNulls.Sum();

            _standardDeviation = nonNulls.PopulationStandardDeviation();
            _nonNullCount = nonNulls.Count;

            _mean = _sum / _nonNullCount;
        }

        #endregion
        #region Private Methods

        /// <summary>
        /// Returns a list of all of the non-null voxel scalar values. 
        /// Used for calculating overall statistics of the structure.
        /// </summary>
        /// <returns>A list containing all of the valid voxel values.</returns>
        private List<double> GetNonNullScalarValues()
        {
            return _voxelStructure.Values
                    .Where(x => x.HasValue)
                    .Where(x => !double.IsNaN(x.Value.Value))
                    .Select(x => x.Value.Value)
                    .ToList();
        }

        /// <summary>
        /// Traverse the voxels to get the minimum and maximun indices 
        /// before converting those to world space. 
        /// </summary>
        /// <returns>A tuple of tuples representing the min and max 
        /// corners of the bounding box in world space.</returns>
        private (
            (double minx, double miny, double minz),
            (double maxx, double maxy, double maxz))
            GetWorldBoundingBox()
        {
            if (_voxelStructure.Count == 0) return ((0, 0, 0), (0, 0, 0));

            (int x, int y, int z) minVoxel = (int.MaxValue, int.MaxValue, int.MaxValue);
            (int x, int y, int z) maxVoxel = (int.MinValue, int.MinValue, int.MinValue);

            lock (_voxelLock)
            {
                foreach ((int x, int y, int z) voxel in _voxelStructure.Keys)
                {
                    if (voxel.x < minVoxel.x) minVoxel.x = voxel.x;
                    if (voxel.y < minVoxel.y) minVoxel.y = voxel.y;
                    if (voxel.z < minVoxel.z) minVoxel.z = voxel.z;

                    if (voxel.x > maxVoxel.x) maxVoxel.x = voxel.x;
                    if (voxel.y > maxVoxel.y) maxVoxel.y = voxel.y;
                    if (voxel.z > maxVoxel.z) maxVoxel.z = voxel.z;
                }
            }

            var minWorld = VoxelToWorld(minVoxel.x, minVoxel.y, minVoxel.z);
            var maxWorld = VoxelToWorld(maxVoxel.x, maxVoxel.y, maxVoxel.z);

            return (minWorld, maxWorld);
        }

        /// <summary>
        /// Returns a version of the voxel structure that only contains non-null values.
        /// </summary>
        /// <returns>The non-null dictionary.</returns>
        private Dictionary<(int, int, int), double> GetNonNullVoxelScalars()
        {
            var keys = _voxelStructure.Keys;
            var nonNulls = new Dictionary<(int, int, int), double>();

            foreach (var key in keys)
            {
                if (_voxelStructure.TryGetValue(key, out var voxel))
                {
                    if (voxel.HasValue)
                    {
                        nonNulls[key] = voxel.Value.Value;
                    }
                }
            }
            return nonNulls;
        }

        /// <summary>
        /// Calculates the scalar value for the specified voxel using the aggrgationMethod and aggregationRadius given.
        /// </summary>
        /// <param name="voxelKey">The voxel to calculate.</param>
        /// <param name="aggregationMethod">The method to combine sample data.</param>
        /// <param name="aggregationRadius">The voxel radius to consider sample data.</param>
        /// <returns>The nullable double representing the scala voxel value.</returns>
        private double? CalculateVoxelScalar((int x, int y, int z) voxelKey, Utilities.AggregationMethod aggregationMethod, int aggregationRadius)
        {
            List<double> aggregatedData = new List<double>();
            int startX = voxelKey.x - aggregationRadius;
            int endX = voxelKey.x + aggregationRadius;
            int startY = voxelKey.y - aggregationRadius;
            int endY = voxelKey.y + aggregationRadius;
            int startZ = voxelKey.z - aggregationRadius;
            int endZ = voxelKey.z + aggregationRadius;

            double sum = 0;
            double sumOfSquares = 0;
            int count = 0;

            if (aggregationRadius < 1)
            {
                if (!_voxelStructure.TryGetValue(voxelKey, out var voxelData)) return null;
                else
                {
                    aggregatedData = voxelData.ScalarData;
                    sum = aggregatedData.Sum();
                    count = aggregatedData.Count();
                    sumOfSquares = aggregatedData.Sum(v => v * v);
                }
            }

            else
            {
                for (int ix = startX; ix < endX; ix++)
                {
                    for (int iy = startY; iy < endY; iy++)
                    {
                        for (int iz = startZ; iz < endZ; iz++)
                        {
                            if (_voxelStructure.TryGetValue((ix, iy, iz), out var subvoxel))
                            {
                                foreach (var value in subvoxel.ScalarData)
                                {
                                    sum += value;
                                    sumOfSquares += value * value;
                                    count++;
                                    if (aggregationMethod == Utilities.AggregationMethod.Skewness)
                                    {
                                        aggregatedData.Add(value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            double outValue = 0;

            switch (aggregationMethod)
            {
                case Utilities.AggregationMethod.Mean:
                    outValue = aggregatedData.Mean();
                    break;

                case Utilities.AggregationMethod.Sum:
                    outValue = aggregatedData.Sum();
                    break;

                case Utilities.AggregationMethod.StandardDeviation:
                    outValue = aggregatedData.PopulationStandardDeviation();
                    break;

                case Utilities.AggregationMethod.Skewness:
                    outValue = aggregatedData.Skewness();
                    break;

                case Utilities.AggregationMethod.Count:
                    outValue = aggregatedData.Count;
                    break;

                default:
                    outValue = aggregatedData.Mean();
                    break;
            }
            lock (_scalarLock)
            {
                _voxelStructure[voxelKey].Value = outValue;
            }

            if (double.IsNaN(outValue))
            {
                return null;
            }

            return outValue;
        }
        #endregion

        #region Fields

        private Dictionary<(int, int, int), VoxelData> _voxelStructure;

        private readonly object _voxelLock = new object();
        private readonly object _scalarLock = new object();

        private readonly double _voxelSize;

        private (double x, double y, double z) _offset = (0, 0, 0);

        private double _sum;
        private double _mean;
        private double _standardDeviation;
        private double _min;
        private double _max;

        private int _nonNullCount = -1;

        #endregion
    }
}
