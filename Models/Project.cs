using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Duan_CNPM.Models {
    public class Project {
        [Key]
        public int ProjectID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [Column(TypeName = "ntext")]
        public string? Description { get; set; }

        [Required]
        public int StudentID { get; set; }

        public int? ProfessorID { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // Pending, Approved, InProgress, Completed, Rejected

        [Required]
        public int Year { get; set; }

        [Required]
        public int Semester { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime? DefenseDate { get; set; }

        public int? CouncilID { get; set; }

        public decimal? FinalScore { get; set; }

        [Column(TypeName = "ntext")]
        public string? ProfessorComment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; }

        public virtual ICollection<ProfessorComment> ProfessorComments { get; set; }

        // Navigation properties
        [ForeignKey("StudentID")]
        public virtual User Student { get; set; }

        [ForeignKey("ProfessorID")]
        public virtual User Professor { get; set; }

        [ForeignKey("CouncilID")]
        public virtual Council Council { get; set; }

        public virtual ICollection<ProjectFile> ProjectFiles { get; set; }
        public virtual ICollection<ProjectProgress> ProjectProgresses { get; set; }
        public virtual ICollection<Score> Scores { get; set; }
    }
}