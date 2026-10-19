namespace Platform.Core.Modules;

internal sealed class ModuleRegistration
{
    public Type ModuleType { get; }

    public IReadOnlyCollection<Type> Dependencies { get; }

    public ModuleRegistration(
        Type moduleType,
        IReadOnlyCollection<Type> dependencies)
    {
        ModuleType = moduleType;
        Dependencies = dependencies;
    }
}