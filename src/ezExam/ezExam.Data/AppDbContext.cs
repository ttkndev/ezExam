using ezExam.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ezExam.Data
{
    /// <summary>
    /// DbContext chính của ứng dụng - quản lý kết nối SQLite và ánh xạ các bảng
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<ExamSession> ExamSessions { get; set; }
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<ExamRoom> ExamRooms { get; set; }
        public DbSet<RoomAssignment> RoomAssignments { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // --- ExamSession ---
            mb.Entity<ExamSession>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.HasMany(x => x.Candidates)
                 .WithOne(c => c.ExamSession)
                 .HasForeignKey(c => c.ExamSessionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- Candidate ---
            mb.Entity<Candidate>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
                e.Property(x => x.SubjectNames).HasMaxLength(500);
                e.HasIndex(x => new { x.ExamSessionId, x.CandidateNumber });
            });

            // --- ExamRoom ---
            mb.Entity<ExamRoom>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.RoomName).IsRequired().HasMaxLength(50);
                e.HasMany(x => x.Assignments)
                 .WithOne(a => a.ExamRoom)
                 .HasForeignKey(a => a.ExamRoomId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // --- RoomAssignment ---
            mb.Entity<RoomAssignment>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Candidate)
                 .WithMany()
                 .HasForeignKey(x => x.CandidateId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
