using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data.Configurations;

public class AlarmCycleConfiguration : IEntityTypeConfiguration<AlarmCycle>
{
    public void Configure(EntityTypeBuilder<AlarmCycle> builder)
    {
        builder.ToTable("AlarmCycles");
        
        builder.HasKey(ac => ac.Id);
        
        builder.Property(ac => ac.AlarmId)
            .IsRequired();
        
        builder.Property(ac => ac.AlarmPointTime)
            .IsRequired();
        
        builder.Property(ac => ac.StartedAt)
            .IsRequired();
        
        builder.Property(ac => ac.CompletedAt);
        
        builder.Property(ac => ac.CompletedByPingId);
        
        builder.Property(ac => ac.Status)
            .IsRequired()
            .HasConversion<string>();
        
        builder.HasOne(ac => ac.Alarm)
            .WithMany(a => a.Cycles)
            .HasForeignKey(ac => ac.AlarmId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(ac => ac.CompletedByPing)
            .WithMany()
            .HasForeignKey(ac => ac.CompletedByPingId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(ac => ac.AlarmId);
        builder.HasIndex(ac => ac.Status);
        builder.HasIndex(ac => ac.AlarmPointTime);
    }
}
