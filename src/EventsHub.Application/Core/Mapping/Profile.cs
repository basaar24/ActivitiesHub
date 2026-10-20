namespace EventsHub.Application.Core.Mapping;

/// <summary>
/// Base class for a group of related maps. Declare maps with <see cref="CreateMap{TSource, TDestination}"/>
/// in the constructor; profiles in an assembly are found by <c>AddMapper</c>.
/// A profile instance belongs to a single <see cref="Mapper"/>.
/// </summary>
public abstract class Profile
{
    private readonly List<TypeMap> _maps = [];

    internal IReadOnlyList<TypeMap> Maps => _maps;

    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        where TSource : class
        where TDestination : class
    {
        var map = new TypeMap<TSource, TDestination>();
        _maps.Add(map);
        return map;
    }
}
