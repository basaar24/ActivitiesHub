using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace EventsHub.Application.Core.Mapping;

public static class MappingServiceCollectionExtensions
{
    /// <summary>
    /// Finds every concrete <see cref="Profile"/> in <paramref name="assembly"/>, builds the mapper once
    /// (throwing on invalid or duplicate maps) and registers it as a singleton <see cref="IMapper"/>.
    /// </summary>
    public static IServiceCollection AddMapper(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var profiles = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                        && typeof(Profile).IsAssignableFrom(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .Select(CreateProfile)
            .ToList();

        return services.AddSingleton<IMapper>(new Mapper(profiles));
    }

    private static Profile CreateProfile(Type type)
    {
        try
        {
            return (Profile)Activator.CreateInstance(type, nonPublic: true)!;
        }
        catch (Exception ex)
        {
            var cause = ex is System.Reflection.TargetInvocationException { InnerException: { } inner } ? inner : ex;
            throw new InvalidOperationException($"Could not create mapping profile '{type.FullName}': {cause.Message}", cause);
        }
    }
}
