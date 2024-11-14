using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using IPMSpatialAnalysis.Classes;
using IPMSpatialAnalysis.Goo;
using Rhino.Geometry;

namespace IPMSpatialAnalysis.NewFolder
{
    public class VoxelParam : GH_Param<VoxelGoo>, IGH_PreviewObject
    {
        public override Guid ComponentGuid => new Guid("5A106831-86DF-4D41-B38F-53D0D54FDEB3");

        public bool IsPreviewCapable => true;

        public BoundingBox ClippingBox
        {
            get
            {
                return Preview_ComputeClippingBox();
            }
        }

        bool _hidden;
        public bool Hidden 
        {
            get
            {
                return _hidden;
            }
            set
            {
                _hidden = value;
            }
        }

        public VoxelParam()
            : base(new GH_InstanceDescription("Voxel Structure", "Voxels", "A voxel structure", "IPMSpatialAnalysis", "Types"))
        { }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Preview_DrawMeshes(args);
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            Preview_DrawWires(args);
        }
    }
}
