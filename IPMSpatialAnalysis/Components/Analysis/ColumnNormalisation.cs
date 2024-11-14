using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class ColumnNormalisation : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ColumnNormalisation class.
        /// </summary>
        public ColumnNormalisation()
          : base("ColumnNormalisation", "ColumnNormalisation",
              "Normalises the voxels based on their XY coordinate by calculating the mean and standard deviation of each vertical column of data.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Input voxel data to column normalise.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Column-normalised voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            if (!DA.GetData(0, ref voxelGoo)) return;

            VoxelGoo normalisedGoo = new VoxelGoo(voxelGoo);
            normalisedGoo.ColumnNormalise();

            DA.SetData(0, normalisedGoo);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.col_norm;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3B304C66-7363-412C-8492-F73D9306D4BB"); }
        }
    }
}