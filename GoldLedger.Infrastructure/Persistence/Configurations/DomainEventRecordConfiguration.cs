using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldLedger.Infrastructure.Persistence.Configurations;

public sealed class DomainEventRecordConfiguration : IEntityTypeConfiguration<DomainEventRecord>
{
    public void Configure(EntityTypeBuilder<DomainEventRecord> builder)
    {
        builder.ToTable("DomainEventRecords");
        builder.HasKey(record => record.Id);
        builder.HasIndex(record => record.OrderId);
        builder.Property(record => record.EventType).HasMaxLength(200).IsRequired();
        builder.Property(record => record.Payload).IsRequired();
    }
}
