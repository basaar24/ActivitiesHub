namespace EventsHub.Application.Core.Mapping;

/// <summary>
/// Maps objects using the maps declared in the supplied profiles. All maps are validated and compiled in the
/// constructor, so configuration mistakes surface at startup. Instances are immutable afterwards and thread-safe.
/// </summary>
public sealed class Mapper : IMapper
{
    private readonly Dictionary<(Type Source, Type Destination), TypeMap> _maps = [];

    public Mapper(params Profile[] profiles) : this((IEnumerable<Profile>)profiles)
    {
    }

    public Mapper(IEnumerable<Profile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        foreach (var profile in profiles)
        {
            foreach (var map in profile.Maps)
            {
                map.ProfileType = profile.GetType();
                if (!_maps.TryAdd((map.SourceType, map.DestinationType), map))
                {
                    var existing = _maps[(map.SourceType, map.DestinationType)];
                    throw new InvalidOperationException(
                        $"Duplicate map from '{map.SourceType.FullName}' to '{map.DestinationType.FullName}' " +
                        $"registered by profiles '{existing.ProfileType!.Name}' and '{map.ProfileType.Name}'.");
                }
            }
        }

        foreach (var map in _maps.Values)
        {
            map.Build(_maps);
        }
    }

    public TDestination Map<TDestination>(object source) where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return (TDestination)GetMap(source.GetType(), typeof(TDestination)).MapNew(source);
    }

    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        where TSource : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        GetMap(typeof(TSource), typeof(TDestination)).MapInto(source, destination);
        return destination;
    }

    private TypeMap GetMap(Type source, Type destination) =>
        _maps.TryGetValue((source, destination), out var map)
            ? map
            : throw new InvalidOperationException(
                $"No map from '{source.FullName}' to '{destination.FullName}' is registered. Add CreateMap in a Profile.");
}
