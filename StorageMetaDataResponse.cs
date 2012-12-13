using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Umea.se.MiljoHalsoKontroll.PresentationLayer
{
    public class StorageMetaDataResponse
    {
        public string _bucket { get; set; }
        public string _checksum { get; set; }
        public int _content_length { get; set; }
        public string _format { get; set; }
        public string _label { get; set; }
        public string _last_modified { get; set; }
        public string _location { get; set; }
        public object _owner { get; set; }

    }
}
