using Microsoft.EntityFrameworkCore;
using MontenegroFiscalization.Core.Models;

namespace MontenegroFiscalization.Infrastructure.Data;

public class FiscalizationDbContext : DbContext
{
    public FiscalizationDbContext(DbContextOptions<FiscalizationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<FiscalizationAudit> FiscalizationAudits { get; set; }
    public DbSet<TenantConfiguration> TenantConfigurations { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // FiscalizationAudit
        modelBuilder.Entity<FiscalizationAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.InvoiceNumber);
            entity.HasIndex(e => e.IIC);
            entity.HasIndex(e => e.FIC);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.RequestXml).HasColumnType("TEXT");
            entity.Property(e => e.ResponseXml).HasColumnType("TEXT");
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        });
        
        // TenantConfiguration
        modelBuilder.Entity<TenantConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.Property(e => e.TIN).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BusinessUnitCode).IsRequired().HasMaxLength(50);
        });
        
        // ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
        });
    }
}