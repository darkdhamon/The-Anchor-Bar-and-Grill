using Microsoft.Extensions.Options;

namespace Anchor.Domain.Tests.Support;

internal sealed class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T> where T : class
{
    public T CurrentValue { get; private set; } = currentValue;

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener) => NoOpDisposable.Instance;

    public void Set(T nextValue) => CurrentValue = nextValue;

    private sealed class NoOpDisposable : IDisposable
    {
        public static readonly NoOpDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
