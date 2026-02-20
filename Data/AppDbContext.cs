using Microsoft.EntityFrameworkCore;
using WordLearnerKids.Models;

namespace WordLearnerKids.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<StoredFile> Files => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(user => user.Email).HasMaxLength(256);
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.Property(note => note.Title).HasMaxLength(200);
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(note => note.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.Property(file => file.OriginalName).HasMaxLength(255);
            entity.Property(file => file.StoredName).HasMaxLength(100);
            entity.Property(file => file.ContentType).HasMaxLength(255);
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(file => file.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
