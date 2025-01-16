using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class MaxMinNormaliseVoxels : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MaxMinNormaliseVoxels class.
        /// </summary>
        public MaxMinNormaliseVoxels()
          : base("MaxMinNormaliseVoxels", "MaxMinNormaliseVoxels",
              "Normalises voxel scalar values using max/min normalisation.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "V", "Input voxel data to column normalise.", GH_ParamAccess.item);
            pManager.AddIntervalParameter("Range", "R", "Range in which to normalise data.", GH_ParamAccess.item);
            pManager[1].Optional = true;
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
            Interval interval = new Interval();
            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;

            VoxelGoo normalisedGoo = new VoxelGoo(voxelGoo);

            bool range = DA.GetData(1, ref interval);

            if (!range)
            {
                normalisedGoo.MaxMinNormalise();
            }

            else
            {
                normalisedGoo.MaxMinNormalise(interval.Min, interval.Max);
            }

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
            get { return new Guid("7362988A-8811-48B7-9ED2-FB929DBF1CC9"); }
        }
    }
}