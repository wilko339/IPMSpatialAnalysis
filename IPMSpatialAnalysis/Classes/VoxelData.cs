using System.Collections.Generic;

namespace IPMSpatialAnalysis.Classes
{
    internal class VoxelData
    {
        #region Properties

        public double? Value
        {
            get
            {
                if (HasValue) return _value.Value;
                return null;
            }

            set
            {
                _value = value;
            }
        }

        public List<double> ScalarData
        {
            get
            {
                return _scalarData;
            }

            set
            {
                _scalarData = value;
            }
        }

        public int Count
        {
            get
            {
                return _scalarData.Count;
            }
        }
        public bool HasValue
        {
            get
            {
                return _value != null;
            }
        }

        #endregion
        #region Constructors

        public VoxelData()
        {
            _scalarData = new List<double>();
            _value = null;
        }

        public VoxelData(List<double> scalarData, double value)
        {
            _scalarData = scalarData;
            _value = value;
        }

        public VoxelData(double value)
        {
            _value = value; 
            _scalarData = new List<double>();
        }

        public VoxelData(VoxelData voxelData)
        {
            _value = voxelData.Value;
            if (voxelData.ScalarData != null)
            {
                _scalarData = new List<double>(voxelData.ScalarData);
            }
            else _scalarData = new List<double>();
        }

        #endregion
        #region Public Methods

        public void Add(double value)
        {
            if (!double.IsNaN(value))
            {
                _scalarData.Add(value);
            }
        }

        #endregion
        #region Fields

        private List<double> _scalarData;
        private double? _value;
        
        #endregion
    }
}
