using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WdAlarm.Core.Entities;

namespace WdAlarm.Infrastructure.Data.Configurations;

public class AlarmDelayActionConfiguration : IEntityTypeConfiguration<AlarmDelayAction>
{
    public void Configure(EntityTypeBuilder<AlarmDelayAction> builder)
    {
        builder.ToTable("AlarmDelayActions");
        
        builder.HasKey(ada => ada.Id);
        
        builder.Property(ada => ada.AlarmId)
            .IsRequired();
        
        builder.Property(ada => ada.OffsetMilliseconds)
            .IsRequired();
        
        builder.Property(ada => ada.ExecutionOrder)
            .IsRequired();
        
        builder.Property(ada => ada.ActionType)
            .IsRequired()
            .HasConversion<string>();
        
        builder.Property(ada => ada.ActionConfig)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(ada => ada.CreatedAt)
            .IsRequired();
        
        builder.HasOne(ada => ada.Alarm)
            .WithMany(a => a.DelayActions)
            .HasForeignKey(ada => ada.AlarmId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(ada => ada.AlarmId);
    }
}
