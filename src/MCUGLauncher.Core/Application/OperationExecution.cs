namespace BlockLaunch;

// Flows the current commit boundary through asynchronous service calls.
public static class OperationExecution
{
    internal static readonly AsyncLocal<OperationContext?> Ambient = new();

    public static IDisposable BeginCommit() => Ambient.Value?.BeginCommit() ?? new Empty();

    sealed class Empty : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
