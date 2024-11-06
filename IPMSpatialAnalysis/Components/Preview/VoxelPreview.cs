using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using IPMSpatialAnalysis.Goo;
using IPMSpatialAnalysis.Properties;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace IPMSpatialAnalysis.Components.Preview
{
    public class VoxelPreview : GH_Component, IGH_PreviewObject
    {
        /// <summary>
        /// Initializes a new instance of the VoxelPreview class.
        /// </summary>
        public VoxelPreview()
          : base("VoxelPreview", "VoxelPreview",
              "Provides a colourful preview of the voxel data, with a controllable point size.",
              "IPMSpatialAnalysis", "Preview")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Voxel Data", "V", "Input voxel data to filter.", GH_ParamAccess.tree);
            pManager.AddIntervalParameter("Scalar Range", "R", "Sets a custom scalar range for colouring.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Point Weight", "W", "The weight of the display points.", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> voxelGoos = new GH_Structure<IGH_Goo>();
            Interval displayRange = new Interval();
            int weight = 2;

            if (!DA.GetDataTree("Voxel Data", out voxelGoos)) return;
            if (!DA.GetData("Scalar Range", ref displayRange)) return;
            if (!DA.GetData("Point Weight", ref weight)) return;

            _weight = weight < 1 ? 1 : weight;

            //if (voxelGoos == _voxelGoos && displayRange == _interval) return;

            _displayGoos = new List<VoxelGoo>();

            foreach (var branch in voxelGoos.Branches)
            {
                if (branch != null)
                {
                    foreach (var voxelGoo in branch)
                    {
                        if (voxelGoo == null) continue;
                        var display = new VoxelGoo((VoxelGoo)voxelGoo);

                        // This needs to change so that the point cloud stores the values,
                        // and then the display function takes care of the colour.
                        display.UpdatePointCloud();
                        display.UpdatePointCloudColours(displayRange.T0, displayRange.T1);

                        _displayGoos.Add(display);
                    }
                }
            }

            _interval = displayRange;
            _voxelGoos = voxelGoos;
        }

        /// <summary>
        /// We need this to be able to draw the preview.
        /// </summary>
        /// <param name="args"></param>
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (!Locked)
            {
                if (_displayGoos.Count > 0)
                {
                    foreach (var goo in _displayGoos)
                    {
                        args.Display.DrawPointCloud(goo.PreviewCloud, _weight);
                    }
                }
            }

            int lineWeight = 2;

            int colourDivisions = 10;

            int recHeight = 200;
            int recWidth = 100;

            var interval = recHeight / colourDivisions;

            var rec = new Rectangle(10, 30, recWidth, recHeight);
            args.Display.Draw2dRectangle(rec, Color.Black, lineWeight, Color.White);

            for (int i = 0; i < colourDivisions; i++)
            {
                Rectangle section = new Rectangle(rec.X, rec.Y + i * interval, recWidth / 3, interval);

                double factor = (double)i / (double)(colourDivisions - 1);

                Color sectionColour = Classes.Utilities.Lerp3(Color.Green, Color.Yellow, Color.Red, factor);

                args.Display.Draw2dRectangle(section, Color.Black, lineWeight, sectionColour);

                PointF start = new PointF(rec.X, rec.Y + (i + 1) * interval);
                PointF end = new PointF(rec.X + recWidth, rec.Y + (i + 1) * interval);

                args.Display.Draw2dLine(start, end, Color.Black, 1);
                args.Display.Draw2dText(_interval.ParameterAt(factor).ToString("F4"),
                    Color.Black,
                    new Point2d(start.X + recWidth / 3 + 3, rec.Y + i * interval + 3),
                    false, interval - 6);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.preview;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("992B5E13-D2B6-4471-8CEB-700CDFFA946B"); }
        }

        #region Fields

        private List<VoxelGoo> _displayGoos = new List<VoxelGoo>();
        private int _weight = 5;

        private GH_Structure<IGH_Goo> _voxelGoos = new GH_Structure<IGH_Goo>();
        private Interval _interval = new Interval();

        #endregion
    }
}