using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocService.Models
{
    public class Doc
    {
        public string Title { get; set; }
        public string FileName { get; set; }
        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
    }
}