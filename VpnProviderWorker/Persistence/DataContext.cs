using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VpnProviderWorker.Persistence.Inbox;
using VpnProviderWorker.Persistence.Outbox;

namespace VpnProviderWorker.Persistence;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<OutboxEvent> Outbox { get; set; }
    public DbSet<InboxEvent> Inbox { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InboxEventTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxEventTypeConfiguration());
    }

    internal class InboxEventTypeConfiguration : IEntityTypeConfiguration<InboxEvent>
    {
        public void Configure(EntityTypeBuilder<InboxEvent> builder)
        {
            builder.ToTable("inbox");
            
            builder.HasKey(x => x.EventId);
            
            builder.HasIndex(x => new { x.OccurredOnUtc, x.ProcessedOnUtc, x.EventId, x.Type}, "IX_inbox_messages_unprocessed")
                .HasFilter("processed_on_utc IS NULL");
            
            builder.Property(x => x.EventId).ValueGeneratedNever().HasColumnName("event_id").IsRequired();
            builder.Property(x => x.Type).HasColumnName("type").IsRequired();
            builder.Property(x => x.Content).HasColumnName("content").IsRequired();
            builder.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
            builder.Property(x => x.ProcessedOnUtc).HasColumnName("processed_on_utc").IsRequired(false);
        }
    }
    internal class OutboxEventTypeConfiguration : IEntityTypeConfiguration<OutboxEvent>
    {
        public void Configure(EntityTypeBuilder<OutboxEvent> builder)
        {
            builder.ToTable("outbox");

            builder.HasKey(x => x.EventId);

            builder.Property(x => x.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            builder.HasIndex(x => x.OccurredOnUtc, "IX_outbox_messages_unprocessed")
                .HasFilter("processed_on_utc IS NULL");

            builder.Property(x => x.OccurredOnUtc)
                .HasColumnName("occurred_on_utc")
                .IsRequired();

            builder.Property(x => x.ProcessedOnUtc)
                .HasColumnName("processed_on_utc")
                .IsRequired(false);

            builder.Property(x => x.Type)
                .HasColumnName("type")
                .IsRequired();

            builder.Property(x => x.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(x => x.Error)
                .HasColumnName("error")
                .IsRequired(false);
        }
    }
}