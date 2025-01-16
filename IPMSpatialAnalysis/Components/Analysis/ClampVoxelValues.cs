using System;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class ClampVoxelValues : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ClampVoxelValues class.
        /// </summary>
        public ClampVoxelValues()
          : base("ClampVoxelValues", "ClampVoxelValues",
              "Clamps voxel values into a given interval.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Input voxel data to column normalise.", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Range", "R", "Range in which to clamp data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Clamped voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            Interval interval = new Interval();
            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;
            if(!DA.GetData(1, ref interval)) return;

            VoxelGoo clampedGoo = new VoxelGoo(voxelGoo);

            clampedGoo.Clamp(interval.Min, interval.Max);

            DA.SetData(0, clampedGoo);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CF8A45A7-02B1-452D-AE77-60198FCEFD99"); }
        }
    }
}