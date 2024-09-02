using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace IPMSpatialAnalysis
{
    public class IPMSpatialAnalysisInfo : GH_AssemblyInfo
    {
        public override string Name => "IPMSpatialAnalysis";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("d73522d4-618a-4726-838c-8b96d18999c9");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}