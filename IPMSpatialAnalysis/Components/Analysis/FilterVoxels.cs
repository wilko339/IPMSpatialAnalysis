﻿using Grasshopper.Kernel;
using IPMSpatialAnalysis.Goo;
using System;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class FilterVoxels : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FilterVoxels class.
        /// </summary>
        public FilterVoxels()
          : base("FilterVoxels", "FilterVoxels",
              "Filters a voxel structure using a range. Also has an optional flag to remove zero valued voxels.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxel Data", "V", "Input voxel data to filter.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Value", "Min", "Minimum scalar bound.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max Value", "Max", "Maximum scalar bound.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Remove Zeros", "Z", "Boolean flag to remove or keep zero voxels", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Normalise", "N", "Whether or not to normalise the output data using z scaling before filtering.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Filtered Voxel Data", "V", "Filtered voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            double minFilterValue = double.MinValue;
            double maxFilterValue = double.MaxValue;
            bool normalise = false;
            bool removeZeros = false;

            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;
            if (!DA.GetData("Min Value", ref minFilterValue)) return;
            if (!DA.GetData("Max Value", ref maxFilterValue)) return;
            if (!DA.GetData("Normalise", ref normalise)) return;
            if (!DA.GetData("Remove Zeros", ref removeZeros)) return;

            VoxelGoo filteredGoo = new VoxelGoo(voxelGoo);
            if (normalise) filteredGoo.Normalise();

            filteredGoo.Filter(minFilterValue, maxFilterValue, removeZeros);

            DA.SetData(0, filteredGoo);
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
            get { return new Guid("C077BAD3-001B-45A4-9423-D9BBD241C7C1"); }
        }
    }
}