using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Duan_CNPM.Models
{
    public class Council
    {
        [Key]
        public int CouncilID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        public DateTime DefenseDate { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [Column(TypeName = "ntext")]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<CouncilMember> CouncilMembers { get; set; }
        public virtual ICollection<Project> Projects { get; set; }
    }

    public class CouncilMember
    {
        [Key]
        public int CouncilMemberID { get; set; }

        [Required]
        public int CouncilID { get; set; }

        [Required]
        public int ProfessorID { get; set; }

        [Required]
        [StringLength(50)]
        public string Role { get; set; } // Chairman, Secretary, Member

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CouncilID")]
        public virtual Council Council { get; set; }

        [ForeignKey("ProfessorID")]
        public virtual User Professor { get; set; }

        public virtual ICollection<Score> Scores { get; set; }
    }

    public class Score
    {
        [Key]
        public int ScoreID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [Required]
        public int CouncilMemberID { get; set; }

        [Range(0, 10)]
        public decimal ScoreValue { get; set; }

        [Column(TypeName = "ntext")]
        public string Comment { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }

        [ForeignKey("CouncilMemberID")]
        public virtual CouncilMember CouncilMember { get; set; }
    }

    public class ProjectFile
    {
        [Key]
        public int FileID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } // Report, Code, Poster, Other

        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public int UploadedBy { get; set; }

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
    }

    public class ProjectProgress
    {
        [Key]
        public int ProgressID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Column(TypeName = "ntext")]
        public string Description { get; set; }

        [Range(0, 100)]
        public int Percentage { get; set; }

        public DateTime UpdateDate { get; set; } = DateTime.Now;

        public int UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
    }

    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [Column(TypeName = "ntext")]
        public string Message { get; set; }

        [StringLength(50)]
        public string Type { get; set; } // Info, Warning, Success, Error

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? Link { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }

    public class Message
    {
        [Key]
        public int MessageID { get; set; }

        [Required]
        public int SenderID { get; set; }

        [Required]
        public int ReceiverID { get; set; }

        [Required]
        [Column(TypeName = "ntext")]
        public string Content { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime SentDate { get; set; } = DateTime.Now;

        public DateTime? ReadDate { get; set; }

        // Navigation properties
        [ForeignKey("SenderID")]
        public virtual User Sender { get; set; }

        [ForeignKey("ReceiverID")]
        public virtual User Receiver { get; set; }
    }

    public class SystemConfig
    {
        [Key]
        public int ConfigID { get; set; }

        [Required]
        [StringLength(100)]
        public string ConfigKey { get; set; }

        [Required]
        [StringLength(500)]
        public string ConfigValue { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }

public class AuditLog
{
    [Key]
    public int LogID { get; set; }

    public int? UserID { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; }

    [StringLength(100)]
    public string? TableName { get; set; }

    public int? RecordID { get; set; }

    [Column(TypeName = "ntext")]
    public string? OldValue { get; set; }

    [Column(TypeName = "ntext")]
    public string? NewValue { get; set; }

    [StringLength(50)]
    public string? IPAddress { get; set; } // ✅ Bỏ [Required]

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}

}