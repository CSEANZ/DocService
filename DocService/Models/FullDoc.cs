using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocService.Models
{
    public class FullDoc
    {
        public Doc Header { get; set; }
        public List<Para> Paragraphs { get; set; }
    }
}