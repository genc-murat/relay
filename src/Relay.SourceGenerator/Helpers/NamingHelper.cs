using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Helpers;

/// <summary>
/// Helper class for consistent naming conventions across the source generator.
/// Ensures all generated code follows C# naming guidelines.
/// </summary>
public static class NamingHelper
{
    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The PascalCase string</returns>
    public static string ToPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        var words = SplitIntoWords(value);
        var builder = new StringBuilder();

        foreach (var word in words)
        {
            if (word.Length > 0)
            {
                builder.Append(char.ToUpperInvariant(word[0]));
                if (word.Length > 1)
                {
                    // Preserve existing casing for acronyms (e.g., "DI" stays "DI")
                    if (word.Length == 2 && char.IsUpper(word[1]))
                    {
                        builder.Append(word.Substring(1));
                    }
                    else
                    {
                        builder.Append(word.Substring(1).ToLowerInvariant());
                    }
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The camelCase string</returns>
    public static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        var pascalCase = ToPascalCase(value);
        if (pascalCase.Length == 0) return pascalCase;

        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }

    /// <summary>
    /// Generates a private field name from a property or parameter name.
    /// </summary>
    /// <param name="name">The property or parameter name</param>
    /// <returns>The private field name with underscore prefix</returns>
    public static string ToPrivateFieldName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;

        var camelCase = ToCamelCase(name);
        return $"_{camelCase}";
    }

    /// <summary>
    /// Generates a parameter name from a type name.
    /// </summary>
    /// <param name="typeName">The type name</param>
    /// <returns>The parameter name in camelCase</returns>
    public static string ToParameterName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return typeName;

        // Remove generic type parameters
        var baseName = typeName.Split('<')[0];

        // Remove interface prefix if present
        if (baseName.StartsWith("I") && baseName.Length > 1 && char.IsUpper(baseName[1]))
        {
            baseName = baseName.Substring(1);
        }

        return ToCamelCase(baseName);
    }

