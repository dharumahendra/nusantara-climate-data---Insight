using Microsoft.EntityFrameworkCore;
using NADI.Core.Models;

namespace NADI.Infrastructure.Database;

public class NadiDbContext : DbContext
{
    public NadiDbContext(DbContextOptions<NadiDbContext> options) : base(options)
    {
    }

    public DbSet<Region> Regions { get; set; } = null!;
    public DbSet<ClimateData> ClimateDataRecords { get; set; } = null!;
    public DbSet<ClimateTrend> ClimateTrends { get; set; } = null!;
    public DbSet<RegionComparison> RegionComparisons { get; set; } = null!;
    public DbSet<ClimateActionRecommendation> ClimateActionRecommendations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ClimateChat> ClimateChats { get; set; } = null!;
    public DbSet<ClimateDataSource> ClimateDataSources { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.RegionId);
            entity.HasMany(e => e.ClimateDataRecords)
                  .WithOne(d => d.Region)
                  .HasForeignKey(d => d.RegionId);
            entity.HasMany(e => e.ClimateTrends)
                  .WithOne(t => t.Region)
                  .HasForeignKey(t => t.RegionId);
        });

        modelBuilder.Entity<ClimateData>(entity =>
        {
            entity.HasKey(e => e.DataId);
            entity.HasOne(e => e.Source)
                  .WithMany(s => s.ClimateDataRecords)
                  .HasForeignKey(e => e.SourceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClimateDataSource>(entity =>
        {
            entity.HasKey(e => e.SourceId);
        });

        modelBuilder.Entity<ClimateTrend>(entity =>
        {
            entity.HasKey(e => e.TrendId);
        });

        modelBuilder.Entity<RegionComparison>(entity =>
        {
            entity.HasKey(e => e.ComparisonId);
            entity.HasOne(e => e.RegionA)
                  .WithMany()
                  .HasForeignKey(e => e.RegionAId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.RegionB)
                  .WithMany()
                  .HasForeignKey(e => e.RegionBId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClimateActionRecommendation>(entity =>
        {
            entity.HasKey(e => e.RecommendationId);
            entity.HasOne(e => e.Region)
                  .WithMany()
                  .HasForeignKey(e => e.RegionId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasMany(e => e.ClimateChats)
                  .WithOne(c => c.User)
                  .HasForeignKey(c => c.UserId);
        });

        modelBuilder.Entity<ClimateChat>(entity =>
        {
            entity.HasKey(e => e.ChatId);
        });
    }
}
