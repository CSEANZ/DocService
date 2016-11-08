using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocService.Models
{
    public class Para
    {
        public Guid Id { get; set; }
        public Guid? DocId { get; set; }
        public string Text { get; set; }
        public string Style { get; set; }
        public int Order { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
    }
}