    /// <summary>
    /// Generates a generator class name.
    /// </summary>
    /// <param name="purpose">The purpose of the generator</param>
    /// <returns>The generator class name</returns>
    public static string ToGeneratorName(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose)) 
            throw new ArgumentNullException(nameof(purpose));

        var pascalCase = ToPascalCase(purpose);
        return pascalCase.EndsWith("Generator") ? pascalCase : $"{pascalCase}Generator";
    }

    /// <summary>
    /// Generates a validator class name.
    /// </summary>
    /// <param name="target">The target being validated</param>
    /// <returns>The validator class name</returns>
    public static string ToValidatorName(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) 
            throw new ArgumentNullException(nameof(target));

        var pascalCase = ToPascalCase(target);
        return pascalCase.EndsWith("Validator") ? pascalCase : $"{pascalCase}Validator";
    }

    /// <summary>
    /// Generates a helper class name.
    /// </summary>
    /// <param name="purpose">The purpose of the helper</param>
    /// <returns>The helper class name</returns>
    public static string ToHelperName(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose)) 
            throw new ArgumentNullException(nameof(purpose));

        var pascalCase = ToPascalCase(purpose);
        return pascalCase.EndsWith("Helper") ? pascalCase : $"{pascalCase}Helper";
    }

    /// <summary>
    /// Generates an extension class name.
    /// </summary>
    /// <param name="typeName">The type being extended</param>
    /// <returns>The extension class name</returns>
    public static string ToExtensionClassName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) 
            throw new ArgumentNullException(nameof(typeName));

        var pascalCase = ToPascalCase(typeName);
        return pascalCase.EndsWith("Extensions") ? pascalCase : $"{pascalCase}Extensions";
    }

    /// <summary>
    /// Generates a service class name.
    /// </summary>
    /// <param name="purpose">The purpose of the service</param>
    /// <returns>The service class name</returns>
    public static string ToServiceName(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose)) 
            throw new ArgumentNullException(nameof(purpose));

        var pascalCase = ToPascalCase(purpose);
        return pascalCase.EndsWith("Service") ? pascalCase : $"{pascalCase}Service";
    }

    /// <summary>
    /// Generates a context class name.
    /// </summary>
    /// <param name="purpose">The purpose of the context</param>
    /// <returns>The context class name</returns>
    public static string ToContextName(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose)) 
            throw new ArgumentNullException(nameof(purpose));

        var pascalCase = ToPascalCase(purpose);
        return pascalCase.EndsWith("Context") ? pascalCase : $"{pascalCase}Context";
    }

    /// <summary>
    /// Generates a result class name.
    /// </summary>
    /// <param name="operation">The operation that produces the result</param>
    /// <returns>The result class name</returns>
    public static string ToResultName(string operation)
    {
        if (string.IsNullOrWhiteSpace(operation)) 
            throw new ArgumentNullException(nameof(operation));

        var pascalCase = ToPascalCase(operation);
        return pascalCase.EndsWith("Result") ? pascalCase : $"{pascalCase}Result";
    }

    /// <summary>
    /// Generates an info/data class name.
    /// </summary>
    /// <param name="entity">The entity the info/data represents</param>
    /// <returns>The info class name</returns>
    public static string ToInfoName(string entity)
    {
        if (string.IsNullOrWhiteSpace(entity)) 
            throw new ArgumentNullException(nameof(entity));

        var pascalCase = ToPascalCase(entity);
        return pascalCase.EndsWith("Info") ? pascalCase : $"{pascalCase}Info";
    }

    /// <summary>
    /// Generates an interface name.
    /// </summary>
    /// <param name="name">The base name</param>
    /// <returns>The interface name with 'I' prefix</returns>
    public static string ToInterfaceName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) 
            throw new ArgumentNullException(nameof(name));

        // If already starts with I and next char is uppercase, assume it's already an interface name
        if (name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1]))
        {
            return name;
        }

        var pascalCase = ToPascalCase(name);
        return pascalCase.StartsWith("I") ? pascalCase : $"I{pascalCase}";
    }

    /// <summary>
    /// Generates a test class name.
    /// </summary>
    /// <param name="typeUnderTest">The type being tested</param>
    /// <returns>The test class name</returns>
    public static string ToTestClassName(string typeUnderTest)
    {
        if (string.IsNullOrWhiteSpace(typeUnderTest)) 
            throw new ArgumentNullException(nameof(typeUnderTest));

        var pascalCase = ToPascalCase(typeUnderTest);
        return pascalCase.EndsWith("Tests") ? pascalCase : $"{pascalCase}Tests";
    }

    /// <summary>
    /// Generates a test method name.
    /// </summary>
    /// <param name="methodUnderTest">The method being tested</param>
    /// <param name="scenario">The test scenario</param>
    /// <param name="expectedResult">The expected result</param>
    /// <returns>The test method name</returns>
    public static string ToTestMethodName(string methodUnderTest, string scenario, string expectedResult)
    {
        if (string.IsNullOrWhiteSpace(methodUnderTest)) 
            throw new ArgumentNullException(nameof(methodUnderTest));
        if (string.IsNullOrWhiteSpace(scenario)) 
            throw new ArgumentNullException(nameof(scenario));
        if (string.IsNullOrWhiteSpace(expectedResult)) 
            throw new ArgumentNullException(nameof(expectedResult));

        return $"{ToPascalCase(methodUnderTest)}_{ToPascalCase(scenario)}_{ToPascalCase(expectedResult)}";
    }

    /// <summary>
    /// Generates a diagnostic ID.
    /// </summary>
    /// <param name="number">The diagnostic number</param>
    /// <returns>The diagnostic ID</returns>
    public static string ToDiagnosticId(int number)
    {
        if (number < 0) throw new ArgumentOutOfRangeException(nameof(number));

        return $"RELAY_GEN_{number:D3}";
    }

    /// <summary>
    /// Generates a generated class name.
    /// </summary>
    /// <param name="purpose">The purpose of the generated class</param>
    /// <returns>The generated class name</returns>
    public static string ToGeneratedClassName(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose)) 
            throw new ArgumentNullException(nameof(purpose));

        var pascalCase = ToPascalCase(purpose);
        return pascalCase.StartsWith("Generated") ? pascalCase : $"Generated{pascalCase}";
    }

    /// <summary>
    /// Gets a safe identifier name from a symbol.
    /// </summary>
    /// <param name="symbol">The symbol</param>
    /// <returns>A safe identifier name</returns>
    public static string GetSafeIdentifier(ISymbol symbol)
    {
        if (symbol == null) throw new ArgumentNullException(nameof(symbol));

        var name = symbol.Name;
        
        // Handle special characters
        name = name.Replace("<", "_").Replace(">", "_").Replace(",", "_");
        
        // Ensure it starts with a letter or underscore
        if (!char.IsLetter(name[0]) && name[0] != '_')
        {
            name = "_" + name;
        }

        return name;
    }

    /// <summary>
    /// Splits a string into words based on various delimiters and casing.
    /// </summary>
    private static IEnumerable<string> SplitIntoWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;

        var currentWord = new StringBuilder();
        var previousWasUpper = false;

        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsWhiteSpace(c) || c == '_' || c == '-')
            {
                if (currentWord.Length > 0)
                {
                    yield return currentWord.ToString();
                    currentWord.Clear();
                }
                previousWasUpper = false;
            }
            else if (char.IsUpper(c))
            {
                // Check if next char is lowercase (e.g., "DIRegistration" -> "DI" + "Registration")
                bool nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
                
                if (currentWord.Length > 0 && !previousWasUpper)
                {
                    // Start of new word after lowercase letters
                    yield return currentWord.ToString();
                    currentWord.Clear();
                }
                else if (currentWord.Length > 0 && previousWasUpper && nextIsLower)
                {
                    // End of acronym (e.g., "DI" before "Registration")
                    yield return currentWord.ToString();
                    currentWord.Clear();
                }
                
                currentWord.Append(c);
                previousWasUpper = true;
            }
            else
            {
                currentWord.Append(c);
                previousWasUpper = false;
            }
        }

        if (currentWord.Length > 0)
        {
            yield return currentWord.ToString();
        }
    }
}
