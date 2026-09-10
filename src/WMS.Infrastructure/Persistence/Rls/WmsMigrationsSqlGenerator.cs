using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

// INpgsqlSingletonOptions — Npgsql "Internal" namespace'i, lekin bazaviy konstruktor aynan shuni talab qiladi.
#pragma warning disable EF1001

namespace WMS.Infrastructure.Persistence.Rls;

/// <summary>
/// Migratsiya SQL generatori: <c>CREATE TABLE</c> (va keyin <c>tenant_id</c> qo'shilganda)
/// jadvalga <c>ENABLE/FORCE ROW LEVEL SECURITY</c> + <c>tenant_isolation</c> siyosatini chiqaradi.
/// </summary>
/// <remarks>
/// Qo'lda SQL yozilmaydi (Wash naqshi): siyosat har tenant jadvali uchun modeldan
/// avtomatik keladi, ya'ni yangi jadval qo'shgan agent uni «unutib» qoldira olmaydi.
/// Qaror tartibi: model annotatsiyasi <c>wms:rls</c>; bo'lmasa — <c>tenant_id</c>
/// ustuni borligi (xavfsizlik to'ri).
/// </remarks>
public class WmsMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public WmsMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal.INpgsqlSingletonOptions npgsqlSingletonOptions)
        : base(dependencies, npgsqlSingletonOptions)
    {
    }

    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(builder);

        base.Generate(operation, model, builder, terminate);

        if (!terminate || !NeedsRls(operation, model))
        {
            return;
        }

        AppendRowLevelSecurity(operation.Schema, operation.Name, builder);
    }

    protected override void Generate(AddColumnOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(builder);

        base.Generate(operation, model, builder, terminate);

        if (!terminate || !string.Equals(operation.Name, RlsAnnotations.DefaultTenantColumn, StringComparison.Ordinal))
        {
            return;
        }

        if (FindModelFlag(model, operation.Schema, operation.Table) == false)
        {
            return;
        }

        AppendRowLevelSecurity(operation.Schema, operation.Table, builder);
    }

    private static bool NeedsRls(CreateTableOperation operation, IModel? model)
    {
        if (operation.FindAnnotation(RlsAnnotations.Enabled)?.Value is bool flag)
        {
            return flag;
        }

        if (FindModelFlag(model, operation.Schema, operation.Name) is { } modelFlag)
        {
            return modelFlag;
        }

        return operation.Columns.Any(c => string.Equals(c.Name, RlsAnnotations.DefaultTenantColumn, StringComparison.Ordinal));
    }

    private static bool? FindModelFlag(IModel? model, string? schema, string table)
    {
        if (model is null)
        {
            return null;
        }

        foreach (IEntityType entityType in model.GetEntityTypes())
        {
            if (string.Equals(entityType.GetTableName(), table, StringComparison.Ordinal)
                && string.Equals(entityType.GetSchema(), schema, StringComparison.Ordinal)
                && entityType.FindAnnotation(RlsAnnotations.Enabled)?.Value is bool flag)
            {
                return flag;
            }
        }

        return null;
    }

    private void AppendRowLevelSecurity(string? schema, string tableName, MigrationCommandListBuilder builder)
    {
        string table = Dependencies.SqlGenerationHelper.DelimitIdentifier(tableName, schema);
        string column = Dependencies.SqlGenerationHelper.DelimitIdentifier(RlsAnnotations.DefaultTenantColumn);
        string terminator = Dependencies.SqlGenerationHelper.StatementTerminator;
        string expression = RlsAnnotations.BuildPolicyExpression(column);

        Append(builder, $"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY", terminator);

        // FORCE — jadval egasi (app_migrator) ham siyosatni chetlab o'tolmaydi.
        Append(builder, $"ALTER TABLE {table} FORCE ROW LEVEL SECURITY", terminator);
        Append(builder, $"DROP POLICY IF EXISTS {RlsAnnotations.PolicyName} ON {table}", terminator);

        // WITH CHECK aniq beriladi — begona tenant qatorini YOZISH ham rad etiladi.
        Append(builder, $"CREATE POLICY {RlsAnnotations.PolicyName} ON {table} USING ({expression}) WITH CHECK ({expression})", terminator);
    }

    private static void Append(MigrationCommandListBuilder builder, string sql, string terminator) =>
        builder.Append(sql).AppendLine(terminator).EndCommand();
}
