using EventsHub.Application.Core.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace EventsHub.Application.UnitTests.Mapping;

[TestFixture]
public class MapperTests
{
    private static Mapper PersonMapper(Action<IMappingExpression<Person, PersonView>>? configure = null) =>
        new(new TestProfile<object>(p =>
        {
            var map = p.Map<Person, PersonView>();
            configure?.Invoke(map);
        }));

    // ---- Map onto an existing destination / convention matching / overrides ----

    [Test]
    public void Map_OntoExisting_CopiesMatchingMembersAndReturnsSameInstance()
    {
        var mapper = PersonMapper();
        var source = new Person { Name = "Ana", Age = 30, Secret = "s3" };
        var destination = new PersonView();

        var result = mapper.Map(source, destination);

        Assert.That(result, Is.SameAs(destination));
        Assert.Multiple(() =>
        {
            Assert.That(destination.Name, Is.EqualTo("Ana"));
            Assert.That(destination.Age, Is.EqualTo(30));
            Assert.That(destination.Secret, Is.EqualTo("s3"));
        });
    }

    [Test]
    public void Map_WhenDestinationMemberHasNoSourceOrNoSetter_LeavesItUnchangedWithoutError()
    {
        var mapper = PersonMapper();
        var destination = new PersonView();

        Assert.DoesNotThrow(() => mapper.Map(new Person { ReadOnly = "changed" }, destination));

        Assert.Multiple(() =>
        {
            Assert.That(destination.Untouched, Is.EqualTo("keep"), "no same-named source member");
            Assert.That(destination.ReadOnly, Is.EqualTo("fixed"), "no public setter");
        });
    }

    [Test]
    public void Map_WhenMemberIgnored_PreservesDestinationValue()
    {
        var mapper = PersonMapper(m => m.ForMember(d => d.Secret, o => o.Ignore()));
        var destination = new PersonView();

        mapper.Map(new Person { Name = "Ana", Secret = "leak" }, destination);

        Assert.Multiple(() =>
        {
            Assert.That(destination.Secret, Is.EqualTo("original-secret"));
            Assert.That(destination.Name, Is.EqualTo("Ana"));
        });
    }

