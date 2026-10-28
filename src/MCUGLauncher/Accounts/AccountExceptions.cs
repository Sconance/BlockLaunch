namespace BlockLaunch;

public class AccountStorageException(string message, Exception? inner = null) : Exception(message, inner);

public class AccountLoginException(string message, bool requiresInteractive = false) : Exception(message)
{
    public bool RequiresInteractive { get; } = requiresInteractive;
}
