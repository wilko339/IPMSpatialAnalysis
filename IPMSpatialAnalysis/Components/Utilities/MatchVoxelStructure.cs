using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using IPMSpatialAnalysis.Classes;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis
{
    public class MatchVoxelStructure : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MatchVoxelStructure class.
        /// </summary>
        public MatchVoxelStructure()
          : base("MatchVoxelStructure", "MatchVoxelStructure",
              "Attempts to match the keys in the second voxel structure with the keys in the first. Removes extra keys, and adds new ones (but these will have no value).",
              "IPMSpatialAnalysis", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Structure A", "A", "Template structure.", GH_ParamAccess.item);
            pManager.AddParameter(new VoxelParam(), "Voxel Structure B", "B", "Voxel structure to update.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "VoxelData", "VD", "Updated voxel structure.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelStructure voxelA = null;
            VoxelStructure voxelB = null;
            if (!DA.GetData("Voxel Structure A", ref voxelA)) return;
            if (!DA.GetData("Voxel Structure B", ref voxelB)) return;

            // Create a copy of the template structure
            VoxelStructure outStructure = new VoxelStructure(voxelA);

            // Update the values in the copied structure with the values from voxelB

            foreach (var key in outStructure.VoxelScalars.Keys)
            {
                voxelB.GetVoxelDataValue(key, out double value);
                outStructure.SetVoxelDataValue(key, value);
            }

            // Match the structure of voxelB to that of voxelA
            DA.SetData(0, new VoxelGoo(outStructure));
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
            get { return new Guid("CBBB99F9-046D-4493-9075-FBAE5F0AC2F3"); }
        }
    }
}