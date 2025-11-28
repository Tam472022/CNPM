using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Duan_CNPM.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // Student, Professor, Admin

        [StringLength(100)]
        public string? Email { get; set; } // ✅ Nullable

        [StringLength(20)]
        public string? Phone { get; set; } // ✅ Nullable

        [StringLength(255)]
        public string? Avatar { get; set; } // ✅ Đã nullable

        [StringLength(100)]
        public string? Major { get; set; } // ✅ Nullable - Chuyên ngành

        [StringLength(50)]
        public string? StudentCode { get; set; } // ✅ Nullable - Mã sinh viên

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLogin { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        // Navigation properties
        public virtual ICollection<Project> ProjectsAsStudent { get; set; }
        public virtual ICollection<Project> ProjectsAsProfessor { get; set; }
        public virtual ICollection<CouncilMember> CouncilMemberships { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<Message> ReceivedMessages { get; set; }
    }
}