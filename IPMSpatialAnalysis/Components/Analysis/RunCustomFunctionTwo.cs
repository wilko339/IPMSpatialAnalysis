using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Goo;
using System;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class RunCustomFunctionTwo : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RunCustomFunctionTwo class.
        /// </summary>
        public RunCustomFunctionTwo()
          : base("RunCustomFunctionTwo", "CustomFunction",
              "Runs a custom delegate function for each pair of corresponding voxel values in two structures.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxel Data 1", "V", "Input voxel data 1.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Voxel Data 2", "V", "Input voxel data 2.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Function", "F", "Function of the form Func<float, float, float>.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxel Data", "V", "Output voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo1 = new VoxelGoo();
            VoxelGoo voxelGoo2 = new VoxelGoo();
            GH_ObjectWrapper func = null;

            if (!DA.GetData("Voxel Data 1", ref voxelGoo1)) return;
            if (!DA.GetData("Voxel Data 2", ref voxelGoo2)) return;
            if (!DA.GetData("Function", ref func)) return;

            VoxelGoo outGoo = new VoxelGoo(voxelGoo1);
            outGoo.RunCustomFunction((Func<double, double, double>)func.Value, voxelGoo2);

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
            get { return new Guid("1AE42F17-098E-4AE2-8BCC-C5A2DA9378E2"); }
        }
    }
}