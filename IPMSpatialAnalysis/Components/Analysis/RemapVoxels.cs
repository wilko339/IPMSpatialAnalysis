using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class RemapVoxels : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RemapVoxels class.
        /// </summary>
        public RemapVoxels()
          : base("RemapVoxels", "RemapVoxels",
              "Remaps voxel values to the given range.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Input voxel data to remap.", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Input Range", "I", "Input data range.", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddIntervalParameter("Remap Range", "R", "Range to remap voxel values to.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Remapped voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            Interval inputInterval = new Interval();
            Interval remapInterval = new Interval();


            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;
            if (!DA.GetData("Remap Range", ref remapInterval)) return;

            VoxelGoo remappedGoo = new VoxelGoo(voxelGoo);

            if (!DA.GetData("Input Range", ref inputInterval))
            {
                remappedGoo.Remap(remapInterval.Min, remapInterval.Max);
                DA.SetData(0, remappedGoo);
            }
            else
            {
                remappedGoo.Remap(inputInterval.Min, inputInterval.Max, remapInterval.Min, remapInterval.Max);
                DA.SetData(0, remappedGoo);
            }
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
            get { return new Guid("5A7CAF8D-FED9-4702-8114-67189556EFB4"); }
        }
    }
}