    [Test]
    public void Map_WhenMemberComputed_UsesExpressionEvenIfSourceHasSameName()
    {
        var mapper = PersonMapper(m => m
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Name.ToUpperInvariant()))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Nickname)));
        var destination = new PersonView();

        mapper.Map(new Person { Name = "Ana", Nickname = "Anita" }, destination);

        Assert.Multiple(() =>
        {
            Assert.That(destination.Label, Is.EqualTo("ANA"));
            Assert.That(destination.Name, Is.EqualTo("Anita"), "override beats name matching");
        });
    }

    // ---- Map into a new destination ----

    [Test]
    public void Map_IntoNewInstance_ReturnsPopulatedDistinctObject()
    {
        var mapper = PersonMapper();
        var source = new Person { Name = "Ana", Age = 30 };

        var result = mapper.Map<PersonView>(source);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Ana"));
            Assert.That(result.Age, Is.EqualTo(30));
            Assert.That(result.Untouched, Is.EqualTo("keep"));
        });
    }

    [Test]
    public void Map_WhenSourceIsNull_ThrowsArgumentNullException()
    {
        var mapper = PersonMapper();

        Assert.Throws<ArgumentNullException>(() => mapper.Map<PersonView>(null!));
        Assert.Throws<ArgumentNullException>(() => mapper.Map<Person, PersonView>(null!, new PersonView()));
    }

    [Test]
    public void Constructor_WhenDestinationHasNoParameterlessConstructor_ThrowsNamingTheType()
    {
        var profile = new TestProfile<object>(p => p.Map<Person, Immutable>());

        var ex = Assert.Throws<InvalidOperationException>(() => new Mapper(profile));

        Assert.That(ex.Message, Does.Contain(nameof(Immutable)));
    }

    // ---- Nested and collection members ----

    [Test]
    public void Map_WhenMemberTypesDifferAndMapExists_MapsNestedObject()
    {
        var mapper = ParentMapper(childFirst: false);

        var result = mapper.Map<ParentDest>(new ParentSource { Child = new ChildSource { Name = "kid" } });

        Assert.That(result.Child, Is.Not.Null);
        Assert.That(result.Child!.Name, Is.EqualTo("kid"));
    }

    [Test]
    public void Map_WhenNestedSourceIsNull_LeavesDestinationNull()
    {
        var mapper = ParentMapper(childFirst: false);

        var result = mapper.Map<ParentDest>(new ParentSource { Child = null });

        Assert.That(result.Child, Is.Null);
    }

    [TestCase(false, TestName = "Map_NestedProfileRegisteredAfterOuter")]
    [TestCase(true, TestName = "Map_NestedProfileRegisteredBeforeOuter")]
    public void Map_ResolvesNestedMapsRegardlessOfProfileOrder(bool childFirst)
    {
        var mapper = ParentMapper(childFirst);

        var result = mapper.Map<ParentDest>(new ParentSource
        {
            Child = new ChildSource { Name = "kid" },
            Items = [new ChildSource { Name = "a" }],
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Child!.Name, Is.EqualTo("kid"));
            Assert.That(result.Items.Select(i => i.Name), Is.EqualTo(new[] { "a" }));
        });
    }

    [Test]
    public void Map_WhenMemberIsCollectionOfMappedElements_MapsEachElementInOrder()
    {
        var mapper = ParentMapper(childFirst: false);
        var source = new ParentSource
        {
            Items = [new ChildSource { Name = "a" }, new ChildSource { Name = "b" }, new ChildSource { Name = "c" }],
            Extras = [new ChildSource { Name = "x" }, new ChildSource { Name = "y" }],
        };

        var result = mapper.Map<ParentDest>(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items.Select(i => i.Name), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(result.Extras.Select(i => i.Name), Is.EqualTo(new[] { "x", "y" }), "array -> IReadOnlyList");
            Assert.That(result.Items, Is.All.TypeOf<ChildDest>());
        });
    }

    private static Mapper ParentMapper(bool childFirst)
    {
        var parent = new TestProfile<int>(p => p.Map<ParentSource, ParentDest>());
        var child = new TestProfile<string>(p => p.Map<ChildSource, ChildDest>());
        return childFirst ? new Mapper(child, parent) : new Mapper(parent, child);
    }

    // ---- Failure modes ----

    [Test]
    public void Map_WhenNoMapRegistered_ThrowsNamingBothTypes()
    {
        var mapper = PersonMapper();

        var ex1 = Assert.Throws<InvalidOperationException>(() => mapper.Map<PersonView>(new Unrelated()));
        var ex2 = Assert.Throws<InvalidOperationException>(() => mapper.Map(new Unrelated(), new PersonView()));

        foreach (var ex in new[] { ex1, ex2 })
        {
            Assert.That(ex.Message, Does.Contain(nameof(Unrelated)).And.Contain(nameof(PersonView)));
        }
    }

    [Test]
    public void Constructor_WhenTwoProfilesRegisterSamePair_ThrowsNamingThePair()
    {
        var first = new TestProfile<int>(p => p.Map<Person, PersonView>());
        var second = new TestProfile<string>(p => p.Map<Person, PersonView>());

        var ex = Assert.Throws<InvalidOperationException>(() => new Mapper(first, second));

        Assert.That(ex.Message, Does.Contain("Duplicate").And.Contain(nameof(Person)).And.Contain(nameof(PersonView)));
    }

    [Test]
    public void ForMember_WhenSelectorIsNotAWritableProperty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PersonMapper(m => m.ForMember(d => d.Name.Length, o => o.Ignore())),
            "nested member access");
        Assert.Throws<ArgumentException>(() => PersonMapper(m => m.ForMember(d => d.ReadOnly, o => o.Ignore())),
            "no public setter");
    }

    [Test]
    public void ForMember_WhenNoMapFromOrIgnore_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PersonMapper(m => m.ForMember(d => d.Name, _ => { })));
    }

    // ---- Registration ----

    [Test]
    public void AddMapper_ScansAssemblyForProfiles_AndRegistersSingletonMapper()
    {
        var services = new ServiceCollection();

        services.AddMapper(typeof(MapperTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var result = mapper.Map<PersonView>(new Person { Name = "Ana" });
        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Ana"));
            Assert.That(provider.GetRequiredService<IMapper>(), Is.SameAs(mapper));
        });
    }
}
