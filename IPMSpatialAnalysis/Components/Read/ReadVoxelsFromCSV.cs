using System;
using System.IO;
using CsvHelper;
using Grasshopper.Kernel;
using System.Globalization;
using IPMSpatialAnalysis.Classes;
using IPMSpatialAnalysis.Goo;

namespace IPMSpatialAnalysis.Components.Read
{
    public class ReadVoxelsFromCSV : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ReadVoxelsFromCSV class.
        /// </summary>
        public ReadVoxelsFromCSV()
          : base("ReadVoxelsFromCSV", "ReadVoxelsFromCSV",
              "Reads voxel structure from a CSV.",
              "IPMSpatialAnalysis", "IO")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Filepath", "F", "CSV file with voxel data", GH_ParamAccess.item);
            pManager.AddNumberParameter("Voxel Size", "S", "Voxel size.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("VoxelData", "VD", "Voxel data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filepath = "";
            double voxelSize = 0;

            if (!DA.GetData(0, ref filepath)) return;
            if (!DA.GetData(1, ref voxelSize)) return;

            VoxelStructure voxelStructure = new VoxelStructure(voxelSize);

            using (StreamReader reader = new StreamReader(File.OpenRead(filepath)))
            {
                var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    int x = csv.GetField<int>("x");
                    int y = csv.GetField<int>("y");
                    int z = csv.GetField<int>("z");
                    double value = csv.GetField<double>("value");

                    voxelStructure.AddVoxelValue((x, y, z), value);
                }
            }

            voxelStructure.UpdateStatistics();

            VoxelGoo outGoo = new VoxelGoo(voxelStructure);

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
            get { return new Guid("B84AC9E5-94FC-47F4-BDC5-888A30A3B76F"); }
        }
    }
}