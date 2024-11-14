using System;

using Grasshopper.Kernel;
using IPMSpatialAnalysis.Components.Types;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace IPMSpatialAnalysis.Components.Analysis
{
    public class CorrelationCoefficients : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CorrelationCoefficients class.
        /// </summary>
        public CorrelationCoefficients()
          : base("CorrelationCoefficients", "Nickname",
              "Calculates the Pearson and Spearman Rank correlation coefficients between two voxel grids. THe two grids must have the same keys.",
              "IPMSpatialAnalysis", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VoxelParam(), "Voxel Data 1", "VD1", "First voxel dataset.", GH_ParamAccess.item);
            pManager.AddParameter(new VoxelParam(), "Voxel Data 2", "VD2", "Second voxel dataset.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Pearson Coefficient", "P", "Calculated Pearson correlation coefficient.", GH_ParamAccess.item);
            pManager.AddNumberParameter("PearsonP", "PSig", "The p-value of the calculated Pearson coefficient.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Spearman Rank Coefficient", "S", "Calculated Spearman correlation coefficient.", GH_ParamAccess.item);
            pManager.AddNumberParameter("SpearmanP", "SSig", "The p-value of the calculated Spearman coefficient.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            VoxelGoo vg1 = new VoxelGoo();
            VoxelGoo vg2 = new VoxelGoo();

            if (!DA.GetData(0, ref vg1)) return;
            if (!DA.GetData(1, ref vg2)) return;

            VoxelGoo goo1 = new VoxelGoo(vg1);
            VoxelGoo goo2 = new VoxelGoo(vg2);

            if (goo1.Count != goo2.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The two grids must have the same voxel count.");
                return;
            }

            var goo1Scalars = goo1.VoxelScalars;
            var goo2Scalars = goo2.VoxelScalars;

            if (goo1Scalars.Count < 3 || goo2Scalars.Count < 3)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length of scalar lists must be greater than 2.");
                return;
            }

            int n = goo1Scalars.Count;

            double pearson = Correlation.Pearson(goo1Scalars, goo2Scalars);
            double tPearson = pearson * Math.Sqrt(n - 2) / Math.Sqrt(1 - pearson * pearson);
            double pPearson = 2 * (1 - StudentT.CDF(0, 1, n - 2, Math.Abs(tPearson)));

            double spearman = Correlation.Spearman(goo1Scalars, goo2Scalars);
            double tSpearman = spearman * Math.Sqrt(n - 2) / Math.Sqrt(1 - spearman * spearman);
            double pSpearman = 2 * (1 - StudentT.CDF(0, 1, n - 2, Math.Abs(tSpearman)));

            DA.SetData(0, pearson);
            DA.SetData(1, pPearson);
            DA.SetData(2, spearman);
            DA.SetData(3, pSpearman);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.correlation;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("37699227-04BE-464C-88D6-2FD396BFD38B"); }
        }
    }
}