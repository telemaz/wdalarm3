using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data.Configurations;

public class PingConfiguration : IEntityTypeConfiguration<Ping>
{
    public void Configure(EntityTypeBuilder<Ping> builder)
    {
        builder.ToTable("Pings");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.AlarmId)
            .IsRequired();
        
        builder.Property(p => p.AlarmCycleId);
        
        builder.Property(p => p.ReceivedAt)
            .IsRequired();
        
        builder.Property(p => p.Payload)
            .HasMaxLength(4000);
        
        builder.Property(p => p.VerificationProof)
            .HasMaxLength(2000);
        
        builder.Property(p => p.VerificationResult)
            .IsRequired();
        
        builder.Property(p => p.VerificationError)
            .HasMaxLength(500);
        
        builder.Property(p => p.ResetTimerRequested)
            .IsRequired();
        
        builder.Property(p => p.TimerWasReset)
            .IsRequired();
        
        builder.Property(p => p.IpAddress)
            .HasMaxLength(45);
        
        builder.Property(p => p.UserAgent)
            .HasMaxLength(500);
        
        builder.HasOne(p => p.Alarm)
            .WithMany(a => a.Pings)
            .HasForeignKey(p => p.AlarmId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(p => p.AlarmCycle)
            .WithMany()
            .HasForeignKey(p => p.AlarmCycleId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasIndex(p => p.AlarmId);
        builder.HasIndex(p => p.ReceivedAt);
    }
}
