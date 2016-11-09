using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocServiceCore.Models
{
    public class FullDoc
    {
        public Doc Header { get; set; }
        public List<Para> Paragraphs { get; set; }
    }
}