using System;
using System.Reflection;
using System.Text;
using Relay.SourceGenerator.Discovery;
using Relay.SourceGenerator.Generators;
using Xunit;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for BaseCodeGenerator StringBuilder pooling functionality.
/// Ensures efficient memory usage through StringBuilder reuse.
/// </summary>
public class BaseCodeGeneratorStringBuilderPoolingTests
{
    private class TestCodeGenerator : BaseCodeGenerator
    {
        public override string GeneratorName => "Test Generator";
        public override string OutputFileName => "TestOutput";

        protected override void GenerateContent(StringBuilder builder, HandlerDiscoveryResult result, GenerationOptions options)
        {
            builder.AppendLine("// Test content");
        }

        // Expose protected methods for testing
        public StringBuilder TestGetStringBuilder() => GetStringBuilder();
        public void TestReturnStringBuilder(StringBuilder sb) => ReturnStringBuilder(sb);
    }

    [Fact]
    public void GetStringBuilder_FirstCall_ReturnsNewStringBuilder()
    {
        // Arrange
        var generator = new TestCodeGenerator();

        // Act
        var sb = generator.TestGetStringBuilder();

        // Assert
        Assert.NotNull(sb);
        Assert.Equal(0, sb.Length);
        Assert.True(sb.Capacity >= 1024); // Should have at least 1KB capacity
    }

    [Fact]
    public void GetStringBuilder_AfterReturn_ReturnsPooledStringBuilder()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb1 = generator.TestGetStringBuilder();
        sb1.Append("test content");
        generator.TestReturnStringBuilder(sb1);

        // Act
        var sb2 = generator.TestGetStringBuilder();

