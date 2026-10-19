namespace Platform.Core.Modules;

public interface IModule
{
    void RegisterServices(ModuleContext context);
}