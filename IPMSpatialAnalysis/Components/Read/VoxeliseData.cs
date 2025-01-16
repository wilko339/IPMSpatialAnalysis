using CsvHelper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Classes;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPMSpatialAnalysis.Components.Read
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
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Input", "I", "File paths to read.", GH_ParamAccess.tree);
            pManager.AddTextParameter("X Column Name", "X", "Name of column containing x position data.", GH_ParamAccess.item, "GalvoXActualCartesian");
            pManager.AddTextParameter("Y Column Name", "Y", "Name of column containing x position data.", GH_ParamAccess.item, "GalvoYActualCartesian");
            pManager.AddTextParameter("Data Column Name", "D", "Name of column containing measurement data.", GH_ParamAccess.item, "Photodiode1Normalised");
            pManager.AddNumberParameter("Layer Heights", "L", "The layer height corresponding to each file in the input filepaths.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Point Read Interval", "P", "Sets the point reading frequency. Use higher numbers for downsampling, or 1 for full sampling.", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Voxel Size", "V", "The output voxel size.", GH_ParamAccess.item, 1);

            pManager.AddIntervalParameter("Value Interval", "I", "Interval to specify range of acceptable scalar values.", GH_ParamAccess.item);
            pManager[7].Optional = true;

            var iMethod = pManager.AddIntegerParameter("Method", "M",
                "Method to use for extracting the feature. 0 = Mean, 1 = Standard Dev, 2 = Skewness, 3 = Count.", GH_ParamAccess.item, 1);
            var methodParam = pManager[iMethod] as Param_Integer;
            methodParam.AddNamedValue("Mean", 0);
            methodParam.AddNamedValue("Std. Dev.", 1);
            methodParam.AddNamedValue("Skewness", 2);
            methodParam.AddNamedValue("Count", 3);

            pManager.AddIntegerParameter("Extraction Radius", "E", "Determines the number of layers of voxels used to calculate the statistics.", GH_ParamAccess.item, 1);

            pManager.AddBooleanParameter("Reset", "R", "Reread all data.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxels", "V", "Output voxel structure.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_String> fileTree = new GH_Structure<GH_String>();

            string xColumnName = "";
            string yColumnName = "";
            string dataColumnName = "";

            GH_Structure<GH_Number> layerHeights = new GH_Structure<GH_Number>();
            double voxelSize = double.MinValue;

            int pointReadInterval = 0;
            int method = 0;
            int aggregationRadius = 0;
            bool rereadData = false;

            Interval valueRange = Interval.Unset;
            bool restrictValues = false;

            if (!DA.GetDataTree("Input", out fileTree)) return;
            if (!DA.GetData(1, ref xColumnName)) return;
            if (!DA.GetData(2, ref yColumnName)) return;
            if (!DA.GetData(3, ref dataColumnName)) return;
            if (!DA.GetDataTree("Layer Heights", out layerHeights)) return;
            if (!DA.GetData("Point Read Interval", ref pointReadInterval)) return;
            if (!DA.GetData("Voxel Size", ref voxelSize)) return;
            if (DA.GetData("Value Interval", ref valueRange)) restrictValues = true;
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

            if (!CheckTreeEquality(fileTree, _files) || reset || rereadData)
            {
                _voxelStructure = new Dictionary<GH_Path, VoxelStructure>();

                foreach (var path in fileTree.Paths)
                {
                    VoxelStructure voxelData = new VoxelStructure(voxelSize);

                    // Get the list of filepaths and corresponding layer heights to read
                    List<string> filePaths = fileTree[path].Select(x => x.Value).ToList();
                    List<double> heights = layerHeights[path].Select(x => x.Value).ToList();

                    if (filePaths.Count > 0)
                    {
                        // Set the buffer size
                        int blockSize = 10000;

                        Parallel.For(0, 
                            filePaths.Count,
                            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2},
                            i =>
                        {
                            using (StreamReader reader = new StreamReader(File.OpenRead(filePaths[i])))
                            {
                                var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                                csv.Read();
                                csv.ReadHeader();

                                int dataColumnIndex = Array.IndexOf(csv.HeaderRecord, dataColumnName);
                                int xColumnIndex = Array.IndexOf(csv.HeaderRecord, xColumnName);
                                int yColumnIndex = Array.IndexOf(csv.HeaderRecord, yColumnName);

                                int counter = 0;

                                (double x, double y, double z, double scalar)[] pointsBlock = new (double x, double y, double z, double scalar)[blockSize];
                                int blockIndex = 0;

                                while (csv.Read())
                                {
                                    if (counter % _pointReadInterval == 0)
                                    {
                                        double photodiodeReading = double.Parse(csv.GetField(dataColumnIndex));

                                        if (restrictValues)
                                        {
                                            if (!valueRange.IncludesParameter(photodiodeReading)) continue;
                                        }

                                        double x = double.Parse(csv.GetField(xColumnIndex));
                                        double y = double.Parse(csv.GetField(yColumnIndex));

                                        pointsBlock[blockIndex++] = (x, y, heights[i], photodiodeReading);

                                        if (blockIndex >= blockSize)
                                        {
                                            voxelData.AddPoints(pointsBlock);
                                            blockIndex = 0;
                                        }
                                    }
                                    counter++;
                                }
                                if (blockIndex > 0)
                                {
                                    // Don't allow unused zero points to be added.
                                    voxelData.AddPoints(pointsBlock.Take(blockIndex));
                                }
                            }
                        });
                    }
                    voxelData.PruneVoxels(5);
                    _voxelStructure.Add(path, voxelData);
                }
                _files = fileTree.Duplicate();
            }

            Classes.Utilities.AggregationMethod agg;
            switch (method)
            {
                case 0: agg = Classes.Utilities.AggregationMethod.Mean; break;
                case 1: agg = Classes.Utilities.AggregationMethod.StandardDeviation; break;
                case 2: agg = Classes.Utilities.AggregationMethod.Skewness; break;
                case 3: agg = Classes.Utilities.AggregationMethod.Count; break;
                default: agg = Classes.Utilities.AggregationMethod.StandardDeviation; break;
            }

            var outTree = new GH_Structure<VoxelGoo>();

            foreach (var key in _voxelStructure.Keys)
            {
                outTree.Insert(new VoxelGoo(_voxelStructure[key], agg, aggregationRadius), key, 0);
            }

            DA.SetDataTree(0, outTree);
        }

        /// <summary>
        /// Check equality between two data trees.
        /// 
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        private bool CheckTreeEquality(GH_Structure<GH_String> tree1, GH_Structure<GH_String> tree2)
        {
            if (tree1.Branches.Count != tree2.Branches.Count) return false;
            if (tree1.DataCount != tree2.DataCount) return false;

            var paths1 = tree1.Paths.ToList();
            var paths2 = tree2.Paths.ToList();

            if (paths1.Count != paths2.Count) return false;

            for (int i = 0; i < paths1.Count; i++)
            {
                if (!paths1[i].Indices.SequenceEqual(paths2[i].Indices))
                    return false;
            }

            return true;
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
                return Resources.voxelise;
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


        private GH_Structure<GH_String> _files = new GH_Structure<GH_String>();
        private GH_Structure<GH_Number> _layerHeights = new GH_Structure<GH_Number>();
        private Dictionary<GH_Path, VoxelStructure> _voxelStructure;

        private const double _samplingRate = 100000;

        private double _voxelSize = 1;
        private int _pointReadInterval = 10;

        #endregion
    }
}