using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence.Rls;

namespace WMS.Infrastructure.Persistence.Configurations;

// Telegram: ulanish (tenant, RLS), deep-link tokeni va navbat (platforma, RLS YO'Q).
// ⚠️ HasFilter ichidagi SQL YAKUNIY (snake_case) nomlarda — sababi SnakeCaseNaming izohida.

internal sealed class TelegramLinkConfiguration : IEntityTypeConfiguration<TelegramLink>
{
    public void Configure(EntityTypeBuilder<TelegramLink> builder)
    {
        builder.Property(l => l.Username).HasMaxLength(64);
        builder.Property(l => l.FirstName).HasMaxLength(128);
        builder.Property(l => l.Lang).HasMaxLength(8).IsRequired();
        builder.Property(l => l.MutedTypes).HasMaxLength(512);

        builder.HasOne(l => l.UserProfile).WithMany().HasForeignKey(l => l.UserProfileId).OnDelete(DeleteBehavior.Cascade);

        // Bir profil — bitta yozuv (qayta ulanishda chat almashadi, qator ko'paymaydi).
        builder.HasIndex(l => new { l.TenantId, l.UserProfileId }).IsUnique()
            .HasFilter("\"user_profile_id\" IS NOT NULL AND \"is_deleted\" = false");

        // Bloklash va /status — chat bo'yicha (tenant scope'i ichida).
        builder.HasIndex(l => l.ChatId);
    }
}

internal sealed class TelegramLinkTokenConfiguration : IEntityTypeConfiguration<TelegramLinkToken>
{
    public void Configure(EntityTypeBuilder<TelegramLinkToken> builder)
    {
        // ⚠️ `tenant_id` ustuni bor, lekin RLS YO'Q: `/start <token>` tenant kontekstisiz keladi va
        // token RLS ostida bo'lsa topilmasdi. Generator ustunga qarab RLS qo'yardi — oshkora o'chiriladi.
        builder.HasAnnotation(RlsAnnotations.Enabled, false);

        builder.Property(t => t.Token).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => new { t.SubjectType, t.SubjectId });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TelegramGroupConfiguration : IEntityTypeConfiguration<TelegramGroup>
{
    public void Configure(EntityTypeBuilder<TelegramGroup> builder)
    {
        builder.HasAnnotation(RlsAnnotations.Enabled, false);
        builder.Property(g => g.Title).HasMaxLength(256);
        builder.Property(g => g.MutedTypes).HasMaxLength(512);
        builder.HasIndex(g => g.ChatId).IsUnique();
        builder.HasIndex(g => g.TenantId);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TelegramChatStateConfiguration : IEntityTypeConfiguration<TelegramChatState>
{
    public void Configure(EntityTypeBuilder<TelegramChatState> builder)
    {
        builder.HasAnnotation(RlsAnnotations.Enabled, false);
        builder.HasIndex(s => s.ChatId).IsUnique();
    }
}

internal sealed class TelegramOutboxConfiguration : IEntityTypeConfiguration<TelegramOutbox>
{
    public void Configure(EntityTypeBuilder<TelegramOutbox> builder)
    {
        // Yuboruvchi fon xizmati bitta so'rovda hamma tenantning navbatini o'qiydi — RLS yo'q (yuqoridagi izoh).
        builder.HasAnnotation(RlsAnnotations.Enabled, false);

        builder.Property(o => o.Text).HasMaxLength(TelegramOutbox.MaxTextLength).IsRequired();
        builder.Property(o => o.LastError).HasMaxLength(500);
        builder.Property(o => o.DedupKey).HasMaxLength(200);
        builder.Property(o => o.ReplyMarkup).HasMaxLength(2000);

        builder.HasIndex(o => new { o.Status, o.NextAttemptAt });

        // Tugma bosilganda (chat_id, message_id) bo'yicha topiladi (TG9).
        builder.HasIndex(o => new { o.ChatId, o.MessageId });

        // Ayni hodisa ikki marta ishlansa ikkinchi qator yozilmaydi (Postgres'da NULL'lar noyoblikka kirmaydi).
        builder.HasIndex(o => o.DedupKey).IsUnique();
    }
}
