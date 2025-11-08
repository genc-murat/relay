using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class JsonSchemaGeneratorTests
    {
        [Fact]
        public void GenerateSchema_WithSimpleClass_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class SimpleClass
                    {
                        public string Name { get; set; } = string.Empty;
                        public int Age { get; set; }
                        public bool IsActive { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("SimpleClass").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"$schema\": \"http://json-schema.org/draft-07/schema#\"", result);
            Assert.Contains("\"title\": \"SimpleClass\"", result);
            Assert.Contains("\"type\": \"object\"", result);
            Assert.Contains("\"name\"", result);
            Assert.Contains("\"age\"", result);
            Assert.Contains("\"isActive\"", result); // Fixed camelCase conversion
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"type\": \"integer\"", result);
            Assert.Contains("\"type\": \"boolean\"", result);
        }

        [Fact]
        public void GenerateSchema_WithNullableProperties_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                using System;

                namespace TestNamespace
                {
                    public class ClassWithNullable
                    {
                        public string? Name { get; set; }
                        public int? Age { get; set; }
                        public DateTime? BirthDate { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithNullable").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"name\"", result);
            Assert.Contains("\"age\"", result);
            Assert.Contains("\"birthDate\"", result);
            // Nullable properties should still be included in properties
        }

        [Fact]
        public void GenerateSchema_WithEnumProperty_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public enum Status
                    {
                        Active,
                        Inactive,
                        Pending
                    }
                    
                    public class ClassWithEnum
                    {
                        public Status Status { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithEnum").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"enum\"", result);
            Assert.Contains("\"Active\"", result);
            Assert.Contains("\"Inactive\"", result);
            Assert.Contains("\"Pending\"", result);
        }

        [Fact]
        public void GenerateSchema_WithArrayProperty_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class ClassWithArray
                    {
                        public string[] Tags { get; set; } = new string[0];
                        public int[] Numbers { get; set; } = new int[0];
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithArray").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"tags\"", result);
            Assert.Contains("\"numbers\"", result);
            Assert.Contains("\"type\": \"array\"", result);
            Assert.Contains("\"items\"", result);
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"type\": \"integer\"", result);
        }

        [Fact]
        public void GenerateSchema_WithListProperty_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                using System.Collections.Generic;

                namespace TestNamespace
                {
                    public class ClassWithList
                    {
                        public List<string> Items { get; set; } = new List<string>();
                        public List<int> Numbers { get; set; } = new List<int>();
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithList").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"items\"", result);
            Assert.Contains("\"numbers\"", result);
            Assert.Contains("\"type\": \"array\"", result);
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"type\": \"integer\"", result);
        }

        [Fact]
        public void GenerateSchema_WithComplexObjectProperty_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class Address
                    {
                        public string Street { get; set; } = string.Empty;
                        public string City { get; set; } = string.Empty;
                    }
                    
                    public class Person
                    {
                        public string Name { get; set; } = string.Empty;
                        public Address Address { get; set; } = new Address();
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("Person").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"name\"", result);
            Assert.Contains("\"address\"", result);
            Assert.Contains("\"$ref\"", result);
        }

        [Fact]
        public void GenerateSchema_WithDateTimeProperty_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                using System;

                namespace TestNamespace
                {
                    public class ClassWithDateTime
                    {
                        public DateTime CreatedAt { get; set; }
                        public DateTime? UpdatedAt { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithDateTime").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"createdAt\"", result);
            Assert.Contains("\"updatedAt\"", result);
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"format\": \"date-time\"", result);
        }

        [Fact]
        public void GenerateSchema_WithNumericTypes_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class ClassWithNumbers
                    {
                        public int IntValue { get; set; }
                        public long LongValue { get; set; }
                        public short ShortValue { get; set; }
                        public byte ByteValue { get; set; }
                        public sbyte SByteValue { get; set; }
                        public double DoubleValue { get; set; }
                        public float FloatValue { get; set; }
                        public decimal DecimalValue { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithNumbers").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"intValue\"", result);
            Assert.Contains("\"longValue\"", result);
            Assert.Contains("\"shortValue\"", result);
            Assert.Contains("\"byteValue\"", result);
            Assert.Contains("\"sByteValue\"", result); // Fixed to match actual camelCase conversion (SByte -> sByte)
            Assert.Contains("\"doubleValue\"", result);
            Assert.Contains("\"floatValue\"", result);
            Assert.Contains("\"decimalValue\"", result);
            Assert.Contains("\"type\": \"integer\"", result);  // For int, long, short, byte, sbyte
            Assert.Contains("\"type\": \"number\"", result);   // For double, float, decimal
        }

        [Fact]
        public void GenerateSchema_WithRequiredProperties_GeneratesCorrectRequiredArray()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class ClassWithRequired
                    {
                        public string Name { get; set; } = string.Empty;  // Required
                        public int Age { get; set; }  // Required (value type)
                        public bool IsActive { get; set; } // Required (value type)
                        public string? OptionalValue { get; set; } // Optional
                        public int? OptionalNumber { get; set; } // Optional
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithRequired").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert - Check that required properties are in the required array
            Assert.Contains("\"required\"", result);
            Assert.Contains("\"name\"", result);
            Assert.Contains("\"age\"", result);
            Assert.Contains("\"isActive\"", result);
        }

        [Fact]
        public void GenerateSchema_WithFieldMembers_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class ClassWithFields
                    {
                        public string PublicField = string.Empty;
                        public int IntField;
                        private string PrivateField = string.Empty; // Should be ignored
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithFields").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert - Fields should be included if they're public
            Assert.Contains("\"publicField\"", result);
            Assert.Contains("\"intField\"", result);
            // Private field should not be included
        }

        [Fact]
        public void GenerateSchema_WithEmptyClass_GeneratesMinimalSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class EmptyClass
                    {
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("EmptyClass").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert - Should have minimal schema without properties
            Assert.Contains("\"$schema\": \"http://json-schema.org/draft-07/schema#\"", result);
            Assert.Contains("\"type\": \"object\"", result);
            Assert.DoesNotContain("\"properties\"", result);
            Assert.DoesNotContain("\"required\"", result);
        }

        [Fact]
        public void GenerateSchema_WithInheritedClass_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class BaseClass
                    {
                        public string BaseProperty { get; set; } = string.Empty;
                    }
                    
                    public class DerivedClass : BaseClass
                    {
                        public string DerivedProperty { get; set; } = string.Empty;
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("DerivedClass").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert - Should include properties from both base and derived class
            Assert.Contains("\"baseProperty\"", result);
            Assert.Contains("\"derivedProperty\"", result);
        }

        [Fact]
        public void GenerateSchema_WithStruct_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public struct PersonStruct
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("PersonStruct").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"$schema\": \"http://json-schema.org/draft-07/schema#\"", result);
            Assert.Contains("\"title\": \"PersonStruct\"", result);
            Assert.Contains("\"type\": \"object\"", result);
            Assert.Contains("\"name\"", result);
            Assert.Contains("\"age\"", result);
        }

        [Fact]
        public void GenerateSchema_WithNestedCollections_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                using System.Collections.Generic;
                
                namespace TestNamespace
                {
                    public class ClassWithNestedCollections
                    {
                        public List<List<string>> NestedLists { get; set; } = new List<List<string>>();
                        public int[][] NestedArrays { get; set; } = new int[0][];
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithNestedCollections").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"nestedLists\"", result);
            Assert.Contains("\"nestedArrays\"", result);
            Assert.Contains("\"type\": \"array\"", result);
            Assert.Contains("\"items\"", result);
        }

        [Fact]
        public void GenerateSchema_WithEnumWithFlags_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                using System;
                
                namespace TestNamespace
                {
                    [Flags]
                    public enum Permissions
                    {
                        None = 0,
                        Read = 1,
                        Write = 2,
                        Execute = 4,
                        All = Read | Write | Execute
                    }
                    
                    public class ClassWithFlagsEnum
                    {
                        public Permissions UserPermissions { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithFlagsEnum").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"userPermissions\"", result);
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"enum\"", result);
            Assert.Contains("\"None\"", result);
            Assert.Contains("\"Read\"", result);
            Assert.Contains("\"Write\"", result);
            Assert.Contains("\"Execute\"", result);
            Assert.Contains("\"All\"", result);
        }

        [Fact]
        public void GenerateSchema_WithGenericClass_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                using System.Collections.Generic;
                
                namespace TestNamespace
                {
                    public class GenericContainer<T>
                    {
                        public T Value { get; set; }
                        public List<T> Items { get; set; } = new List<T>();
                    }
                    
                    public class StringContainer : GenericContainer<string>
                    {
                        public string AdditionalProperty { get; set; } = string.Empty;
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("StringContainer").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"value\"", result);
            Assert.Contains("\"items\"", result);
            Assert.Contains("\"additionalProperty\"", result);
            Assert.Contains("\"type\": \"array\"", result);
        }

        [Fact]
        public void GenerateSchema_WithReadOnlyProperties_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public class ClassWithReadOnlyProperties
                    {
                        public string ReadOnlyProperty { get; } = string.Empty;
                        public int ComputedProperty => 42;
                        public string WritableProperty { get; set; } = string.Empty;
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithReadOnlyProperties").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert - All public properties should be included regardless of writability
            Assert.Contains("\"readOnlyProperty\"", result);
            Assert.Contains("\"computedProperty\"", result);
            Assert.Contains("\"writableProperty\"", result);
        }

        [Fact]
        public void GenerateSchema_WithNullableEnum_GeneratesCorrectSchema()
        {
            // Arrange
            var sourceCode = @"
                namespace TestNamespace
                {
                    public enum Status
                    {
                        Active,
                        Inactive
                    }

                    public class ClassWithNullableEnum
                    {
                        public Status? Status { get; set; }
                    }
                }";

            var compilation = CreateCompilation(sourceCode);
            var typeSymbol = compilation.GetSymbolsWithName("ClassWithNullableEnum").FirstOrDefault() as INamedTypeSymbol;
            Assert.NotNull(typeSymbol); // Ensure we found the symbol

            // Act
            var result = JsonSchemaGenerator.GenerateSchema(typeSymbol!, compilation);

            // Assert
            Assert.Contains("\"status\"", result);
            Assert.Contains("\"type\": \"string\"", result);
            Assert.Contains("\"enum\"", result);
        }

        [Fact]
        public void GetMemberType_WithUnsupportedMemberType_ReturnsNull()
        {
            // Arrange - Create a mock symbol that's neither property nor field
            // This tests the default case in the switch expression
            var mockSymbol = new Mock<ISymbol>();
            mockSymbol.Setup(s => s.Kind).Returns(SymbolKind.Method); // Not a property or field

            // Act - Use reflection to access the private method
            var method = typeof(Relay.SourceGenerator.JsonSchemaGenerator).GetMethod("GetMemberType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = method?.Invoke(null, new object[] { mockSymbol.Object });

            // Assert
            Assert.Null(result);
        }



        [Fact]
        public void GetCollectionElementType_WithNonGenericType_ReturnsNull()
        {
            // Arrange - Test with a non-generic type
            var compilation = CreateCompilation("namespace Test { public class TestClass { } }");
            var stringType = compilation.GetSpecialType(SpecialType.System_String);

            // Act - Use reflection to access the private method
            var method = typeof(Relay.SourceGenerator.JsonSchemaGenerator).GetMethod("GetCollectionElementType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = method?.Invoke(null, new object[] { stringType });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ToCamelCase_WithAlreadyLowercaseInput_ReturnsUnchanged()
        {
            // Arrange
            var input = "alreadylowercase";

            // Act - Use reflection to access the private method
            var method = typeof(Relay.SourceGenerator.JsonSchemaGenerator).GetMethod("ToCamelCase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = method?.Invoke(null, new object[] { input });

            // Assert
            Assert.Equal("alreadylowercase", result);
        }

        private Compilation CreateCompilation(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Array).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DateTime).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}