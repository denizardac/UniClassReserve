using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UniClassReserve.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Reservation - User (Cascade Delete)
            builder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Feedback - User (Cascade Delete)
            builder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        // Ek kullanıcı özellikleri buraya eklenebilir
        public bool IsDeleted { get; set; } = false;
    }

    public class Classroom
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class Term
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class Reservation
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        public int ClassroomId { get; set; }
        public Classroom Classroom { get; set; } = null!;
        public int? TermId { get; set; }
        public Term? Term { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = ""; // Pending, Approved, Rejected
        public string? AdminNote { get; set; }
        public int DayOfWeek { get; set; } // 1=Monday, 7=Sunday
        public DateTime StartDate { get; set; } // Tekrarın başladığı tarih
        public DateTime EndDate { get; set; }   // Tekrarın bittiği tarih
        public bool IsHoliday { get; set; } = false;
        public string? ConflictReason { get; set; }
    }

    public class Feedback
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        public int ClassroomId { get; set; }
        public Classroom Classroom { get; set; } = null!;
        public int? TermId { get; set; }
        public Term? Term { get; set; }
        public int Rating { get; set; } // 1-5
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; } = false;
    }

    public class Log
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "";
        public string Operation { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsError { get; set; }
        public string? Details { get; set; }
        public string LogLevel { get; set; } = "Info"; // Info, Warning, Error
    }
} 