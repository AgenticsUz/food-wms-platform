using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WMS.Infrastructure.Persistence.Naming;

/// <summary>
/// Postgres nomlash konvensiyasi: jadval/ustun/kalit/indeks nomlari snake_case (Wash/HRM naqshi).
/// </summary>
/// <remarks>
/// ⚠️ Model konfiguratsiyasidan KEYIN qo'llanadi. Shuning uchun <c>HasFilter</c> ichidagi
/// SQL YAKUNIY nomlarda yoziladi: <c>"is_deleted" = false</c>, <c>"IsDeleted" = 0</c> emas
/// (ikkinchisi SQLite davridan qolgan shakl — Postgres uni rad etadi).
/// </remarks>
public static class SnakeCaseNaming
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            string? tableName = entityType.GetTableName();
            if (tableName is not null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            foreach (IMutableProperty property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (IMutableKey key in entityType.GetKeys())
            {
                if (key.GetName() is { } name)
                {
                    key.SetName(ToSnakeCase(name));
                }
            }

            foreach (IMutableForeignKey foreignKey in entityType.GetForeignKeys())
            {
                if (foreignKey.GetConstraintName() is { } name)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(name));
                }
            }

            foreach (IMutableIndex index in entityType.GetIndexes())
            {
                if (index.GetDatabaseName() is { } name)
                {
                    index.SetDatabaseName(ToSnakeCase(name));
                }
            }
        }
    }

    /// <summary><c>TenantId</c> → <c>tenant_id</c>, <c>IX_A_B</c> → <c>ix_a_b</c>.</summary>
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        StringBuilder builder = new(name.Length + 8);

        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];

            if (current == '_')
            {
                AppendSeparator(builder);
                continue;
            }

            if (char.IsUpper(current) && builder.Length > 0 && NeedsSeparator(name, i))
            {
                AppendSeparator(builder);
            }

            builder.Append(char.ToLower(current, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static void AppendSeparator(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != '_')
        {
            builder.Append('_');
        }
    }

    private static bool NeedsSeparator(string name, int index)
    {
        char previous = name[index - 1];
        return !char.IsUpper(previous) || (index + 1 < name.Length && char.IsLower(name[index + 1]));
    }
}
