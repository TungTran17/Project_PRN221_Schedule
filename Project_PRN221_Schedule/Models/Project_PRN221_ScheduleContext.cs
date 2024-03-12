using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Project_PRN221_Schedule.Models
{
    public partial class Project_PRN221_ScheduleContext : DbContext
    {
        public Project_PRN221_ScheduleContext()
        {
        }

        public Project_PRN221_ScheduleContext(DbContextOptions<Project_PRN221_ScheduleContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Class> Classes { get; set; } = null!;
        public virtual DbSet<Course> Courses { get; set; } = null!;
        public virtual DbSet<Group> Groups { get; set; } = null!;
        public virtual DbSet<Room> Rooms { get; set; } = null!;
        public virtual DbSet<Schedule> Schedules { get; set; } = null!;
        public virtual DbSet<Slot> Slots { get; set; } = null!;
        public virtual DbSet<Teacher> Teachers { get; set; } = null!;
        public virtual DbSet<WeekSchedule> WeekSchedules { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var ConnectionString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Class>(entity =>
            {
                entity.ToTable("Class");

                entity.Property(e => e.ClassName)
                    .HasMaxLength(10)
                    .IsFixedLength();
            });

            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("Course");

                entity.HasIndex(e => e.CourseCode, "course_coursecode_unique")
                    .IsUnique();

                entity.Property(e => e.CourseCode).HasMaxLength(255);
            });

            modelBuilder.Entity<Group>(entity =>
            {
                entity.ToTable("Group");

                entity.HasOne(d => d.Class)
                    .WithMany(p => p.Groups)
                    .HasForeignKey(d => d.ClassId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("group_classid_foreign");

                entity.HasOne(d => d.Course)
                    .WithMany(p => p.Groups)
                    .HasForeignKey(d => d.CourseId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("group_courseid_foreign");

                entity.HasOne(d => d.Teacher)
                    .WithMany(p => p.Groups)
                    .HasForeignKey(d => d.TeacherId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("group_teacherid_foreign");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.ToTable("Room");

                entity.Property(e => e.RoomCode)
                    .HasMaxLength(10)
                    .IsFixedLength();
            });

            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.ToTable("Schedule");

                entity.Property(e => e.ImplementDate).HasColumnType("date");
            });

            modelBuilder.Entity<Slot>(entity =>
            {
                entity.ToTable("Slot");

                entity.Property(e => e.EndTime).HasMaxLength(50);

                entity.Property(e => e.StartTime).HasMaxLength(50);
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("Teacher");

                entity.Property(e => e.TeacherName)
                    .HasMaxLength(255)
                    .IsFixedLength();
            });

            modelBuilder.Entity<WeekSchedule>(entity =>
            {
                entity.ToTable("WeekSchedule");

                entity.HasOne(d => d.Group)
                    .WithMany(p => p.WeekSchedules)
                    .HasForeignKey(d => d.GroupId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WeekSchedule_Group");

                entity.HasOne(d => d.Room)
                    .WithMany(p => p.WeekSchedules)
                    .HasForeignKey(d => d.RoomId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WeekSchedule_Room");

                entity.HasOne(d => d.Schedule)
                    .WithMany(p => p.WeekSchedules)
                    .HasForeignKey(d => d.ScheduleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WeekSchedule_Schedule");

                entity.HasOne(d => d.Slot)
                    .WithMany(p => p.WeekSchedules)
                    .HasForeignKey(d => d.SlotId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_WeekSchedule_Slot");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
