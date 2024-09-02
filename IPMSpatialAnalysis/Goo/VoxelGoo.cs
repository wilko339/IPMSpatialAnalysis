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
        public override BoundingBox Boundingbox => throw new NotImplementedException();

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

        public VoxelGoo()
        {
            this.Value = new VoxelStructure();
        }
        public VoxelGoo(VoxelGoo voxelGoo)
        {
            this.Value = new VoxelStructure(voxelGoo.Value);

            _aggregationMethod = voxelGoo._aggregationMethod;
            _previewCloud = new PointCloud(voxelGoo.PreviewCloud);

            //UpdatePointCloud();
        }

        public VoxelGoo(VoxelStructure voxelData, Utilities.AggregationMethod aggregation, int aggregationRadius)
        {
            this.Value = voxelData;
            _previewCloud = new PointCloud();
            _aggregationMethod = aggregation;

            // This is expensive since the whole voxel structure is traversed.
            Value.CalculateVoxelData(_aggregationMethod, aggregationRadius);

            UpdatePointCloud();
        }

        public VoxelGoo(VoxelStructure voxelData)
        {
            this.Value = voxelData;
            _previewCloud = new PointCloud();

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
            throw new NotImplementedException();
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region Other Methods

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

        #endregion
        #region Fields

        private BoundingBox _boundingBox;
        private PointCloud _previewCloud;

        private Utilities.AggregationMethod _aggregationMethod;

        #endregion
    }
}
