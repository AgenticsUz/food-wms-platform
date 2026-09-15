using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence.Rls;

namespace WMS.Infrastructure.Persistence.Configurations;

// AI (F10·A0): suhbat, xabar va sarf hisobi.
// ⚠️ HasFilter ichidagi SQL YAKUNIY (snake_case) nomlarda — sababi SnakeCaseNaming izohida.

internal sealed class AiConversationConfiguration : IEntityTypeConfiguration<AiConversation>
{
    public void Configure(EntityTypeBuilder<AiConversation> builder)
    {
        builder.Property(c => c.Title).HasMaxLength(200);
        builder.Property(c => c.Language).HasMaxLength(8).IsRequired();

        // Profil o'chsa suhbat QOLADI: u hujjatning audit izi va uni odam bilan birga
        // olib ketish «bu hujjat qayerdan chiqdi?» savolini javobsiz qoldirardi.
        builder.HasOne(c => c.UserProfile).WithMany()
            .HasForeignKey(c => c.UserProfileId).OnDelete(DeleteBehavior.SetNull);

        // Ro'yxat: «oxirgi faollik» bo'yicha teskari tartibda.
        builder.HasIndex(c => new { c.TenantId, c.LastActivityAt });

        // Telegram'da davom etayotgan suhbatni topish (chat + oyna).
        builder.HasIndex(c => new { c.TelegramChatId, c.LastActivityAt })
            .HasFilter("\"telegram_chat_id\" IS NOT NULL");

        // ⚠️ AI tayyorlagan hujjat/to'lov shu suhbatga ISHORA qiladi va havola qilingan
        // suhbat O'CHIRILMAYDI (RESTRICT): tarix tozalash audit izini uzib ketmasin.
        builder.HasMany<Transfer>().WithOne()
            .HasForeignKey(t => t.AiConversationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany<PaymentHistory>().WithOne()
            .HasForeignKey(p => p.AiConversationId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AiMessageConfiguration : IEntityTypeConfiguration<AiMessage>
{
    public void Configure(EntityTypeBuilder<AiMessage> builder)
    {
        // Matn uzunligi CHEKLANMAYDI: tool natijasi ham, model javobi ham uzun bo'lishi
        // mumkin va kesish tarixni jimgina yolg'on qilardi.
        builder.Property(m => m.Text).HasColumnType("text");
        builder.Property(m => m.Model).HasMaxLength(100);

        // jsonb — keyinchalik «qaysi tool necha marta chaqirilgan» kabi savollar so'rov
        // bilan javob olsin (matn ustunida buning uchun har safar parse kerak bo'lardi).
        builder.Property(m => m.ToolCallsJson).HasColumnType("jsonb");
        builder.Property(m => m.ToolResultsJson).HasColumnType("jsonb");

        builder.HasOne(m => m.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);

        // Tartib suhbat ichida NOYOB: bitta aylanishda yozilgan ikki xabar bir xil raqam
        // olsa tarix qayta tiklanganda o'rin almashardi.
        builder.HasIndex(m => new { m.ConversationId, m.Sequence }).IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class AiUsageConfiguration : IEntityTypeConfiguration<AiUsage>
{
    public void Configure(EntityTypeBuilder<AiUsage> builder)
    {
        builder.Property(u => u.Model).HasMaxLength(100).IsRequired();

        // Dollar — to'rt xonali: bitta so'rov tiyindan ham arzon bo'lishi mumkin va
        // ikki xonada kunlik yig'indi nolga yaxlitlanib ketardi.
        builder.Property(u => u.UsdCost).HasPrecision(18, 6);

        // ⚠️ Qisman noyob indeks — `AiUsageMeter` dagi `ON CONFLICT` ning ARBITRI
        // (`TenantCounter` naqshi): predikat o'zgarsa SQL ham o'zgarishi shart.
        builder.HasIndex(u => new { u.TenantId, u.Day, u.Model }).IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}

internal sealed class AiDailyCostConfiguration : IEntityTypeConfiguration<AiDailyCost>
{
    public void Configure(EntityTypeBuilder<AiDailyCost> builder)
    {
        // ⚠️ Tenant ustuni YO'Q va RLS ham yo'q (sababi entity izohida): kunlik shift
        // HAMMA tenant bo'yicha yig'indiga qaraydi, RLS esa uni o'z tenantiga qisardi.
        builder.HasAnnotation(RlsAnnotations.Enabled, false);

        builder.Property(c => c.UsdCost).HasPrecision(18, 6);
        builder.HasIndex(c => c.Day).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}
