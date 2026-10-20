namespace EventsHub.Application.Core.Mapping;

/// <summary>Maps objects using the maps registered by <see cref="Profile"/> classes.</summary>
public interface IMapper
{
    /// <summary>Creates a new <typeparamref name="TDestination"/> populated from <paramref name="source"/>.</summary>
    TDestination Map<TDestination>(object source) where TDestination : class;

    /// <summary>Copies mapped members from <paramref name="source"/> onto <paramref name="destination"/> and returns it.</summary>
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        where TSource : class
        where TDestination : class;
}
