using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.GUI;
using MathNet.Numerics.Statistics;

namespace IPMSpatialAnalysis.Classes
{
    /// <summary>
    /// Manages a 3D voxel structure for spatial data analysis.
    /// Each voxel contains statistical data.
    /// Thread-safe.
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
            _transformationMatrix = voxelStructure._transformationMatrix;

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

            return _transformationMatrix.Transform(wx, wy, wz);
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

            return _transformationMatrix.Transform(wx, wy, wz);
        }

        public void SetTransformation(Matrix4x4 transformationMatrix)
        {
            _transformationMatrix = transformationMatrix;
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
        /// Adds a new voxel key to the structure.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddVoxelValue((int, int, int) key, double value)
        {
            _voxelStructure.Add(key, new VoxelData(value));
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
        /// Recalculates the statistics of the voxel structure.
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

        public void Sigmoid(double factor)
        {
            var voxelKeys = _voxelStructure.Keys.ToList();

            // Then apply the sigmoid function
            Parallel.ForEach(voxelKeys, voxelKey =>
            {
                var value = _voxelStructure[voxelKey].Value.Value;
                _voxelStructure[voxelKey].Value = 1 / (1 + Math.Exp(-factor * value));
            });

            UpdateStatistics();

            SetNullStoredRawData();
        }

        /// <summary>
        /// Remaps the voxel values to the given range.
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public void RemapVoxelValues(double low, double high)
        {
            UpdateStatistics();

            var voxelKeys = _voxelStructure.Keys.ToList();

            Parallel.ForEach(voxelKeys, voxelKey =>
            {
                var value = _voxelStructure[voxelKey].Value.Value;
                _voxelStructure[voxelKey].Value = (value - _min) / (_max - _min) * (high - low) + low ;
            });

            UpdateStatistics();
            SetNullStoredRawData();
        }

        /// <summary>
        /// Remaps the voxel values to the given range.
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public void RemapVoxelValues(double inputMin, double inputMax, double outputMin, double outputMax)
        {
            UpdateStatistics();

            var voxelKeys = _voxelStructure.Keys.ToList();

            Parallel.ForEach(voxelKeys, voxelKey =>
            {
                var value = _voxelStructure[voxelKey].Value.Value;
                _voxelStructure[voxelKey].Value = (value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin;
            });

            UpdateStatistics();
            SetNullStoredRawData();
        }

        /// <summary>
        /// Normalises the data using it's mean and standard deviation.
        /// </summary>
        public void NormaliseData()
        {
            if (VoxelScalars.Count < 1) return;
            var nonNANs = VoxelScalars.Values.Where(x => !double.IsNaN(x));
            double mean = nonNANs.Mean();
            double standardDeviation = nonNANs.PopulationStandardDeviation();

            var voxelKeys = _voxelStructure.Keys.ToList();

            // Update the values in the dictionary
            Parallel.ForEach(voxelKeys, voxelKey =>
            {
                var value = _voxelStructure[voxelKey].Value.Value;
                _voxelStructure[voxelKey].Value = (value - mean) / standardDeviation;
            });

            UpdateStatistics();
            // The stored raw data doesn't really relate to the scalar value
            // so we may as well free up memory.
            SetNullStoredRawData();
        }

        public void MaxMinNormaliseData()
        {
            if (VoxelScalars.Count < 1) return;
            var nonNANs = VoxelScalars.Values.Where(x => !double.IsNaN(x));
            double max = nonNANs.Max();
            double min = nonNANs.Min();

            MaxMinNormaliseData(min, max);
        }

        public void MaxMinNormaliseData(double min, double max)
        {
            var voxelKeys = _voxelStructure.Keys.ToList();

            // Update the values in the dictionary
            Parallel.ForEach(voxelKeys, voxelKey =>
            {
                var value = _voxelStructure[voxelKey].Value.Value;
                _voxelStructure[voxelKey].Value = (value - min) / (max - min);
            });

            UpdateStatistics();
            // The stored raw data doesn't really relate to the scalar value
            // so we may as well free up memory.
            SetNullStoredRawData();
        }

        public void ClampVoxelValues(double min, double max)
        {
            var voxelKeys = _voxelStructure.Keys.ToList();

            // Update the values in the dictionary
            Parallel.ForEach(voxelKeys, voxelKey =>
            {
                var value = _voxelStructure[voxelKey].Value.Value;
                _voxelStructure[voxelKey].Value = Math.Max(Math.Min(value, max), min);
            });

            UpdateStatistics();
            // The stored raw data doesn't really relate to the scalar value
            // so we may as well free up memory.
            SetNullStoredRawData();
        }

        /// <summary>
        /// Performs column normalisation by scaling all voxel values by the mean and 
        /// standard deviation of all voxels in the same xy column.
        /// If a column only contains a single value, that value is unchanged.
        /// </summary>
        public void ColumnNormaliseData()
        {
            if (_voxelStructure == null) return;

            var bbox = GetVoxelBoundingBox();

            int minx = bbox.Item1.minx;
            int maxx = bbox.Item2.maxx + 1;

            int miny = bbox.Item1.miny;
            int maxy = bbox.Item2.maxy + 1;

            int minz = bbox.Item1.minz;
            int maxz = bbox.Item2.maxz + 1;

            double[,] columnMeans = new double[maxx - minx, maxy - miny];
            double[,] columnSTDs = new double[maxx - minx, maxy - miny];

            // Loop over all values to calculate mean and std for each data column.
            for (int i = minx; i < maxx; i++)
            {
                for (int j = miny; j < maxy; j++)
                {
                    List<double> columnValues = new List<double>();
                    for (int k = minz; k < maxz; k++)
                    {
                        if (_voxelStructure.TryGetValue((i, j, k), out VoxelData voxelData))
                        {
                            if (voxelData.HasValue) columnValues.Add(voxelData.Value.Value);
                        }
                    }

                    if (columnValues.Count > 1)
                    {
                        columnMeans[i - minx, j - miny] = columnValues.Mean();
                        columnSTDs[i - minx, j - miny] = columnValues.PopulationStandardDeviation();
                    }
                    else
                    {
                        columnMeans[i - minx, j - miny] = 0;
                        columnSTDs[i - minx, j - miny] = 1;
                    }
                }
            }
            // Second loop to normalise values
            for (int i = minx; i < maxx; i++)
            {
                for (int j = miny; j < maxy; j++)
                {
                    for (int k = minz; k < maxz; k++)
                    {
                        if (_voxelStructure.TryGetValue((i, j, k), out VoxelData voxelData))
                        {
                            if (voxelData.HasValue)
                            {
                                voxelData.Value = (voxelData.Value.Value - columnMeans[i - minx, j - miny]) /
                                    columnSTDs[i - minx, j - miny];
                            }
                        }
                    }
                }
            }
            UpdateStatistics();

            // The stored raw data doesn't really relate to the scalar value
            // so we may as well free up memory.
            SetNullStoredRawData();
        }

        /// <summary>
        /// Removes voxels from the structure that contain fewer than minDataCount items. 
        /// </summary>
        /// <param name="minDataCount"></param>
        public void PruneVoxels(int minDataCount)
        {
            var keys = _voxelStructure.Keys.ToList();

            foreach (var key in keys)
            {
                if (_voxelStructure[key].Count < minDataCount)
                {
                    _voxelStructure.Remove(key);
                }
            }
        }

        /// <summary>
        /// Removes voxels outside of the provided range, with an optional flag to also remove 
        /// zero value voxels.
        /// </summary>
        /// <param name="min">Lower bound</param>
        /// <param name="max">Upper bound</param>
        /// <param name="removeZeros">Whether to remove zero value voxels</param>
        public void FilterByScalarValues(double min, double max, bool removeZeros)
        {
            // Create a list of keys to remove
            var keysToRemove = _voxelStructure
                .Where(item => item.Value.Value < min || item.Value.Value > max)
                .Select(item => item.Key)
                .ToList();

            // Remove the keys from the dictionaries
            foreach (var key in keysToRemove)
            {
                _voxelStructure.Remove(key);
            }

            UpdateStatistics();
        }

        public void CalculateSpatialCorrelation(int getisRadius)
        {
            var voxelKeys = _voxelStructure.Keys.ToList();
            ConcurrentDictionary<(int, int, int), VoxelData> newVoxelData = new ConcurrentDictionary<(int, int, int), VoxelData>();

            // Make sure we have up to date statistics.
            UpdateStatistics();

            Parallel.ForEach(voxelKeys, voxel =>
            {
                double go = CalculateGetisOrd(voxel.Item1, voxel.Item2, voxel.Item3, getisRadius);
                VoxelData newData = new VoxelData(go);

                newVoxelData.TryAdd(voxel, newData);
            });

            _voxelStructure = new Dictionary<(int, int, int), VoxelData>(newVoxelData);

            // Then update this again with the new data.
            UpdateStatistics();

            // The stored raw data doesn't really relate to the scalar value
            // so we may as well free up memory.
            SetNullStoredRawData();
        }

        /// <summary>
        /// Calculates the spatial correlation for the entire voxel structure. 
        /// The getisRadius determines the number of voxel layers in the neighbourhood.
        /// </summary>
        /// <param name="getisRadius"></param>
        public void CalculateSpatialCorrelation(int getisRadius, double globalMean, double globalStandardDeviation)
        {
            var voxelKeys = _voxelStructure.Keys.ToList();
            ConcurrentDictionary<(int, int, int), VoxelData> newVoxelData = new ConcurrentDictionary<(int, int, int), VoxelData>();

            // Make sure we have up to date statistics.
            UpdateStatistics();

            Parallel.ForEach(voxelKeys, voxel =>
            {
                double go = CalculateGetisOrd(voxel.Item1, voxel.Item2, voxel.Item3, getisRadius, globalMean, globalStandardDeviation);
                VoxelData newData = new VoxelData(go);

                newVoxelData.TryAdd(voxel, newData);
            });

            _voxelStructure = new Dictionary<(int, int, int), VoxelData>(newVoxelData);

            // Then update this again with the new data.
            UpdateStatistics();

            // The stored raw data doesn't really relate to the scalar value
            // so we may as well free up memory.
            SetNullStoredRawData();
        }

        /// <summary>
        /// Executes a custom delegate function for each voxel value in the structure.
        /// </summary>
        /// <param name="func">The function to execute, taking the voxel value as the only argument, returning a double value.</param>
        public void ExecuteCustomFunction(Func<double, double> func)
        {
            foreach (var voxel in _voxelStructure.Values)
            {
                if (voxel.HasValue)
                {
                    voxel.Value = func(voxel.Value.Value);
                }
            }
            UpdateStatistics();
        }

        /// <summary>
        /// Exectutes a custome delegate function for each corresponding voxel pair in the current structure
        /// and another provided structure.
        /// </summary>
        /// <param name="function">The function to execute, taking the current voxel value and the corresponding
        /// value in the other provided VoxelStructure.</param>
        /// <param name="other">The other VoxelStructure to operate over.</param>
        public void ExecuteCustomFunction(Func<double, double, double> function, VoxelStructure other)
        {
            var voxelKeys = _voxelStructure.Keys;

            foreach (var key in voxelKeys)
            {
                if (_voxelStructure.TryGetValue(key, out var voxelValue1) && other._voxelStructure.TryGetValue(key, out var voxelValue2))
                {
                    if (voxelValue1.HasValue && voxelValue2.HasValue)
                    {
                        voxelValue1.Value = function(voxelValue1.Value.Value, voxelValue2.Value.Value);
                    }
                }
            }
        }

        #endregion
        #region Private Methods

        /// <summary>
        /// Sets the raw data lists to null to free memory.
        /// </summary>
        private void SetNullStoredRawData()
        {
            var keys = _voxelStructure.Keys.ToList();

            foreach (var voxel in keys)
            {
                if (_voxelStructure[voxel].ScalarData != null)
                    _voxelStructure[voxel].ScalarData = null;
            }
        }

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
        /// Traverse the voxels to get the minimum and maximum indices 
        /// before converting those to world space. 
        /// </summary>
        /// <returns>A tuple of tuples representing the min and max 
        /// corners of the bounding box in world space.</returns>
        private (
            (double minx, double miny, double minz),
            (double maxx, double maxy, double maxz))
            GetWorldBoundingBox()
        {
            var vox_bbox = GetVoxelBoundingBox();

            (int x, int y, int z) minVoxel = vox_bbox.Item1;
            (int x, int y, int z) maxVoxel = vox_bbox.Item2;

            var minWorld = VoxelToWorld(minVoxel.x, minVoxel.y, minVoxel.z);
            var maxWorld = VoxelToWorld(maxVoxel.x, maxVoxel.y, maxVoxel.z);

            return (minWorld, maxWorld);
        }

        /// <summary>
        /// Returns the min and max voxel index coordinates of the structure. 
        /// Traverses the voxel field to do so.
        /// </summary>
        /// <returns></returns>
        private (
            (int minx, int miny, int minz),
            (int maxx, int maxy, int maxz))
            GetVoxelBoundingBox()
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

            return (minVoxel, maxVoxel);
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
        /// Calculates the scalar value for the specified voxel using the aggregationMethod and aggregationRadius given.
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
                                    aggregatedData.Add(value);
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

        private double CalculateGetisOrd(int x, int y, int z, int getisRadius)
        {
            return CalculateGetisOrd(x, y, z, getisRadius, Mean, StandardDeviation);
        }


        /// <summary>
        /// Calculates the Getis-ord Gi spatial correlation for the specified voxel. 
        /// </summary>
        /// <param name="x">Voxel space x coordinate.</param>
        /// <param name="y">Voxel space y coordinate.</param>
        /// <param name="z">Voxel space z coordinate.</param>
        /// <param name="getisRadius">Voxel radius for calculation.</param>
        /// <returns></returns>
        private double CalculateGetisOrd(int x, int y, int z, int getisRadius, double mean, double standardDeviation)
        {
            double sumWeights = 0;

            List<double> values = new List<double>();
            List<double> weights = new List<double>();

            for (int ix = x - getisRadius; ix <= x + getisRadius; ix++)
            {
                for (int iy = y - getisRadius; iy <= y + getisRadius; iy++)
                {
                    for (int iz = z - getisRadius; iz <= z + getisRadius; iz++)
                    {
                        // Ignore central value
                        if (ix == x && iy == y && iz == z) continue;
                        if (_voxelStructure.TryGetValue((ix, iy, iz), out var voxelValue) && voxelValue.HasValue)
                        {
                            double weight = 1;
                            weights.Add(weight);
                            values.Add(voxelValue.Value.Value);
                        }
                    }
                }
            }

            if (weights.Count < 2 || StandardDeviation == 0) return 0;

            sumWeights = weights.Sum();

            double weightedSum = 0;
            double sumWeights2 = 0;

            for (int i = 0; i < values.Count; i++)
            {
                weightedSum += values[i] * weights[i];
                sumWeights2 += weights[i] * weights[i];
            }

            var numerator = weightedSum - (mean * sumWeights);
            var denominator = standardDeviation *
                Math.Sqrt((Count * sumWeights2 - (sumWeights * sumWeights)) / (Count - 1));

            double go = numerator / denominator;

            return go;
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

        private Matrix4x4 _transformationMatrix = new Matrix4x4();

        #endregion
    }
}
