namespace Platform.Core.Modules;

/// <summary>
/// Declares the modules required by a module.
/// </summary>
/// <remarks>
/// A dependency must be registered before module validation is performed.
/// This interface does not impose an order in which modules must be registered.
/// </remarks>
public interface IModuleDependencies
{
    /// <summary>
    /// Gets the types of modules required by this module.
    /// </summary>
    /// <remarks>
    /// The collection must not be null and must not contain null values,
    /// duplicate types, or the declaring module's own type.
    /// Every dependency type must implement <see cref="IModule"/>.
    /// </remarks>
    IReadOnlyCollection<Type> Dependencies { get; }
}
