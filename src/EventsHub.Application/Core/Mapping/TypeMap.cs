using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace EventsHub.Application.Core.Mapping;

/// <summary>One registered source → destination map. Built once by <see cref="Mapper"/>, then executed as cached delegates.</summary>
internal abstract class TypeMap
{
    private static readonly MethodInfo MapNestedMethod =
        typeof(TypeMap).GetMethod(nameof(MapNested), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo MapCollectionMethod =
        typeof(TypeMap).GetMethod(nameof(MapCollection), BindingFlags.Static | BindingFlags.NonPublic)!;

    public abstract Type SourceType { get; }
    public abstract Type DestinationType { get; }

    /// <summary>The profile that registered this map (used in error messages).</summary>
    public Type? ProfileType { get; set; }

    /// <summary>Compiles the member-copy delegate. Called after every map is registered, so nested maps can be resolved.</summary>
    public abstract void Build(IReadOnlyDictionary<(Type Source, Type Destination), TypeMap> maps);

    public abstract object MapNew(object source);
    public abstract void MapInto(object source, object destination);

    protected static Expression? BuildValue(
        Expression source,
        Type sourceType,
        Type destinationType,
        IReadOnlyDictionary<(Type Source, Type Destination), TypeMap> maps)
    {
        if (destinationType.IsAssignableFrom(sourceType))
        {
            return sourceType == destinationType ? source : Expression.Convert(source, destinationType);
        }

        if (maps.TryGetValue((sourceType, destinationType), out var nestedMap))
        {
            var mapped = Expression.Call(
                MapNestedMethod,
                Expression.Constant(nestedMap),
                Expression.Convert(source, typeof(object)));
            return Expression.Convert(mapped, destinationType);
        }

        if (TryGetElementType(sourceType, out var sourceElement)
            && TryGetElementType(destinationType, out var destinationElement)
            && maps.TryGetValue((sourceElement, destinationElement), out var elementMap)
            && destinationType.IsAssignableFrom(typeof(List<>).MakeGenericType(destinationElement)))
        {
            var mapped = Expression.Call(
                MapCollectionMethod,
                Expression.Constant(elementMap),
                Expression.Constant(destinationElement),
                Expression.Convert(source, typeof(IEnumerable)));
            return Expression.Convert(mapped, destinationType);
        }

        return null;
    }

    private static object? MapNested(TypeMap map, object? source) =>
        source is null ? null : map.MapNew(source);

    private static object? MapCollection(TypeMap elementMap, Type destinationElement, IEnumerable? source)
    {
        if (source is null)
        {
            return null;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(destinationElement))!;
        foreach (var item in source)
        {
            list.Add(item is null ? null : elementMap.MapNew(item));
        }

        return list;
    }

    private static bool TryGetElementType(Type type, out Type element)
    {
        element = null!;
        if (type == typeof(string))
        {
            return false;
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerable is null)
        {
            return false;
        }

        element = enumerable.GetGenericArguments()[0];
        return true;
    }
}

internal sealed class TypeMap<TSource, TDestination> : TypeMap, IMappingExpression<TSource, TDestination>
    where TSource : class
    where TDestination : class
{
    // Destination property name -> MapFrom expression, or null when the member is ignored.
    private readonly Dictionary<string, LambdaExpression?> _overrides = new(StringComparer.Ordinal);
    private Action<TSource, TDestination>? _copy;
    private Func<TDestination>? _create;

    public override Type SourceType => typeof(TSource);
    public override Type DestinationType => typeof(TDestination);

    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberOptions<TSource, TMember>> options)
    {
        ArgumentNullException.ThrowIfNull(destinationMember);
        ArgumentNullException.ThrowIfNull(options);

        var property = GetWritableProperty(destinationMember);
        var memberOptions = new MemberOptions<TSource, TMember>();
        options(memberOptions);
        if (!memberOptions.IsConfigured)
        {
            throw new ArgumentException(
                $"ForMember({property.Name}) must call MapFrom or Ignore.", nameof(options));
        }

        _overrides[property.Name] = memberOptions.MapFrom;
        return this;
    }

    public override void Build(IReadOnlyDictionary<(Type Source, Type Destination), TypeMap> maps)
    {
        if (typeof(TDestination).IsAbstract || typeof(TDestination).GetConstructor(Type.EmptyTypes) is null)
        {
            throw new InvalidOperationException(
                $"Cannot map to '{typeof(TDestination).FullName}': it needs a public parameterless constructor.");
        }

        var source = Expression.Parameter(typeof(TSource), "source");
        var destination = Expression.Parameter(typeof(TDestination), "destination");
        var body = new List<Expression>();

        var sourceProperties = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        foreach (var property in typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0)
            {
                sourceProperties.TryAdd(property.Name, property);
            }
        }

        foreach (var destinationProperty in typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (destinationProperty.SetMethod is not { IsPublic: true } || destinationProperty.GetIndexParameters().Length > 0)
            {
                continue;
            }

            Expression? value;
            if (_overrides.TryGetValue(destinationProperty.Name, out var mapFrom))
            {
                if (mapFrom is null)
                {
                    continue;
                }

                value = Expression.Invoke(mapFrom, source);
                if (value.Type != destinationProperty.PropertyType)
                {
                    value = Expression.Convert(value, destinationProperty.PropertyType);
                }
            }
            else if (sourceProperties.TryGetValue(destinationProperty.Name, out var sourceProperty))
            {
                value = BuildValue(
                    Expression.Property(source, sourceProperty),
                    sourceProperty.PropertyType,
                    destinationProperty.PropertyType,
                    maps);
            }
            else
            {
                continue;
            }

            if (value is not null)
            {
                body.Add(Expression.Assign(Expression.Property(destination, destinationProperty), value));
            }
        }

        if (body.Count == 0)
        {
            body.Add(Expression.Empty());
        }

        _copy = Expression.Lambda<Action<TSource, TDestination>>(
            Expression.Block(typeof(void), body), source, destination).Compile();
        _create = Expression.Lambda<Func<TDestination>>(Expression.New(typeof(TDestination))).Compile();
    }

    public override object MapNew(object source)
    {
        var destination = _create!();
        _copy!((TSource)source, destination);
        return destination;
    }

    public override void MapInto(object source, object destination) =>
        _copy!((TSource)source, (TDestination)destination);

    private static PropertyInfo GetWritableProperty<TMember>(Expression<Func<TDestination, TMember>> selector)
    {
        var body = selector.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Member: PropertyInfo property, Expression: ParameterExpression parameter }
            && parameter == selector.Parameters[0]
            && property.SetMethod is { IsPublic: true }
            && property.GetIndexParameters().Length == 0)
        {
            return property;
        }

        throw new ArgumentException(
            $"'{selector}' must select a public writable property of {typeof(TDestination).Name}, for example d => d.Name.",
            nameof(selector));
    }

    private sealed class MemberOptions<TFrom, TMember> : IMemberOptions<TFrom, TMember>
    {
        public bool IsConfigured { get; private set; }
        public LambdaExpression? MapFrom { get; private set; }

        void IMemberOptions<TFrom, TMember>.MapFrom(Expression<Func<TFrom, TMember>> source)
        {
            ArgumentNullException.ThrowIfNull(source);
            MapFrom = source;
            IsConfigured = true;
        }

        void IMemberOptions<TFrom, TMember>.Ignore()
        {
            MapFrom = null;
            IsConfigured = true;
        }
    }
}
