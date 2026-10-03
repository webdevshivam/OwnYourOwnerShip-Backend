using Autofac;
using Tracker.App.Features.Auth.Interface;
using Tracker.App.Features.Auth.Repository;
using Tracker.App.Features.Auth.Service;
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

        // Register Authentication & Token Services (Scoped per HTTP request)
        builder.RegisterType<AuthRepository>()
            .As<IAuthRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<TokenService>()
            .As<ITokenService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<AuthService>()
            .As<IAuthService>()
            .InstancePerLifetimeScope();
    }
}
