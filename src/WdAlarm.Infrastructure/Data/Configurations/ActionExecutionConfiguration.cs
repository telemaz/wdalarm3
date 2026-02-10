using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data.Configurations;

public class ActionExecutionConfiguration : IEntityTypeConfiguration<ActionExecution>
{
    public void Configure(EntityTypeBuilder<ActionExecution> builder)
    {
        builder.ToTable("ActionExecutions");
        
        builder.HasKey(ae => ae.Id);
        
        builder.Property(ae => ae.AlarmDelayActionId);
        
        builder.Property(ae => ae.AlarmPingActionId);
        
        builder.Property(ae => ae.AlarmCycleId);
        
        builder.Property(ae => ae.PingId);
        
        builder.Property(ae => ae.ActionType)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(ae => ae.ScheduledTime)
            .IsRequired();
        
        builder.Property(ae => ae.ExecutedTime);
        
        builder.Property(ae => ae.Status)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(ae => ae.RetryCount)
            .IsRequired();
        
        builder.Property(ae => ae.ErrorMessage)
            .HasMaxLength(2000);
        
        builder.Property(ae => ae.CreatedAt)
            .IsRequired();
        
        builder.HasOne(ae => ae.AlarmDelayAction)
            .WithMany(ada => ada.Executions)
            .HasForeignKey(ae => ae.AlarmDelayActionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(ae => ae.AlarmPingAction)
            .WithMany(apa => apa.Executions)
            .HasForeignKey(ae => ae.AlarmPingActionId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(ae => ae.AlarmCycle)
            .WithMany(ac => ac.ActionExecutions)
            .HasForeignKey(ae => ae.AlarmCycleId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(ae => ae.Ping)
            .WithMany(p => p.ActionExecutions)
            .HasForeignKey(ae => ae.PingId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasIndex(ae => ae.Status);
        builder.HasIndex(ae => ae.ScheduledTime);
        builder.HasIndex(ae => new { ae.Status, ae.ScheduledTime });
    }
}
