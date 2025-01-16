using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class RunCustomFunctionOne : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RunCustomFunctionOne class.
        /// </summary>
        public RunCustomFunctionOne()
          : base("RunCustomFunctionOne", "Nickname",
              "Runs a custom delegate function for each voxel in the voxel structure.",
              "IPMSpatialAnalysis", "Function")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Input voxel data.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Function", "F", "Function of the form Func<float, float>.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Output voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            GH_ObjectWrapper func = null;

            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;
            if (!DA.GetData("Function", ref func)) return;

            VoxelGoo outGoo = new VoxelGoo(voxelGoo);
            outGoo.RunCustomFunction((Func<double, double>)func.Value);

            DA.SetData(0, outGoo);
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
            get { return new Guid("22E99967-F258-4517-847F-DB935D32A545"); }
        }
    }
}