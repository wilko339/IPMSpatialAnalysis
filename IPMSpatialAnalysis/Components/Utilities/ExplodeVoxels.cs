using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPMSpatialAnalysis.Components.Utilities
{
    public class ExplodeVoxels : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExplodeVoxels class.
        /// </summary>
        public ExplodeVoxels()
          : base("ExplodeVoxels", "ExplodeVoxels",
              "Extracts the centre coordinates and scalar values of the voxels in the structure.",
              "IPMSpatialAnalysis", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data", "VD", "Input voxel data to filter.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Voxel Centres", "P", "Voxel centre points.", GH_ParamAccess.tree);
            pManager.AddPointParameter("Voxel Indices", "I", "Voxel indices", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Voxel Values", "V", "Voxel values", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<VoxelGoo> voxelGooStructure = new GH_Structure<VoxelGoo>();
            if (!DA.GetDataTree(0, out voxelGooStructure)) return;

            GH_Structure<GH_Point> outPoints = new GH_Structure<GH_Point>();
            GH_Structure<GH_Point> outIndices = new GH_Structure<GH_Point>();
            GH_Structure<GH_Number> outValues = new GH_Structure<GH_Number>();

            foreach (GH_Path path in voxelGooStructure.Paths)
            {
                foreach (VoxelGoo goo in voxelGooStructure[path])
                {
                    if (goo == null) continue;
                            outValues.AppendRange(goo.VoxelScalars.Select(n => new GH_Number(n)), path);
                    //outPoints.AppendRange(goo.WorldCoords.Select(p => new GH_Point(new Point3d(p.x, p.y, p.z))), path);
                    outIndices.AppendRange(goo.VoxelCoords.Select(p => new GH_Point(new Point3d(p.x, p.y, p.z))), path);
                }
            }

            DA.SetDataTree(0, outPoints);
            DA.SetDataTree(1, outIndices);
            DA.SetDataTree(2, outValues);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.explode;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DBF42486-F62F-4E89-9F69-791968083355"); }
        }
    }
}