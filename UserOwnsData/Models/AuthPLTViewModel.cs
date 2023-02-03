using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserOwnsData.Models
{
    public class AuthPLTViewModel
    {
        public AuthDetails AuthDetails { get; set; }
        public List<PLTModel> PLTModel { get; set; }
    }
}