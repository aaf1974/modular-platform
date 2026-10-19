namespace Platform.Core.Modules;

/// <summary>
/// Declares required module dependencies.
/// </summary>
public interface IModuleDependencies
{
    IReadOnlyCollection<Type> Dependencies { get; }
}

/*
public sealed class BusinessModule : IModule, IModuleDependencies
{
    public IReadOnlyCollection<Type> Dependencies =>
        [typeof(AuthenticationModule)];

    public void RegisterServices(ModuleContext context)
    {
        // Register Business services.
    }
}
*/