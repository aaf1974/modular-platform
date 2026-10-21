namespace Platform.Core.Modules;

/// <summary>
/// Represents a module that registers its services in the dependency
/// injection container.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Registers the services provided by this module.
    /// </summary>
    /// <param name="context">
    /// The context containing the service collection and application
    /// configuration.
    /// </param>
    void RegisterServices(ModuleContext context);
}