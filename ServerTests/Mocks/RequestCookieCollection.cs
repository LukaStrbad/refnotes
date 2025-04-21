using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace ServerTests.Mocks;

public class RequestCookieCollection : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies = new();

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

    public bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _cookies.TryGetValue(key, out value);

    public int Count => _cookies.Count;
    public ICollection<string> Keys => _cookies.Keys;

    public string? this[string key]
    {
        get => !TryGetValue(key, out var value) ? null : value;
        set => _cookies[key] = value ?? throw new ArgumentNullException(nameof(value));
    }
}