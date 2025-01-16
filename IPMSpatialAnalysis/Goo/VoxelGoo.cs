using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Classes;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Goo
{
    /// <summary>
    /// This class is the Goo interface between the backend VoxelStructure and Grasshopper.
    /// </summary>
    public class VoxelGoo : GH_GeometricGoo<VoxelStructure>, IGH_PreviewData, IGH_Goo
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
        /// The voxel coordinates of the underlying VoxelStructure as a list.
        /// </summary>
        public List<(double x, double y, double z)> WorldCoords
        {
            get
            {
                if (VoxelScalars.Count < 1) return new List<(double x, double y, double z)>();

                return Value.VoxelScalars
                    .OrderBy(kvp => kvp.Key.Item1)
                    .ThenBy(kvp => kvp.Key.Item2)
                    .ThenBy(kvp => kvp.Key.Item3)
                    .Select(kvp => Value.VoxelToWorld(kvp.Key))
                    .ToList();
            }
        }

        public List<(int x, int y, int z)> VoxelCoords
        {
            get
            {
                if (VoxelScalars.Count < 1) return new List<(int x, int y, int z)>();

                return Value.VoxelScalars
                    .OrderBy(kvp => kvp.Key.Item1)
                    .ThenBy(kvp => kvp.Key.Item2)
                    .ThenBy(kvp => kvp.Key.Item3)
                    .Select(kvp => kvp.Key)
                    .ToList();
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
            _aggregationMethod = aggregation;

            // This is expensive since the whole voxel structure is traversed.
            Value.CalculateVoxelData(_aggregationMethod, aggregationRadius);

            //UpdatePointCloud();
        }

        public VoxelGoo(VoxelStructure voxelStructure)
        {
            this.Value = voxelStructure;
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
        /// Draws the point cloud bounding box.
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
            var transformedGoo = new VoxelGoo(this);

            if (xform.IsValid)
            {
                Matrix4x4 transformationMatrix = new Matrix4x4(
                    xform.M00, xform.M01, xform.M02, xform.M03,
                    xform.M10, xform.M11, xform.M12, xform.M13,
                    xform.M20, xform.M21, xform.M22, xform.M23,
                    xform.M30, xform.M31, xform.M32, xform.M33);

                transformedGoo.Value.SetTransformation(transformationMatrix);
                //transformedGoo.UpdatePointCloud();
            }
            return transformedGoo;
        }
        #endregion
        #region Other Methods

        /// <summary>
        /// Calls the normalisation of the underlying voxel structure.
        /// </summary>
        public void Normalise()
        {
            Value.NormaliseData();
        }

        public void MaxMinNormalise()
        {
            Value.MaxMinNormaliseData();
        }

        public void MaxMinNormalise(double min, double max)
        {
            Value.MaxMinNormaliseData(min, max);
        }

        public void Remap(double outputMin, double outputMax)
        {
            Value.RemapVoxelValues(outputMin, outputMax);
        }

        public void Remap(double inputMin, double inputMax, double outputMin, double outputMax)
        {
            Value.RemapVoxelValues(inputMin, inputMax, outputMin, outputMax);
        }

        public void Clamp(double min, double max)
        {
            Value.ClampVoxelValues(min, max);
        }

        public void Sigmoid(double factor)
        {
            Value.Sigmoid(factor);
        }

        /// <summary>
        /// Calls the column normalisation method of the underlying voxel structure.
        /// </summary>
        public void ColumnNormalise()
        {
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

        public void CalculateGetisOrd(int radius, double globalMean, double globalStandardDeviation)
        {
            Value.CalculateSpatialCorrelation(radius, globalMean, globalStandardDeviation);
        }

        public void RunCustomFunction(Func<double, double> function)
        {
            Value.ExecuteCustomFunction(function);
        }

        public void RunCustomFunction(Func<double, double, double> function, VoxelGoo other)
        {
            Value.ExecuteCustomFunction(function, other.Value);
        }

        #endregion
        #region Fields

        private BoundingBox _boundingBox;

        private Utilities.AggregationMethod _aggregationMethod;

        #endregion
    }
}
