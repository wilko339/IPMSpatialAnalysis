using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Classes;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IPMSpatialAnalysis.Goo
{
    /// <summary>
    /// This class is the Goo interface between the backend VoxelStructure and Grasshopper.
    /// </summary>
    public class VoxelGoo : GH_GeometricGoo<VoxelStructure>, IGH_PreviewData
    {
        #region Properties

        /// <summary>
        /// Clipping box for drawing.
        /// </summary>
        public BoundingBox ClippingBox => Boundingbox;

        /// <summary>
        /// Geometric bounding box.
        /// </summary>
        public override BoundingBox Boundingbox
        {
            get
            {
                ((double minx, double miny, double minz), (double maxx, double maxy, double maxz)) = Value.BoundingBox;
                _boundingBox = new BoundingBox(new Point3d(minx, miny, minz), new Point3d(maxx, maxy, maxz));

                // Check if box is planar in any dimension and if so, inflate a little
                if (_boundingBox.Diagonal.X == 0) _boundingBox.Inflate(0, 0, 0.001);
                if (_boundingBox.Diagonal.Y == 0) _boundingBox.Inflate(0, 0, 0.001);
                if (_boundingBox.Diagonal.Z == 0) _boundingBox.Inflate(0, 0, 0.001);

                return _boundingBox;
            }
        }

        public override string TypeName => "VoxelStructure";

        public override string TypeDescription => "Spatially organised voxel structure representing a dense scalar point cloud.";

        /// <summary>
        /// The PointCloud instance used for the preview in Rhino.
        /// </summary>
        public PointCloud PreviewCloud
        {
            get
            {
                return _previewCloud;
            }
        }

        /// <summary>
        /// The voxel coordinates of the underlying VoxelStructure as a list.
        /// </summary>
        public List<(double x, double y, double z)> VoxelCoords
        {
            get
            {
                if (VoxelScalars.Count < 1) return new List<(double x, double y, double z)>();
                return Value.VoxelScalars.Keys.Select(item => Value.VoxelToWorld(item)).ToList();
            }
        }
        public List<double> VoxelScalars
        {
            get
            {
                // Ensure a consistent ordering when returning the scalars.
                return Value.VoxelScalars
                    .OrderBy(kvp => kvp.Key.Item1)
                    .ThenBy(kvp => kvp.Key.Item2)
                    .ThenBy(kvp => kvp.Key.Item3)
                    .Select(kvp => kvp.Value)
                    .ToList();
            }
        }
        public double StandardDeviation => Value.StandardDeviation;
        public double Mean => Value.Mean;
        public int Count => Value.Count;
        public double Min => Value.Min;
        public double Max => Value.Max;
        #endregion
        #region Constructors

        /// <summary>
        /// Base constructor.
        /// </summary>
        public VoxelGoo()
        {
            this.Value = new VoxelStructure();
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="voxelGoo"></param>
        public VoxelGoo(VoxelGoo voxelGoo)
        {
            this.Value = new VoxelStructure(voxelGoo.Value);

            _aggregationMethod = voxelGoo._aggregationMethod;
            _previewCloud = new PointCloud(voxelGoo.PreviewCloud);
        }

        /// <summary>
        /// Constructor that wraps an existing VoxelStructure and calculates the voxel values. 
        /// </summary>
        /// <param name="voxelStructure"></param>
        /// <param name="aggregation"></param>
        /// <param name="aggregationRadius"></param>
        public VoxelGoo(VoxelStructure voxelStructure, Utilities.AggregationMethod aggregation, int aggregationRadius)
        {
            this.Value = voxelStructure;
            _previewCloud = new PointCloud();
            _aggregationMethod = aggregation;

            // This is expensive since the whole voxel structure is traversed.
            Value.CalculateVoxelData(_aggregationMethod, aggregationRadius);

            //UpdatePointCloud();
        }

        #endregion
        #region Grasshopper Methods

        /// <summary>
        /// Handles casting.
        /// </summary>
        /// <typeparam name="Q"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        public override bool CastTo<Q>(out Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(VoxelStructure)))
            {
                if (Value == null)
                {
                    target = default(Q);
                }
                else
                {
                    target = (Q)(object)Value;
                }
                return true;
            }

            target = default(Q);
            return false;
        }

        /// <summary>
        /// No mesh preview!
        /// </summary>
        /// <param name="args"></param>
        public void DrawViewportMeshes(GH_PreviewMeshArgs args) 
        {
            
        }

        /// <summary>
        /// Draws the point cloud preview using a small default point size.
        /// </summary>
        /// <param name="args"></param>
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawBox(Boundingbox, Color.DarkOliveGreen);
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new VoxelGoo(this);
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            return Boundingbox;
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{TypeName} with {Count} voxels";
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region Other Methods

        /// <summary>
        /// Updates the point cloud preview. 
        /// Call this any time the underlying voxel structure changes in some way.
        /// </summary>
        public void UpdatePointCloud()
        {
            _previewCloud = new PointCloud();

            if (Value.Count < 1) return;

            List<Point3d> points = new List<Point3d>();
            List<double> values = new List<double>();

            foreach (var voxel in Value.VoxelScalars)
            {
                (double x, double y, double z) worldPoint = Value.VoxelToWorld(voxel.Key);
                points.Add(new Point3d(worldPoint.x, worldPoint.y, worldPoint.z));
                values.Add(voxel.Value);
            }

            _previewCloud.AddRange(
                points,
                Enumerable.Repeat(new Vector3d(), points.Count),
                Enumerable.Repeat(Color.Black, points.Count),
                values);
        }

        /// <summary>
        /// Calls the normalisation of the underlying voxel structure.
        /// </summary>
        public void Normalise()
        {
            _previewCloud = new PointCloud();

            Value.NormaliseData();
        }

        /// <summary>
        /// Calls the column normalisation method of the underlying voxel structure.
        /// </summary>
        public void ColumnNormalise()
        {
            _previewCloud = new PointCloud();

            Value.ColumnNormaliseData();
            //UpdatePointCloud();
        }

        /// <summary>
        /// Filters the voxel field to remove voxels that are outside of the specified range.
        /// </summary>
        /// <param name="minFilterValue">Lower bound</param>
        /// <param name="maxFilterValue">Upper bound</param>
        /// <param name="filterZeros">Whether to remove voxels equal to 0</param>
        public void Filter(double minFilterValue, double maxFilterValue, bool filterZeros)
        {
            _previewCloud = new PointCloud();

            Value.FilterByScalarValues(minFilterValue, maxFilterValue, filterZeros);

            //UpdatePointCloud();
        }

        /// <summary>
        /// Calls the underlying voxel structure method to calculate the Getis-Ord spatial correlation.
        /// </summary>
        /// <param name="radius">Voxel radius for calculation.</param>
        public void CalculateGetisOrd(int radius)
        {
            Value.CalculateSpatialCorrelation(radius);
            //UpdatePointCloud();
        }

        public void RunCustomFunction(Func<double, double, double> function, VoxelGoo other)
        {
            Value.ExecuteCustomFunction(function, other.Value);
        }

        /// <summary>
        /// Updates the colours of the stored point cloud points for previewing using a
        /// provided scalar range.
        /// </summary>
        /// <param name="min">Lower scalar bound</param>
        /// <param name="max">Upper scalar bound</param>
        public void UpdatePointCloudColours(double min, double max)
        {
            if (_previewCloud == null) UpdatePointCloud();

            foreach (var pointCloudItem in _previewCloud)
            {
                double colourFactor = (pointCloudItem.PointValue - min) / (max - min);
                Color colour = Utilities.Lerp3(Color.Green, Color.Yellow, Color.Red, colourFactor);
                pointCloudItem.Color = colour;
            }
        }

        #endregion
        #region Fields

        private BoundingBox _boundingBox;
        private PointCloud _previewCloud;

        private Utilities.AggregationMethod _aggregationMethod;

        #endregion
    }
}
