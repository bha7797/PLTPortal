using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using static System.Windows.Forms.AxHost;

namespace UserOwnsData.Models
{
    public partial class LinksInfo
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string ReportId { get; set; }

        [Required]
        public string WorkspaceId { get; set; }

        [Required]
        public string PageId { get; set; }
        public string UserId { get; set; }

    }
}
