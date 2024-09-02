using CsvHelper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Classes;
using IPMSpatialAnalysis.Goo;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPMSpatialAnalysis.Components
{
    public class VoxeliseData : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the VoxeliseData class.
        /// </summary>
        public VoxeliseData()
          : base("VoxeliseData", "VoxeliseData",
              "Reads in IPM data from a csv filen and constructs the voxel data structure.",
              "IPMSpatialAnalysis", "Read")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Input", "IN", "File paths to read.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Layer Height", "LH", "Layer Height", GH_ParamAccess.item, 0.05);
            pManager.AddIntegerParameter("Point Read Interval", "PR", "Sets the point reading frequency. Use higher numbers for downsampling, or 1 for full sampling.", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Voxel Size", "VS", "The output voxel size.", GH_ParamAccess.item, 1);

            var iMethod = pManager.AddIntegerParameter("Method", "ME",
                "Method to use for extracting the feature. 0 = Mean, 1 = Standard Dev, 2 = Skewness, 3 = Count.", GH_ParamAccess.item, 1);
            var methodParam = pManager[iMethod] as Param_Integer;
            methodParam.AddNamedValue("Mean", 0);
            methodParam.AddNamedValue("Std. Dev.", 1);
            methodParam.AddNamedValue("Skewness", 2);
            methodParam.AddNamedValue("Count", 3);

            pManager.AddIntegerParameter("Extraction Radius", "ER", "Determines the number of layers of voxels used to calculate the statistics.", GH_ParamAccess.item, 1);

            pManager.AddBooleanParameter("Reset", "R", "Reread all data.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxels", "V", "Output voxel structure.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_String> fileTree = new GH_Structure<GH_String>();

            double layerHeight = double.MinValue;
            double voxelSize = double.MinValue;

            int pointReadInterval = 0;
            int method = 0;
            int aggregationRadius = 0;
            bool rereadData = false;

            Interval valueRange = new Interval(0, 1);

            if (!DA.GetDataTree("Input", out fileTree)) return;
            if (!DA.GetData("Layer Height", ref layerHeight)) return;
            if (!DA.GetData("Point Read Interval", ref pointReadInterval)) return;
            if (!DA.GetData("Voxel Size", ref voxelSize)) return;
            if (!DA.GetData("Method", ref method)) return;
            if (!DA.GetData("Extraction Radius", ref aggregationRadius)) return;
            if (!DA.GetData("Reset", ref rereadData)) return;

            bool reset = false;

            // Handle weird inputs
            pointReadInterval = Math.Max(pointReadInterval, 1);

            if (pointReadInterval != _pointReadInterval) reset = true;
            _pointReadInterval = pointReadInterval;

            voxelSize = Math.Max(voxelSize, 0.01);
            aggregationRadius = Math.Max(aggregationRadius, 0);

            if (voxelSize != _voxelSize) reset = true;
            _voxelSize = voxelSize;

            layerHeight = Math.Max(layerHeight, 0.01);
            if (layerHeight != _layerThickness) reset = true;
            _layerThickness = layerHeight;

            string xColumnName = "x";
            string yColumnName = "y";
            string pdColumnName = "photodiode";

            if (IsFileTreeChanged(fileTree) || reset || rereadData)
            {
                _voxelStructure = new Dictionary<GH_Path, VoxelStructure>();

                foreach (var path in fileTree.Paths)
                {
                    VoxelStructure voxelData = new VoxelStructure(voxelSize);

                    // Get the list of filepaths to read
                    List<string> filePaths = fileTree[path].Select(x => x.Value).ToList();

                    if (filePaths.Count > 0)
                    {
                        List<(double, int)> layers = GetLayers(filePaths, _layerThickness);

                        // Sort the layers by height
                        layers.Sort((a, b) => a.Item1.CompareTo(b.Item1));

                        // Set the buffer size
                        int blockSize = 10000;

                        Parallel.ForEach(layers, file =>
                        {
                            using (StreamReader reader = new StreamReader(File.OpenRead(filePaths[file.Item2])))
                            {
                                var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                                csv.Read();
                                csv.ReadHeader();

                                int counter = 0;
                                double travelledDistance = double.MaxValue;
                                Point2d previousPoint = new Point2d();

                                (double x, double y, double z, double scalar)[] pointsBlock = new (double x, double y, double z, double scalar)[blockSize];
                                int blockIndex = 0;

                                while (csv.Read())
                                {
                                    if (counter % _pointReadInterval == 0)
                                    {
                                        double photodiodeReading = csv.GetField<double>(pdColumnName);
                                        double x = csv.GetField<double>(xColumnName);
                                        double y = csv.GetField<double>(yColumnName);

                                        Point2d currentPoint = new Point2d(x, y);

                                        pointsBlock[blockIndex++] = (x, y, file.Item1, photodiodeReading);

                                        if (blockIndex >= blockSize)
                                        {
                                            voxelData.AddPoints(pointsBlock);
                                            blockIndex = 0;
                                        }
                                        previousPoint = currentPoint;
                                    }
                                    counter++;
                                }
                                if (blockIndex > 0)
                                {
                                    voxelData.AddPoints(pointsBlock);
                                }
                            }
                        });
                    }
                    voxelData.PruneVoxels(3);
                    _voxelStructure.Add(path, voxelData);
                }
                _files = fileTree.Duplicate();
            }

            Utilities.AggregationMethod agg;
            switch (method)
            {
                case 0: agg = Utilities.AggregationMethod.Mean; break;
                case 1: agg = Utilities.AggregationMethod.StandardDeviation; break;
                case 2: agg = Utilities.AggregationMethod.Skewness; break;
                case 3: agg = Utilities.AggregationMethod.Count; break;
                default: agg = Utilities.AggregationMethod.StandardDeviation; break;
            }

            var outTree = new GH_Structure<VoxelGoo>();

            foreach (var key in _voxelStructure.Keys)
            {
                outTree.Insert(new VoxelGoo(_voxelStructure[key], agg, aggregationRadius), key, 0);
            }

            DA.SetDataTree(0, outTree);
        }

        /// <summary>
        /// Runs some basic checks to see if the file input has been changed. This determines whether to reread all the data
        /// or not. If we just want to change the aggregation method for example, it is not necessary to reread everything. 
        /// 
        /// NOTE: This does not detect directory changes, so if the list of filenames is the same but they are in a different
        /// directory (such as chopped up layers), this function will return false. 
        /// 
        /// </summary>
        /// <param name="fileTree"></param>
        /// <returns></returns>
        private bool IsFileTreeChanged(GH_Structure<GH_String> fileTree)
        {
            if (_files.IsEmpty || _files == null) return true;

            if (fileTree.Branches.Count != _files.Branches.Count) return true;

            if (fileTree.DataCount != _files.DataCount) return true;

            var oldPaths = _files.Paths.ToList();
            var newPaths = fileTree.Paths.ToList();

            for (int i = 0; i < newPaths.Count; i++)
            {
                if (!newPaths[i].Indices.SequenceEqual(oldPaths[i].Indices)) return true;
            }

            return false;
        }

        /// <summary>
        /// Reads the filepaths given and maps the index of the file with a layer height.
        /// </summary>
        /// <param name="filepaths">List of filepaths</param>
        /// <param name="layerThickness">Layer thickness</param>
        /// <returns>List of tuples containing the layer height and file index.</returns>
        private List<(double, int)> GetLayers(List<string> filePaths, double layerThickness)
        {
            List<(double, int)> layers = new List<(double, int)>();

            for (int i = 0; i < filePaths.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePaths[i]);
                Match match = Regex.Match(fileName, @"(\d+(\.\d+)?)");
                if (match.Success)
                {
                    double z = double.Parse(match.Value);
                    layers.Add((z, i));
                }
            }

            return layers;
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
            get { return new Guid("189AB937-0D0B-4EA1-B122-C7D2C80233FC"); }
        }

        #region Fields

        private double _layerThickness = 0.05f;
        private GH_Structure<GH_String> _files = new GH_Structure<GH_String>();
        private GH_Structure<VoxelGoo> _voxelGoo;
        private Dictionary<GH_Path, VoxelStructure> _voxelStructure;

        private const double _samplingRate = 100000;

        private double _voxelSize = 1;
        private int _pointReadInterval = 10; 

        #endregion
    }
}