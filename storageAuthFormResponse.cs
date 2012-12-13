using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Umea.se.MiljoHalsoKontroll.PresentationLayer
{
    public class storageAuthFormResponseField
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    public class storageAuthFormResponse
    {
        public string action { get; set; }
        public List<storageAuthFormResponseField> fields { get; set; }
    }
}
