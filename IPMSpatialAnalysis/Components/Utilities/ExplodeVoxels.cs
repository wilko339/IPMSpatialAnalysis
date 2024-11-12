using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
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
            pManager.AddGenericParameter("Voxel Data", "VD", "Input voxel data to filter.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Voxel Centres", "P", "Voxel centre points.", GH_ParamAccess.list);
            pManager.AddPointParameter("Voxel Indices", "I", "Voxel indices", GH_ParamAccess.list);
            pManager.AddNumberParameter("Voxel Values", "V", "Voxel values", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo voxelGoo = new VoxelGoo();
            if (!DA.GetData("Voxel Data", ref voxelGoo)) return;

            List<GH_Point> outPoints = new List<GH_Point>();
            List<GH_Point> outIndices = new List<GH_Point>();
            List<double> outValues = new List<double>();

            outValues = voxelGoo.VoxelScalars;
            outPoints = voxelGoo.WorldCoords.Select(p => new GH_Point(new Point3d(p.x, p.y, p.z))).ToList();
            outIndices = voxelGoo.VoxelCoords.Select(p => new GH_Point(new Point3d(p.x, p.y, p.z))).ToList();

            DA.SetDataList(0, outPoints);
            DA.SetDataList(1, outIndices);
            DA.SetDataList(2, outValues);
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