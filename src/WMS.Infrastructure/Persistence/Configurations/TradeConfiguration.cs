using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Application.Common;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Persistence.Configurations;

// W1·2 moduli (transfer + kontragent/agent + moliya) — egasi shu modul agenti.

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.Property(t => t.Note).HasMaxLength(1000);
        builder.HasOne(t => t.FromWarehouse).WithMany().HasForeignKey(t => t.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.ToWarehouse).WithMany().HasForeignKey(t => t.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Counterparty).WithMany().HasForeignKey(t => t.CounterpartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Agent).WithMany().HasForeignKey(t => t.AgentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.CreatedByUser).WithMany().HasForeignKey(t => t.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.TenantId, t.Status });
        builder.HasIndex(t => new { t.TenantId, t.ConfirmedAt });
        builder.HasIndex(t => new { t.TenantId, t.OriginalTransferId });
    }
}

internal sealed class TransferItemConfiguration : IEntityTypeConfiguration<TransferItem>
{
    public void Configure(EntityTypeBuilder<TransferItem> builder)
    {
        builder.HasOne(i => i.Transfer).WithMany(t => t.Items).HasForeignKey(i => i.TransferId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class CounterpartyConfiguration : IEntityTypeConfiguration<Counterparty>
{
    public void Configure(EntityTypeBuilder<Counterparty> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.NameSearch).HasMaxLength(SearchNormalizer.MaxLength).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Note).HasMaxLength(1000);
        builder.Property(c => c.Inn).HasMaxLength(20);
        builder.HasOne(c => c.Agent).WithMany().HasForeignKey(c => c.AgentId).OnDelete(DeleteBehavior.Restrict);

        // ⚠️ `tenant_id` indeksi OSHKORA: EF uni tashqi kalit uchun o'zi yaratardi, lekin
        // quyidagi (tenant_id, identity_sub) indeksi shu ustundan boshlangani uchun uni
        // ORTIQCHA deb tashlab yuboradi. U esa QISMAN (filtrli) — RLS ning oddiy
        // `tenant_id = ?` so'rovlariga yaramaydi.
        builder.HasIndex(c => c.TenantId);

        // Bitta kabinet hisobi bitta kontragentga: aks holda odam ikki kontragentning
        // oldi-berdisini ko'rar, kabinet so'rovi esa «qaysi biri» degan savolga
        // javobsiz qolardi (fail-closed o'rniga tasodifiy tanlov).
        builder.HasIndex(c => new { c.TenantId, c.IdentitySub }).IsUnique()
            .HasFilter("\"identity_sub\" IS NOT NULL AND \"is_deleted\" = false");

        // Nom bo'yicha taxminiy qidiruv (P2.1) — sabab va cheklovlar `ProductConfiguration` da.
        builder.HasIndex(c => c.NameSearch).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}

internal sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Phone).HasMaxLength(20);
        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);

        // Kontragentdagi bilan bir xil sabab (EF FK indeksini tashlab yubormasin).
        builder.HasIndex(a => a.TenantId);

        // Kontragentdagi bilan bir xil sabab.
        builder.HasIndex(a => new { a.TenantId, a.IdentitySub }).IsUnique()
            .HasFilter("\"identity_sub\" IS NOT NULL AND \"is_deleted\" = false");
    }
}

internal sealed class CommissionRecordConfiguration : IEntityTypeConfiguration<CommissionRecord>
{
    public void Configure(EntityTypeBuilder<CommissionRecord> builder)
    {
        builder.HasOne(c => c.Agent).WithMany().HasForeignKey(c => c.AgentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Transfer).WithMany().HasForeignKey(c => c.TransferId).OnDelete(DeleteBehavior.Restrict);

        // D13: bitta sotuv bitta agentga bitta komissiya. SQLite davrida dublikatni faqat
        // servisdagi AnyAsync to'sardi — parallel tasdiqda ikkalasi ham o'tardi.
        builder.HasIndex(c => new { c.TenantId, c.TransferId, c.AgentId }).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(c => new { c.TenantId, c.AgentId });

        // D13 kengaytmasi (W1·2): komissiya yozuvi ham JOYIDA o'zgaradigan pul holati — to'lov uni
        // «to'landi» qiladi va xarajat yozadi, qaytarish summasini kamaytiradi. Tokensiz parallel ikki
        // to'lov xarajatni ikki marta yozardi. `xmin` tizim ustuni: jadvalga ustun QO'SHILMAYDI
        // (WmsDbContext.ApplyConcurrencyTokens bilan bir xil shakl).
        builder.Property<uint>("Version").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
    }
}

internal sealed class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.HasOne(d => d.Counterparty).WithMany().HasForeignKey(d => d.CounterpartyId).OnDelete(DeleteBehavior.Restrict);

        // D13: kontragentga bitta yuruvchi balans («topilmasa yarat» naqshi parallelda ikki qator berardi).
        builder.HasIndex(d => new { d.TenantId, d.CounterpartyId }).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class PaymentHistoryConfiguration : IEntityTypeConfiguration<PaymentHistory>
{
    public void Configure(EntityTypeBuilder<PaymentHistory> builder)
    {
        builder.Property(p => p.Note).HasMaxLength(1000);
        builder.HasOne(p => p.Counterparty).WithMany().HasForeignKey(p => p.CounterpartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Transfer).WithMany().HasForeignKey(p => p.TransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.RecordedByUser).WithMany().HasForeignKey(p => p.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.TenantId, p.PaidAt });
    }
}

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.HasOne(t => t.Counterparty).WithMany().HasForeignKey(t => t.CounterpartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Transfer).WithMany().HasForeignKey(t => t.TransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.RecordedByUser).WithMany().HasForeignKey(t => t.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.TenantId, t.Date });
    }
}
