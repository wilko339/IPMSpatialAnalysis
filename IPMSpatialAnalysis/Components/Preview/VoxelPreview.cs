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
                int lineWeight = 2;
                int colourDivisions = 10;
                int recHeight = 200;
                int colourBlockWidth = 33;
                int approxCharacterWidth = 7;
                int textPadding = 3;

                var interval = recHeight / colourDivisions;

                System.Drawing.Point startPoint = new System.Drawing.Point(10, 30);

                List<string> displayValues = new List<string>();
                int longestString = 0;

                // Get all numerical values to display and work out the longest.
                for (int i = 0; i < colourDivisions; i++)
                {
                    double factor = (double)i / (double)(colourDivisions - 1);
                    string displayValue = _interval.ParameterAt(factor).ToString("F4");

                    if (displayValue.Length > longestString) longestString = displayValue.Length;
                    displayValues.Add(displayValue);
                }

                // Calculate textbox width based on string length
                int textBoxWidth = approxCharacterWidth * longestString + 2 * textPadding;
                int extremeRightCoord = startPoint.X + textBoxWidth + colourBlockWidth + 1;

                // If a title is provided, add a box to the top
                bool title = _colourBarTitle.Length > 0;
                System.Drawing.Point colourBarStartPoint = startPoint;

                if (title)
                {
                    colourBarStartPoint.Y += interval;
                    recHeight += interval;
                }

                // Draw background
                var rec = new Rectangle(10, 30, extremeRightCoord - startPoint.X, recHeight);
                args.Display.Draw2dRectangle(rec, Color.Black, lineWeight, Color.White);

                if (title)
                {
                    args.Display.Draw2dText(_colourBarTitle, Color.Black,
                        new Point2d(startPoint.X + (extremeRightCoord - startPoint.X) / 2, startPoint.Y + interval / 2), true, interval - textPadding * 2);
                }

                // Draw the rest
                for (int i = 0; i < colourDivisions; i++)
                {
                    double factor = (double)i / (double)(colourDivisions - 1);

                    // Colour block
                    Rectangle colourBlock = new Rectangle(colourBarStartPoint.X, colourBarStartPoint.Y + i * interval, colourBlockWidth, interval);
                    Color colourBlockColour = Classes.Utilities.Lerp3(Color.Green, Color.Yellow, Color.Red, factor);
                    args.Display.Draw2dRectangle(colourBlock, Color.Black, lineWeight, colourBlockColour);

                    // Separating lines
                    if (i < colourDivisions - 1)
                    {
                        PointF start = new PointF(colourBarStartPoint.X, colourBarStartPoint.Y + (i + 1) * interval);
                        PointF end = new PointF(extremeRightCoord, start.Y);
                        args.Display.Draw2dLine(start, end, Color.Black, 1);
                    }

                    args.Display.Draw2dText(displayValues[i],
                        Color.Black,
                        new Point2d(
                            colourBlockWidth + textPadding + colourBarStartPoint.X,
                            colourBarStartPoint.Y + i * interval + textPadding + 1),
                        false, interval - textPadding * 2);
                }

                if (title)
                {
                    PointF start = new PointF(colourBarStartPoint.X, colourBarStartPoint.Y);
                    PointF end = new PointF(extremeRightCoord, start.Y);
                    args.Display.Draw2dLine(start, end, Color.Black, 1);
                }
                if (_displayGoos.Count > 0)
                {
                    foreach (var goo in _displayGoos)
                    {
                        args.Display.DrawPointCloud(goo.PreviewCloud, _weight);
                    }
                }
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

        private string _colourBarTitle = "";

        #endregion
    }
}