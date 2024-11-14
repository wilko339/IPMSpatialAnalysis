using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class NormaliseVoxels : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NormaliseVoxels class.
        /// </summary>
        public NormaliseVoxels()
          : base("NormaliseVoxels", "Normalise",
              "Normalises voxel scalar values using z-score normalisation.",
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
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Normalised voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;

            VoxelGoo normalisedGoo = new VoxelGoo(voxelGoo);
            normalisedGoo.Normalise();

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
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("92597EDD-6169-4520-9AED-40CC60B15008"); }
        }
    }
}