using Grasshopper.Kernel;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IPMSpatialAnalysis.Components.Read
{
    public class VoxelisePores : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the VoxelisePores class.
        /// </summary>
        public VoxelisePores()
          : base("VoxelisePores", "VoxelisePores",
              "Voxelises a list of pore meshes using inverse exponential distance weighting.",
              "IPMSpatialAnalysis", "Read")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("PoreMeshes", "P", "List of meshes representing pores.", GH_ParamAccess.list);
            pManager.AddGenericParameter("VoxelData", "V", "Existing voxel grid to use as template.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("KClosestPoints", "K", "Number of closest points for porosity calculation.", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("VoxelData", "VD", "Output pore voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> poreMeshes = new List<Mesh>();
            VoxelGoo existingVoxels = new VoxelGoo();
            int kClosestPoints = 1;

            if (!DA.GetDataList("PoreMeshes", poreMeshes)) return;
            if (!DA.GetData("VoxelData", ref existingVoxels)) return;
            if (!DA.GetData("KClosestPoints", ref kClosestPoints)) return;

            if (poreMeshes.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No input meshes.");
                return;
            }

            var newGoo = new VoxelGoo(existingVoxels);

            List<(double, double, double, double)> poreData = new List<(double, double, double, double)>();
            newGoo.Value.SetVoxelDataValues(0);
            newGoo.UpdatePointCloud();

            var voxelCentres = newGoo.VoxelCoords
                .Select(p => new Point3d(p.x, p.y, p.z))
                .ToList();

            var validPoreMeshes = FixPoreMeshes(poreMeshes);
            var poreCentres = validPoreMeshes
                .Select(pore => pore.GetBoundingBox(true).Center).ToList();

            if (poreCentres.Count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid pore meshes.");
                return;
            }

            int voxelCounter = 0;

            var closestKPoints = RTree.Point3dKNeighbors(poreCentres, voxelCentres, kClosestPoints);

            try
            {
                foreach (var pores in closestKPoints)
                {
                    if (pores.Length > 0)
                    {
                        double inverseDistanceSum = 0;
                        var voxelCentre = voxelCentres[voxelCounter];

                        foreach (var poreIndex in pores)
                        {
                            double distance = voxelCentre.DistanceTo(poreCentres[poreIndex]);

                            inverseDistanceSum += Math.Exp(-distance);
                        }
                        var voxelKey = newGoo.Value.WorldToVoxel(voxelCentre.X, voxelCentre.Y, voxelCentre.Z);
                        newGoo.Value.SetVoxelDataValue(voxelKey, inverseDistanceSum);
                    }
                    voxelCounter++;
                }
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                return;
            }
            
            newGoo.Value.UpdateStatistics();
            newGoo.UpdatePointCloud();
            DA.SetData(0, newGoo);
        }

        // Helper function to fix some common issues with the pore meshes.
        private List<Mesh> FixPoreMeshes(List<Mesh> poreMeshes)
        {
            var validMeshes = new List<Mesh>();

            if (poreMeshes.Count < 1) return validMeshes;

            foreach (var mesh in poreMeshes)
            {
                if (mesh == null) continue;
                if (mesh.IsClosed && mesh.Volume() > 0)
                {
                    validMeshes.Add(mesh);
                    continue;
                }
                else
                // Do some fixing
                {
                    mesh.Faces.CullDegenerateFaces();
                    mesh.RebuildNormals();
                    mesh.UnifyNormals();
                    mesh.Normals.ComputeNormals();
                    mesh.Vertices.CullUnused();

                    if (mesh.IsClosed && mesh.Volume() > 0)
                    {
                        validMeshes.Add(mesh);
                        continue;
                    }
                }
            }

            return validMeshes;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.voxelisePores;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2030EBF9-D6DE-4965-9F45-A938A5915BA1"); }
        }
    }
}