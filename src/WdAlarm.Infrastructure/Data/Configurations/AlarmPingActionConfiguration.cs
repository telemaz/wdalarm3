using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data.Configurations;

public class AlarmPingActionConfiguration : IEntityTypeConfiguration<AlarmPingAction>
{
    public void Configure(EntityTypeBuilder<AlarmPingAction> builder)
    {
        builder.ToTable("AlarmPingActions");
        
        builder.HasKey(apa => apa.Id);
        
        builder.Property(apa => apa.AlarmId)
            .IsRequired();
        
        builder.Property(apa => apa.ActionType)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(apa => apa.ActionConfig)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(apa => apa.ExecutionOrder)
            .IsRequired();
        
        builder.Property(apa => apa.CreatedAt)
            .IsRequired();
        
        builder.HasOne(apa => apa.Alarm)
            .WithMany(a => a.PingActions)
            .HasForeignKey(apa => apa.AlarmId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(apa => apa.AlarmId);
    }
}
