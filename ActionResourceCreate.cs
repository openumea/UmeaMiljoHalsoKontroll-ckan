using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Umea.se.MiljoHalsoKontroll.PresentationLayer
{
    public class ActionResourceCreate
    {
        public string mimetype { get; set; }
        public string hash { get; set; }
        public string name { get; set; }
        public string format { get; set; }
        public string url { get; set; }
        public string package_id { get; set; }
        public string resource_type { get; set; }
        public int size { get; set; }
    }
}
