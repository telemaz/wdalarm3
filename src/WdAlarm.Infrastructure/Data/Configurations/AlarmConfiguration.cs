using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data.Configurations;

public class AlarmConfiguration : IEntityTypeConfiguration<Alarm>
{
    public void Configure(EntityTypeBuilder<Alarm> builder)
    {
        builder.ToTable("Alarms");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.UserId)
            .IsRequired();
        
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(a => a.Description)
            .HasMaxLength(1000);
        
        builder.Property(a => a.DelayType)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(a => a.TimeoutDuration);
        
        builder.Property(a => a.CronExpression)
            .HasMaxLength(100);
        
        builder.Property(a => a.AllowPingNoTimerReset)
            .IsRequired();
        
        builder.Property(a => a.VerificationMethod)
            .HasConversion<string>();
        
        builder.Property(a => a.VerificationConfig)
            .HasColumnType("jsonb");
        
        builder.Property(a => a.IsActive)
            .IsRequired();
        
        builder.Property(a => a.CreatedAt)
            .IsRequired();
        
        builder.Property(a => a.UpdatedAt)
            .IsRequired();
        
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.IsActive);
    }
}
