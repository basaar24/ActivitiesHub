using System.Linq.Expressions;

namespace EventsHub.Application.Core.Mapping;

/// <summary>Configures one source → destination map. Returned by <see cref="Profile"/>'s <c>CreateMap</c>.</summary>
public interface IMappingExpression<TSource, TDestination>
{
    /// <summary>Overrides how a single destination member is filled, taking precedence over name matching.</summary>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TMember>> options);
}

/// <summary>Options for one destination member.</summary>
public interface IMemberOptions<TSource, TMember>
{
    /// <summary>Fills the member from an expression over the source.</summary>
    void MapFrom(Expression<Func<TSource, TMember>> source);

    /// <summary>Never writes the member.</summary>
    void Ignore();
}
