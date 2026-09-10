using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Platform.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Persistence.Rls;

/// <summary>
/// Har Postgres ulanishi ochilganda <c>app.tenant_id</c> ni o'rnatadi (RLS asosi), yopilishda tozalaydi.
/// </summary>
/// <remarks>
/// Tenant yo'q → bo'sh satr → siyosat NULL → 0 qator (fail-closed). Tozalash pool
/// xavfsizligi uchun: ulanish keyingi so'rovga oldingi tenant bilan o'tib ketmasin.
/// Qiymat ULANISH ochilgan lahzada o'qiladi — shuning uchun tenant konteksti so'rov
/// boshida (birinchi baza so'rovidan OLDIN) qo'yilishi shart (Wash naqshi).
/// </remarks>
public sealed class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private const string SetTenantSql = "SELECT set_config('app.tenant_id', @tenant_id, false)";
    private const string ParameterName = "tenant_id";

    private readonly ICurrentTenant _currentTenant;

    public TenantConnectionInterceptor(ICurrentTenant currentTenant) => _currentTenant = currentTenant;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Apply(connection, CurrentValue());
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using DbCommand command = CreateCommand(connection, CurrentValue());
        await command.ExecuteNonQueryAsync(cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override InterceptionResult ConnectionClosing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        Reset(connection);
        return base.ConnectionClosing(connection, eventData, result);
    }

    public override ValueTask<InterceptionResult> ConnectionClosingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        Reset(connection);
        return base.ConnectionClosingAsync(connection, eventData, result);
    }

    private string CurrentValue() => _currentTenant.TenantId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty;

    private static DbCommand CreateCommand(DbConnection connection, string tenantId)
    {
        DbCommand command = connection.CreateCommand();
        command.CommandText = SetTenantSql;
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = ParameterName;
        parameter.Value = tenantId;
        command.Parameters.Add(parameter);
        return command;
    }

    private static void Apply(DbConnection connection, string tenantId)
    {
        using DbCommand command = CreateCommand(connection, tenantId);
        command.ExecuteNonQuery();
    }

    private static void Reset(DbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
        {
            return;
        }

        try
        {
            Apply(connection, string.Empty);
        }
        catch (DbException)
        {
            // Ulanish buzilgan — tozalashning ma'nosi yo'q.
        }
        catch (InvalidOperationException)
        {
            // Ulanish holati o'zgarib ketgan.
        }
    }
}
