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
                return Value.VoxelScalars.Values.ToList();
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

            UpdatePointCloud();
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
        public void DrawViewportMeshes(GH_PreviewMeshArgs args) { }

        /// <summary>
        /// Draws the point cloud preview using a small default point size.
        /// </summary>
        /// <param name="args"></param>
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPointCloud(PreviewCloud, 2);
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            throw new NotImplementedException();
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            throw new NotImplementedException();
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
        /// Calls the underlying voxel structure method to calculate the Getis-Ord spatial correlation.
        /// </summary>
        /// <param name="radius">Voxel radius for calculation.</param>
        public void CalculateGetisOrd(int radius)
        {
            Value.CalculateSpatialCorrelation(radius);
            UpdatePointCloud();
        }

        #endregion
        #region Fields

        private BoundingBox _boundingBox;
        private PointCloud _previewCloud;

        private Utilities.AggregationMethod _aggregationMethod;

        #endregion
    }
}
