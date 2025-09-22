using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Generates JSON schema definitions for types at compile time.
    /// </summary>
    public static class JsonSchemaGenerator
    {
        /// <summary>
        /// Generates a JSON schema for the specified type symbol.
        /// </summary>
        /// <param name="typeSymbol">The type symbol to generate schema for.</param>
        /// <param name="compilation">The compilation context.</param>
        /// <returns>The JSON schema as a string.</returns>
        public static string GenerateSchema(ITypeSymbol typeSymbol, Compilation compilation)
        {
            var schema = new Dictionary<string, object>
            {
                ["$schema"] = "http://json-schema.org/draft-07/schema#",
                ["type"] = "object"
            };

            if (typeSymbol is INamedTypeSymbol namedType)
            {
                schema["title"] = namedType.Name;
                
                var properties = new Dictionary<string, object>();
                var required = new List<string>();

                // Get all public properties and fields
                var members = namedType.GetMembers()
                    .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                    .Where(m => m is IPropertySymbol || m is IFieldSymbol)
                    .ToList();

                foreach (var member in members)
                {
                    var memberName = ToCamelCase(member.Name);
                    var memberType = GetMemberType(member);
                    
                    if (memberType != null)
                    {
                        var propertySchema = GeneratePropertySchema(memberType, compilation);
                        properties[memberName] = propertySchema;

                        // Check if the member is required (non-nullable reference type or value type)
                        if (IsRequiredMember(member, memberType))
                        {
                            required.Add(memberName);
                        }
                    }
                }

                if (properties.Count > 0)
                {
                    schema["properties"] = properties;
                }

                if (required.Count > 0)
                {
                    schema["required"] = required;
                }
            }

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        private static Dictionary<string, object> GeneratePropertySchema(ITypeSymbol typeSymbol, Compilation compilation)
        {
            var schema = new Dictionary<string, object>();

            // Handle nullable types
            var underlyingType = typeSymbol;
            if (typeSymbol.CanBeReferencedByName && typeSymbol.Name == "Nullable" && typeSymbol is INamedTypeSymbol nullableType)
            {
                underlyingType = nullableType.TypeArguments.FirstOrDefault() ?? typeSymbol;
            }

            // Map .NET types to JSON schema types
            switch (underlyingType.SpecialType)
            {
                case SpecialType.System_String:
                    schema["type"] = "string";
                    break;
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Int16:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    schema["type"] = "integer";
                    break;
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                case SpecialType.System_Decimal:
                    schema["type"] = "number";
                    break;
                case SpecialType.System_Boolean:
                    schema["type"] = "boolean";
                    break;
                case SpecialType.System_DateTime:
                    schema["type"] = "string";
                    schema["format"] = "date-time";
                    break;
                default:
                    // Handle arrays and collections
                    if (IsArrayOrCollection(underlyingType))
                    {
                        schema["type"] = "array";
                        var elementType = GetCollectionElementType(underlyingType);
                        if (elementType != null)
                        {
                            schema["items"] = GeneratePropertySchema(elementType, compilation);
                        }
                    }
                    // Handle enums
                    else if (underlyingType.TypeKind == TypeKind.Enum)
                    {
                        schema["type"] = "string";
                        var enumValues = underlyingType.GetMembers()
                            .OfType<IFieldSymbol>()
                            .Where(f => f.IsStatic && f.HasConstantValue)
                            .Select(f => f.Name)
                            .ToArray();
                        if (enumValues.Length > 0)
                        {
                            schema["enum"] = enumValues;
                        }
                    }
                    // Handle complex objects
                    else if (underlyingType.TypeKind == TypeKind.Class || underlyingType.TypeKind == TypeKind.Struct)
                    {
                        schema["type"] = "object";
                        schema["$ref"] = $"#/definitions/{underlyingType.Name}";
                    }
                    else
                    {
                        schema["type"] = "object";
                    }
                    break;
            }

            return schema;
        }

        private static ITypeSymbol? GetMemberType(ISymbol member)
        {
            return member switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                _ => null
            };
        }

        private static bool IsRequiredMember(ISymbol member, ITypeSymbol memberType)
        {
            // Value types are always required unless nullable
            if (memberType.IsValueType)
            {
                return !IsNullableValueType(memberType);
            }

            // Reference types are required if they're not nullable
            return memberType.NullableAnnotation != NullableAnnotation.Annotated;
        }

        private static bool IsNullableValueType(ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol namedType && 
                   namedType.IsGenericType && 
                   namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
        }

        private static bool IsArrayOrCollection(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.Array)
                return true;

            if (typeSymbol is INamedTypeSymbol namedType)
            {
                // Check for common collection interfaces
                var collectionInterfaces = new[]
                {
                    "System.Collections.Generic.IEnumerable",
                    "System.Collections.Generic.ICollection",
                    "System.Collections.Generic.IList"
                };

                return namedType.AllInterfaces.Any(i => 
                    collectionInterfaces.Contains(i.ConstructedFrom?.ToDisplayString()));
            }

            return false;
        }

        private static ITypeSymbol? GetCollectionElementType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType;
            }

            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // For generic collections, return the first type argument
                return namedType.TypeArguments.FirstOrDefault();
            }

            return null;
        }

        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
                return input;

            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }
    }
}