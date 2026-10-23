using System.Reflection;
using EnglishAdventure.Domain.Common;

namespace EnglishAdventure.Domain.UnitTests.Architecture;

/// <summary>
/// Enforces the Domain layer and module rules from ADR-1 / ADR-2 by reflection (no architecture-test package).
/// </summary>
public sealed class DomainArchitectureTests
{
    private const string EntitiesNamespace = "EnglishAdventure.Domain.Entities";

    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

    [Fact]
    public void Domain_references_no_other_project_or_framework_package()
    {
        var references = DomainAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !IsBaseClassLibrary(name))
            .ToList();

        Assert.Empty(references);
    }

    [Fact]
    public void Classes_in_the_Entities_namespace_derive_from_Entity()
    {
        var violations = EntityNamespaceClasses()
            .Where(type => !type.IsAssignableTo(typeof(Entity)))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Entities_do_not_hold_entities_of_another_module()
    {
        var violations = new List<string>();

        foreach (var type in EntityNamespaceClasses())
        {
            var module = ModuleOf(type);
            const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var heldTypes = type.GetProperties(members).Select(property => (property.Name, property.PropertyType))
                .Concat(type.GetFields(members).Select(field => (field.Name, field.FieldType)));

            foreach (var (name, heldType) in heldTypes)
            {
                foreach (var referenced in TypeAndGenericArguments(heldType))
                {
                    if (referenced.Namespace?.StartsWith(EntitiesNamespace, StringComparison.Ordinal) == true
                        && ModuleOf(referenced) != module)
                    {
                        violations.Add($"{type.FullName}.{name} holds {referenced.FullName}; reference it by Id instead.");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    private static IEnumerable<Type> EntityNamespaceClasses() =>
        DomainAssembly.GetTypes().Where(type =>
            type.IsClass
            && !type.IsNested
            && type.Namespace?.StartsWith(EntitiesNamespace + ".", StringComparison.Ordinal) == true);

    // EnglishAdventure.Domain.Entities.<Module>[.…]
    private static string ModuleOf(Type type) =>
        type.Namespace![(EntitiesNamespace.Length + 1)..].Split('.')[0];

    private static IEnumerable<Type> TypeAndGenericArguments(Type type)
    {
        yield return type.IsArray ? type.GetElementType()! : type;

        foreach (var argument in type.GetGenericArguments().SelectMany(TypeAndGenericArguments))
        {
            yield return argument;
        }
    }

    private static bool IsBaseClassLibrary(string assemblyName) =>
        assemblyName is "System" or "netstandard" or "mscorlib"
        || assemblyName.StartsWith("System.", StringComparison.Ordinal);
}
