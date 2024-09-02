using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class SpatialCorrelation : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SpatialCorrelation class.
        /// </summary>
        public SpatialCorrelation()
          : base("SpatialCorrelation", "SpatialCorrelation",
              "Calculates the spatial correlation for the voxel structure.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxel Data", "VD", "Input voxel data.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Voxel Radius", "R", "Radius of voxels to use in the calculation.", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxel Data", "V", "Getis-Ord voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            int voxelRadius = 0;
            double decay = 0;

            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;
            if (!DA.GetData("Voxel Radius", ref voxelRadius)) return;

            VoxelGoo newGoo = new VoxelGoo(voxelGoo);
            newGoo.CalculateGetisOrd(voxelRadius);

            DA.SetData(0, newGoo);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.spatialCorrelation;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DE015450-2C2B-4D79-AA59-E559F778C1E6"); }
        }
    }
}