        // Assert
        Assert.Same(sb1, sb2); // Should be the same instance
        Assert.Equal(0, sb2.Length); // Should be cleared
    }

    [Fact]
    public void ReturnStringBuilder_WithinCapacityLimit_PoolsStringBuilder()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb = generator.TestGetStringBuilder();
        
        // Add content within 16KB limit
        for (int i = 0; i < 100; i++)
        {
            sb.Append("test ");
        }
        
        generator.TestReturnStringBuilder(sb);

        // Act
        var sb2 = generator.TestGetStringBuilder();

        // Assert
        Assert.Same(sb, sb2); // Should be pooled
    }

    [Fact]
    public void ReturnStringBuilder_ExceedsCapacityLimit_DoesNotPool()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb = generator.TestGetStringBuilder();
        
        // Force capacity to exceed 16KB
        sb.EnsureCapacity(17 * 1024);
        sb.Append(new string('x', 17 * 1024));
        
        generator.TestReturnStringBuilder(sb);

        // Act
        var sb2 = generator.TestGetStringBuilder();

        // Assert
        Assert.NotSame(sb, sb2); // Should not be pooled due to size
    }

    [Fact]
    public void GetStringBuilder_ClearsContent_BeforeReturning()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb1 = generator.TestGetStringBuilder();
        sb1.Append("previous content");
        generator.TestReturnStringBuilder(sb1);

        // Act
        var sb2 = generator.TestGetStringBuilder();

        // Assert
        Assert.Equal(0, sb2.Length);
        Assert.Equal(string.Empty, sb2.ToString());
    }

    [Fact]
    public void Generate_UsesStringBuilderPooling()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();

        // Act
        var output1 = generator.Generate(result, options);
        var output2 = generator.Generate(result, options);

        // Assert
        Assert.NotNull(output1);
        Assert.NotNull(output2);
        Assert.Contains("// Test content", output1);
        Assert.Contains("// Test content", output2);
    }

    [Fact]
    public void Generate_ReturnsStringBuilderToPool_EvenOnException()
    {
        // Arrange
        var generator = new ThrowingCodeGenerator();
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => generator.Generate(result, options));
        
        // Verify pool still works after exception
        var sb = generator.TestGetStringBuilder();
        Assert.NotNull(sb);
    }

    [Fact]
    public void StringBuilderPooling_IsThreadLocal()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        StringBuilder? sbFromThread1 = null;
        StringBuilder? sbFromThread2 = null;

        // Act
        var thread1 = new System.Threading.Thread(() =>
        {
            sbFromThread1 = generator.TestGetStringBuilder();
            generator.TestReturnStringBuilder(sbFromThread1);
        });

        var thread2 = new System.Threading.Thread(() =>
        {
            sbFromThread2 = generator.TestGetStringBuilder();
            generator.TestReturnStringBuilder(sbFromThread2);
        });

        thread1.Start();
        thread2.Start();
        thread1.Join();
        thread2.Join();

        // Assert
        Assert.NotNull(sbFromThread1);
        Assert.NotNull(sbFromThread2);
        Assert.NotSame(sbFromThread1, sbFromThread2); // Different threads should have different pools
    }

    [Fact]
    public void GetStringBuilder_MultipleCallsWithoutReturn_ReturnsNewInstances()
    {
        // Arrange
        var generator = new TestCodeGenerator();

        // Act
        var sb1 = generator.TestGetStringBuilder();
        var sb2 = generator.TestGetStringBuilder();
        var sb3 = generator.TestGetStringBuilder();

        // Assert
        Assert.NotSame(sb1, sb2);
        Assert.NotSame(sb2, sb3);
        Assert.NotSame(sb1, sb3);
    }

    [Fact]
    public void ReturnStringBuilder_OnlyPoolsLastReturned()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb1 = generator.TestGetStringBuilder();
        var sb2 = generator.TestGetStringBuilder();
        
        // Act
        generator.TestReturnStringBuilder(sb1);
        generator.TestReturnStringBuilder(sb2); // This should overwrite sb1 in pool
        
        var sb3 = generator.TestGetStringBuilder();

        // Assert
        Assert.Same(sb2, sb3); // Should get sb2, not sb1
    }

    [Fact]
    public void Generate_WithLargeContent_StillWorks()
    {
        // Arrange
        var generator = new LargeContentGenerator();
        var result = new HandlerDiscoveryResult();
        var options = new GenerationOptions();

        // Act
        var output = generator.Generate(result, options);

        // Assert
        Assert.NotNull(output);
        Assert.Contains("Large content", output);
    }

    [Fact]
    public void StringBuilderPooling_DefaultCapacity_Is1KB()
    {
        // Arrange
        var generator = new TestCodeGenerator();

        // Act
        var sb = generator.TestGetStringBuilder();

        // Assert
        Assert.True(sb.Capacity >= 1024, $"Expected capacity >= 1024, but was {sb.Capacity}");
    }

    [Fact]
    public void StringBuilderPooling_MaxCapacity_Is16KB()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb = generator.TestGetStringBuilder();
        
        // Grow to exactly 16KB
        sb.EnsureCapacity(16 * 1024);
        sb.Append(new string('x', 16 * 1024));
        
        // Act
        generator.TestReturnStringBuilder(sb);
        var sb2 = generator.TestGetStringBuilder();

        // Assert
        Assert.Same(sb, sb2); // Should still be pooled at exactly 16KB
    }

    [Fact]
    public void StringBuilderPooling_JustOver16KB_NotPooled()
    {
        // Arrange
        var generator = new TestCodeGenerator();
        var sb = generator.TestGetStringBuilder();
        
        // Grow to just over 16KB
        sb.EnsureCapacity((16 * 1024) + 1);
        sb.Append(new string('x', (16 * 1024) + 1));
        
        // Act
        generator.TestReturnStringBuilder(sb);
        var sb2 = generator.TestGetStringBuilder();

        // Assert
        Assert.NotSame(sb, sb2); // Should not be pooled
    }

    private class ThrowingCodeGenerator : TestCodeGenerator
    {
        protected override void GenerateContent(StringBuilder builder, HandlerDiscoveryResult result, GenerationOptions options)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private class LargeContentGenerator : TestCodeGenerator
    {
        protected override void GenerateContent(StringBuilder builder, HandlerDiscoveryResult result, GenerationOptions options)
        {
            // Generate content larger than 16KB
            for (int i = 0; i < 2000; i++)
            {
                builder.AppendLine($"// Large content line {i}");
            }
        }
    }
}
