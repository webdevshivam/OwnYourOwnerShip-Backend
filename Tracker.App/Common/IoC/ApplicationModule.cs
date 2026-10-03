using Autofac;
using Tracker.App.Services;

namespace Tracker.App.Common.IoC;

/// <summary>
/// Autofac module for registering application-level dependencies.
/// Using Autofac Modules keeps Program.cs clean and allows organizing
/// service registrations by feature or layer.
/// </summary>
public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        // Register Redis Cache Service as a Singleton
        builder.RegisterType<RedisCacheService>()
            .As<ICacheService>()
            .SingleInstance();

        // Future repositories, services, and scorers can be registered here
        // or through assembly scanning:
        // builder.RegisterAssemblyTypes(typeof(ApplicationModule).Assembly)
        //     .Where(t => t.Name.EndsWith("Service") || t.Name.EndsWith("Repository"))
        //     .AsImplementedInterfaces()
        //     .InstancePerLifetimeScope();
    }
}
