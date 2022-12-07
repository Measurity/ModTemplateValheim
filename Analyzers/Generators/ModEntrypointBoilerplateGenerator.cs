using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzers.Generators;

/// <summary>
///     Wires up mod entrypoint types with attribute and info required by BepInEx.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class ModMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Setup pipeline that targets BepInEx mod classes.
        IncrementalValuesProvider<ITypeSymbol> modEntrypoints = context.SyntaxProvider
            .CreateSyntaxProvider(static (node, _) => SupportedModClassFilter(node), static (syntaxContext, _) => TransformAsBepinexEntrypoint(syntaxContext))
            .Where(i => i != null)!;
        IncrementalValueProvider<(Compilation Compilation, ImmutableArray<ITypeSymbol> Nodes)> compilationAndClasses =
            context.CompilationProvider.Combine(modEntrypoints.Collect());
        context.RegisterSourceOutput(compilationAndClasses, static (context, source) => Execute(context, source.Compilation, source.Nodes));
    }

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<ITypeSymbol> modClasses)
    {
        if (modClasses.IsDefaultOrEmpty)
        {
            return;
        }

        (string author, string modName, string version) = ExtractMetadataFromAssembly(compilation.Assembly);
        foreach (ITypeSymbol modClass in modClasses)
        {
            string sourceFileName = modClass.ContainingNamespace.IsGlobalNamespace ? $"{modClass.Name}.g.cs" : $"{modClass.ContainingNamespace}.{modClass.Name}.g.cs";
            context.AddSource(sourceFileName, $@"
using BepInEx;
using HarmonyLib;

{(modClass.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {modClass.ContainingNamespace};{Environment.NewLine}")}
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public partial class {modClass.Name}
{{
    public const string PluginAuthor = ""{author}"";
    public const string PluginGuid = ""com.github.{CleanupName(author).ToLowerInvariant()}.{CleanupName(modName).ToLowerInvariant()}"";
    public const string PluginName = ""{modName}"";
    public const string PluginVersion = ""{version}"";
}}
");
        }
    }

    private static bool SupportedModClassFilter(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax clazz)
        {
            return false;
        }
        foreach (SyntaxToken modifier in clazz.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }
        return false;
    }

    private static ITypeSymbol? TransformAsBepinexEntrypoint(GeneratorSyntaxContext context)
    {
        ClassDeclarationSyntax modClass = (ClassDeclarationSyntax)context.Node;
        if (ModelExtensions.GetDeclaredSymbol(context.SemanticModel, modClass) is not ITypeSymbol classTypeSymbol)
        {
            return null;
        }
        if (classTypeSymbol.BaseType is not { Name: "BaseUnityPlugin", ContainingNamespace.Name: "BepInEx" })
        {
            return null;
        }

        return classTypeSymbol;
    }

    private static (string author, string modName, string version) ExtractMetadataFromAssembly(IAssemblySymbol assembly)
    {
        var author = "";
        var version = "1.0.0.0";
        var modName = "";
        foreach (AttributeData attribute in assembly.GetAttributes())
        {
            switch (attribute.AttributeClass?.Name)
            {
                case "AssemblyTitleAttribute":
                    modName = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
                    break;
                case "AssemblyCompanyAttribute":
                    author = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "";
                    break;
                case "AssemblyVersionAttribute":
                    version = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "1.0.0.0";
                    break;
            }
        }
        return (author, modName, version);
    }
    
    private static string CleanupName(string name)
    {
        StringBuilder sb = new();
        foreach (char c in name)
        {
            object? newValue = c switch
            {
                >= 'a' and <= 'z' => c,
                >= 'A' and <= 'Z' => c,
                _ => null
            };
            if (newValue != null)
            {
                sb.Append(newValue);
            }
        }
        return sb.ToString();
    }
}