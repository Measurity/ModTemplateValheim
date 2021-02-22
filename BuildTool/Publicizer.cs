using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace BuildTool
{
    public static class Publicizer
    {
        public static IEnumerable<string> Execute(IEnumerable<string> files, string outputSuffix = "", string outputPath = null)
        {
            // Ensure target directory exists.
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Create dependency resolve for cecil (needed to write dlls that have other dependencies).
            var resolver = new DefaultAssemblyResolver();

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    throw new FileNotFoundException("Dll to publicize not found ", file);
                }
                resolver.AddSearchDirectory(Path.GetDirectoryName(file));

                var outputName = $"{Path.GetFileNameWithoutExtension(file)}{outputSuffix}{Path.GetExtension(file)}";
                var outputFile = Path.Combine(outputPath, outputName);
                Publicize(file, resolver).Write(outputFile);
                yield return outputFile;
            }
        }

        private static AssemblyDefinition Publicize(string file, BaseAssemblyResolver dllResolver)
        {
            var assembly = AssemblyDefinition.ReadAssembly(file,
                new ReaderParameters
                {
                    AssemblyResolver = dllResolver
                });
            var allTypes = GetAllTypes(assembly.MainModule).ToList();
            var allMethods = allTypes.SelectMany(t => t.Methods);
            var allFields = FilterBackingEventFields(allTypes);

            foreach (var type in allTypes)
            {
                if (!(type?.IsPublic ?? false)) continue;
                    
                if (type.IsNested)
                    type.IsNestedPublic = true;
                else
                    type.IsPublic = true;
            }
            foreach (var method in allMethods)
            {
                if (!method?.IsPublic ?? false)
                {
                    method.IsPublic = true;
                }
            }
            foreach (var field in allFields)
            {
                if (!field?.IsPublic ?? false)
                {
                    field.IsPublic = true;
                }
            }

            return assembly;
        }

        public static IEnumerable<FieldDefinition> FilterBackingEventFields(IEnumerable<TypeDefinition> allTypes)
        {
            var eventNames = allTypes.SelectMany(t => t.Events)
                .Select(eventDefinition => eventDefinition.Name)
                .ToList();

            return allTypes.SelectMany(x => x.Fields)
                .Where(fieldDefinition => !eventNames.Contains(fieldDefinition.Name));
        }

        /// <summary>
        ///     Method which returns all Types of the given module, including nested ones (recursively)
        /// </summary>
        /// <param name="moduleDefinition"></param>
        /// <returns></returns>
        public static IEnumerable<TypeDefinition> GetAllTypes(ModuleDefinition moduleDefinition) =>
            _GetAllNestedTypes(moduleDefinition.Types); //.Reverse();

        /// <summary>
        ///     Recursive method to get all nested types. Use <see cref="GetAllTypes(ModuleDefinition)" />
        /// </summary>
        /// <param name="typeDefinitions"></param>
        /// <returns></returns>
        private static IEnumerable<TypeDefinition> _GetAllNestedTypes(IEnumerable<TypeDefinition> typeDefinitions)
        {
            if (typeDefinitions?.Count() == 0)
                return new List<TypeDefinition>();

            var result = typeDefinitions.Concat(_GetAllNestedTypes(typeDefinitions.SelectMany(t => t.NestedTypes)));

            return result;
        }
    }
}
