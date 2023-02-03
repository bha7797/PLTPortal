using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserOwnsData.Models
{
    public class PLTModel
    {
        public int Id { get; set; }
        public string WorkspaceName { get; set; }
        public string ReportName { get; set; }
        public string PageName { get; set; }
        public string EndTime { get; set; }
        public decimal PLT { get; set; }
    }
}