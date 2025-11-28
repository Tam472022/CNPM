using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Duan_CNPM.Models
{
    public class ProfessorComment
    {
        [Key]
        public int CommentID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [Required]
        public int ProfessorID { get; set; }

        [Required]
        [Column(TypeName = "ntext")]
        public string CommentText { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }

        [ForeignKey("ProfessorID")]
        public virtual User Professor { get; set; }
    }
}