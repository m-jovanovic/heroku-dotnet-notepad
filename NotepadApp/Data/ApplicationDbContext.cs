using Microsoft.EntityFrameworkCore;
using NotepadApp.Models;

namespace NotepadApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Note>()
                .Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<Note>()
                .Property(n => n.Content)
                .IsRequired();

            modelBuilder.Entity<Note>()
                .Property(n => n.Color)
                .IsRequired()
                .HasMaxLength(7); // #RRGGBB format

            modelBuilder.Entity<Note>()
                .Property(n => n.PositionX)
                .IsRequired();

            modelBuilder.Entity<Note>()
                .Property(n => n.PositionY)
                .IsRequired();
        }
    }
} 