using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Relay.SourceGenerator;
using System.Linq;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    public class SyntaxReceiverTests
    {
        [Fact]
        public void OnVisitSyntaxNode_Should_Collect_Handle_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request) => request;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(1);
            receiver.CandidateMethods.First().Identifier.ValueText.Should().Be("HandleTest");
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Collect_HandleAttribute_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [HandleAttribute]
        public string HandleTest(string request) => request;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(1);
            receiver.CandidateMethods.First().Identifier.ValueText.Should().Be("HandleTest");
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Collect_Notification_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Notification]
        public void HandleNotification(string notification) { }
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(1);
            receiver.CandidateMethods.First().Identifier.ValueText.Should().Be("HandleNotification");
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Collect_Pipeline_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Pipeline]
        public async Task<T> PipelineMethod<T>(T request, Func<Task<T>> next) => await next();
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(1);
            receiver.CandidateMethods.First().Identifier.ValueText.Should().Be("PipelineMethod");
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Collect_ExposeAsEndpoint_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [ExposeAsEndpoint]
        public string HandleEndpoint(string request) => request;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(1);
            receiver.CandidateMethods.First().Identifier.ValueText.Should().Be("HandleEndpoint");
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Collect_Multiple_Attributed_Methods()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        public string HandleTest(string request) => request;

        [Notification]
        public void HandleNotification(string notification) { }

        [Pipeline]
        public async Task<T> PipelineMethod<T>(T request, Func<Task<T>> next) => await next();

        [ExposeAsEndpoint]
        public string HandleEndpoint(string request) => request;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(4);
            receiver.CandidateMethods.Should().Contain(m => m.Identifier.ValueText == "HandleTest");
            receiver.CandidateMethods.Should().Contain(m => m.Identifier.ValueText == "HandleNotification");
            receiver.CandidateMethods.Should().Contain(m => m.Identifier.ValueText == "PipelineMethod");
            receiver.CandidateMethods.Should().Contain(m => m.Identifier.ValueText == "HandleEndpoint");
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Ignore_Methods_Without_Relay_Attributes()
        {
            // Arrange
            var source = @"
using System;

namespace TestProject
{
    public class TestHandler
    {
        public string RegularMethod(string input) => input;

        [Obsolete]
        public string ObsoleteMethod(string input) => input;

        [System.ComponentModel.Description(""Test"")]
        public string DescribedMethod(string input) => input;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().BeEmpty();
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Ignore_Non_Method_Nodes()
        {
            // Arrange
            var source = @"
using Relay.Core;

namespace TestProject
{
    [Handle] // This should be ignored - attribute on class
    public class TestHandler
    {
        [Handle] // This should be ignored - attribute on property
        public string TestProperty { get; set; }

        [Handle] // This should be ignored - attribute on field
        public string TestField;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().BeEmpty();
        }

        [Fact]
        public void OnVisitSyntaxNode_Should_Handle_Methods_With_Multiple_Attributes()
        {
            // Arrange
            var source = @"
using Relay.Core;
using System;

namespace TestProject
{
    public class TestHandler
    {
        [Handle]
        [Obsolete]
        public string HandleTest(string request) => request;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            receiver.CandidateMethods.Should().HaveCount(1);
            receiver.CandidateMethods.First().Identifier.ValueText.Should().Be("HandleTest");
        }

        [Fact]
        public void CandidateMethods_Should_Be_Empty_Initially()
        {
            // Arrange & Act
            var receiver = new RelaySyntaxReceiver();

            // Assert
            receiver.CandidateMethods.Should().BeEmpty();
        }

        private static void VisitAllNodes(string source, RelaySyntaxReceiver receiver)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            foreach (var node in syntaxTree.GetRoot().DescendantNodes())
            {
                receiver.OnVisitSyntaxNode(node);
            }
        }
    }
}