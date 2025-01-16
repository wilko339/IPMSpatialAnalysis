using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using MathNet.Numerics.Integration;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class Sigmoid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Sigmoid class.
        /// </summary>
        public Sigmoid()
          : base("Sigmoid", "Sigmoid",
              "Applied the logistic sigmoid function to the voxel values. The input voxel values should first be normalised to the range -1 to 1.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Input voxel data to apply the sigmoid function to.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Slope", "S", "Slope of the sigmoid function.", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Voxel data with sigmoid function applied.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            double slope = 1.0;

            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;
            if (!DA.GetData("Slope", ref slope)) return;

            VoxelGoo sigmoidGoo = new VoxelGoo(voxelGoo);
            sigmoidGoo.Sigmoid(slope);

            DA.SetData(0, sigmoidGoo);
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
            get { return new Guid("96227413-13B9-4E42-A8D8-73ADFA53C915"); }
        }
    }
}