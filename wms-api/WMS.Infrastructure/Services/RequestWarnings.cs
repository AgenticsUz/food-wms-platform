using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services;

/// <inheritdoc />
public class RequestWarnings : IRequestWarnings
{
    private readonly List<(string Code, string Template, object?[] Args)> _items = new();

    public void Add(string code, string messageTemplate, params object?[] args)
        => _items.Add((code, messageTemplate, args ?? []));

    public (string Code, string Template, object?[] Args)? First
        => _items.Count == 0 ? null : _items[0];
}
