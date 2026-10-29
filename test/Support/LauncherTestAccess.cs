using System.Reflection;
using BlockLaunch;

// Test-only access to feature modules. Production MainWindow does not expose compatibility fields.
public static class LauncherTestAccess
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    static IEnumerable<object> Owners(MainWindow window)
    {
        yield return window;
        var workspace = typeof(MainWindow).GetProperty("Workspace", Instance)!.GetValue(window)!;
        foreach (var property in workspace.GetType().GetProperties(Instance).Where(p => p.Name != "Window"))
            if (property.GetValue(workspace) is { } owner)
                yield return owner;
    }

    public static object Get(MainWindow window, string name)
    {
        foreach (var owner in Owners(window))
        {
            if (owner.GetType().GetField(name, Instance) is { } field)
                return field.GetValue(owner)!;
            if (owner.GetType().GetProperty(name, Instance) is { } property)
                return property.GetValue(owner)!;
        }
        throw new MissingFieldException("No feature owns field " + name);
    }

    public static void Set(MainWindow window, string name, object? value)
    {
        foreach (var owner in Owners(window))
            if (owner.GetType().GetField(name, Instance) is { } field)
            {
                field.SetValue(owner, value);
                return;
            }
        throw new MissingFieldException("No feature owns field " + name);
    }

    public static object? Invoke(MainWindow window, string name, params object?[]? arguments)
    {
        foreach (var owner in Owners(window))
            if (owner.GetType().GetMethod(name, Instance) is { } method)
                return method.Invoke(owner, arguments);
        throw new MissingMethodException("No feature owns method " + name);
    }

    public static object? InvokeStatic(string name, params object?[]? arguments)
    {
        var methods = typeof(MainWindow).Assembly.GetTypes().Where(t => t.Namespace == "BlockLaunch" && t.Name.EndsWith("Controller"))
            .Select(t => t.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)).OfType<MethodInfo>().ToArray();
        if (methods.Length != 1)
            throw new MissingMethodException("Expected one static feature method: " + name);
        return methods[0].Invoke(null, arguments);
    }
}
