using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Analyzers.Generators;

/// <summary>
///     Generates missing assembly info attributes based on already given information in the assembly.
/// </summary>
[Generator]
public class MissingAssemblyInfoGenerator : IIncrementalGenerator
{
    private static readonly Dictionary<string, Func<IAssemblySymbol, string>> attributeCreatorLookup = new()
    {
        { "AssemblyFileVersion", symbol => GetAssemblyAttributeValue(symbol, "AssemblyVersionAttribute") ?? "" },
        { "AssemblyTitle", symbol => symbol.Name },
        { "AssemblyProduct", symbol => symbol.Name },
        { "AssemblyCopyright", symbol => $"Copyright © {GetAssemblyAttributeValue(symbol, "AssemblyCompanyAttribute")} {DateTimeOffset.UtcNow.Year}" }
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider.Select((c, _) => c.Assembly), static (productionContext, assembly) => Execute(productionContext, assembly));
    }

    private static void Execute(SourceProductionContext context, IAssemblySymbol assembly)
    {
        ImmutableHashSet<string?> existingAttributes = assembly.GetAttributes()
            .Select(a =>
            {
                var name = a.AttributeClass?.Name;
                if (name == null)
                {
                    return null;
                }
                return name.Remove(name.LastIndexOf("Attribute", StringComparison.OrdinalIgnoreCase), 9);
            })
            .Where(i => i != null)
            .ToImmutableHashSet();
        IEnumerable<KeyValuePair<string, Func<IAssemblySymbol, string>>> attributesToAdd = attributeCreatorLookup.Where(creator => !existingAttributes.Contains(creator.Key));

        StringBuilder attributesSource = new();
        foreach (KeyValuePair<string, Func<IAssemblySymbol, string>> pair in attributesToAdd)
        {
            attributesSource.Append("[assembly: ")
                .Append(pair.Key)
                .Append(@"(""")
                .Append(pair.Value(assembly))
                .AppendLine(@""")]");
        }
        context.AddSource("AssemblyInfo.g.cs", $@"
using System.Reflection;
using System.Runtime.InteropServices;

{attributesSource}
");
    }

    private static string? GetAssemblyAttributeValue(IAssemblySymbol assemblySymbol, string attributeName)
    {
        return assemblySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == attributeName)?.ConstructorArguments.FirstOrDefault().Value?.ToString();
    }
}