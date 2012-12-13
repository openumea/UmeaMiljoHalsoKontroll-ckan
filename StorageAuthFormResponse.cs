using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Umea.se.MiljoHalsoKontroll.PresentationLayer
{
    public class StorageAuthFormResponseField
    {
        public string name { get; set; }
        public string value { get; set; }
    }
    public class StorageAuthFormResponse
    {
        public string action { get; set; }
        public List<StorageAuthFormResponseField> fields { get; set; }
    }
}
