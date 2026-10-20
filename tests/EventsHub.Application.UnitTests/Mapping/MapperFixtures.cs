using EventsHub.Application.Core.Mapping;

namespace EventsHub.Application.UnitTests.Mapping;

public class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Secret { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string ReadOnly { get; set; } = "from-source";
}

public class PersonView
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Secret { get; set; } = "original-secret";
    public string Label { get; set; } = "original-label";
    public string Untouched { get; set; } = "keep";
    public string ReadOnly { get; } = "fixed";
}

public class ChildSource
{
    public string Name { get; set; } = "";
}

public class ChildDest
{
    public string Name { get; set; } = "";
}

public class ParentSource
{
    public ChildSource? Child { get; set; }
    public List<ChildSource> Items { get; set; } = [];
    public ChildSource[] Extras { get; set; } = [];
}

public class ParentDest
{
    public ChildDest? Child { get; set; }
    public List<ChildDest> Items { get; set; } = [];
    public IReadOnlyList<ChildDest> Extras { get; set; } = [];
}

public class Immutable
{
    public Immutable(string name) => Name = name;
    public string Name { get; }
}

public class Unrelated
{
}

/// <summary>
/// Discovered by <c>AddMapper</c> when it scans this assembly. It is the only concrete, non-generic
/// profile here; profiles for individual tests use <see cref="TestProfile{TTag}"/>.
/// </summary>
public class SamplePersonProfile : Profile
{
    public SamplePersonProfile() => CreateMap<Person, PersonView>();
}

/// <summary>
/// Lets a test declare maps inline. It is an open generic type, which <c>AddMapper</c> skips when scanning,
/// so deliberately broken profiles used by tests never interfere with the assembly-scan test.
/// </summary>
public sealed class TestProfile<TTag> : Profile
{
    public TestProfile(Action<TestProfile<TTag>> configure) => configure(this);

    public IMappingExpression<TSource, TDestination> Map<TSource, TDestination>()
        where TSource : class
        where TDestination : class
        => CreateMap<TSource, TDestination>();
}
