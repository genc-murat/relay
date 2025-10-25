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
            Assert.Single(receiver.CandidateMethods);
            Assert.Equal("HandleTest", receiver.CandidateMethods.First().Identifier.ValueText);
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
            Assert.Single(receiver.CandidateMethods);
            Assert.Equal("HandleTest", receiver.CandidateMethods.First().Identifier.ValueText);
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
            Assert.Single(receiver.CandidateMethods);
            Assert.Equal("HandleNotification", receiver.CandidateMethods.First().Identifier.ValueText);
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
            Assert.Single(receiver.CandidateMethods);
            Assert.Equal("PipelineMethod", receiver.CandidateMethods.First().Identifier.ValueText);
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
            Assert.Single(receiver.CandidateMethods);
            Assert.Equal("HandleEndpoint", receiver.CandidateMethods.First().Identifier.ValueText);
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
            Assert.Equal(4, receiver.CandidateMethods.Count());
            Assert.Contains(receiver.CandidateMethods, m => m.Identifier.ValueText == "HandleTest");
            Assert.Contains(receiver.CandidateMethods, m => m.Identifier.ValueText == "HandleNotification");
            Assert.Contains(receiver.CandidateMethods, m => m.Identifier.ValueText == "PipelineMethod");
            Assert.Contains(receiver.CandidateMethods, m => m.Identifier.ValueText == "HandleEndpoint");
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
            Assert.Empty(receiver.CandidateMethods);
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
            Assert.Empty(receiver.CandidateMethods);
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
            Assert.Single(receiver.CandidateMethods);
            Assert.Equal("HandleTest", receiver.CandidateMethods.First().Identifier.ValueText);
        }

        [Fact]
        public void CandidateMethods_Should_Be_Empty_Initially()
        {
            // Arrange & Act
            var receiver = new RelaySyntaxReceiver();

            // Assert
            Assert.Empty(receiver.CandidateMethods);
        }

        [Fact]
        public void CandidateCount_Should_Return_Correct_Count()
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

        public string RegularMethod(string input) => input;
    }
}";
            var receiver = new RelaySyntaxReceiver();

            // Act
            VisitAllNodes(source, receiver);

            // Assert
            Assert.Equal(2, receiver.CandidateCount);
        }

        [Fact]
        public void Clear_Should_Remove_All_Candidate_Methods()
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
    }
}";
            var receiver = new RelaySyntaxReceiver();
            VisitAllNodes(source, receiver);

            // Verify methods were collected
            Assert.Equal(2, receiver.CandidateCount);

            // Act
            receiver.Clear();

            // Assert
            Assert.Equal(0, receiver.CandidateCount);
            Assert.Empty(receiver.CandidateMethods);
        }

        [Fact]
        public void GetCandidatesByAttribute_Should_Filter_By_Handle_Attribute()
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

        [Handle]
        public string HandleAnother(string request) => request;

        public string RegularMethod(string input) => input;
    }
}";
            var receiver = new RelaySyntaxReceiver();
            VisitAllNodes(source, receiver);

            // Act
            var handleMethods = receiver.GetCandidatesByAttribute("Handle").ToList();

            // Assert
            Assert.Equal(2, handleMethods.Count);
            Assert.Contains(handleMethods, m => m.Identifier.ValueText == "HandleTest");
            Assert.Contains(handleMethods, m => m.Identifier.ValueText == "HandleAnother");
        }

        [Fact]
        public void GetCandidatesByAttribute_Should_Filter_By_Notification_Attribute()
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

        [Notification]
        public void HandleAnotherNotification(string notification) { }
    }
}";
            var receiver = new RelaySyntaxReceiver();
            VisitAllNodes(source, receiver);

            // Act
            var notificationMethods = receiver.GetCandidatesByAttribute("Notification").ToList();

            // Assert
            Assert.Equal(2, notificationMethods.Count);
            Assert.Contains(notificationMethods, m => m.Identifier.ValueText == "HandleNotification");
            Assert.Contains(notificationMethods, m => m.Identifier.ValueText == "HandleAnotherNotification");
        }

        [Fact]
        public void GetCandidatesByAttribute_Should_Return_Empty_For_NonExistent_Attribute()
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
    }
}";
            var receiver = new RelaySyntaxReceiver();
            VisitAllNodes(source, receiver);

            // Act
            var pipelineMethods = receiver.GetCandidatesByAttribute("Pipeline").ToList();

            // Assert
            Assert.Empty(pipelineMethods);
        }

    [Fact]
    public void GetCandidatesByAttribute_Should_Be_Case_Insensitive()
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

        [Handle] // Both use proper case since IsRelayAttributeName only recognizes proper case
        public string HandleLowercase(string request) => request;
    }
}";
            var receiver = new RelaySyntaxReceiver();
            VisitAllNodes(source, receiver);

            // Act
            var handleMethods = receiver.GetCandidatesByAttribute("handle").ToList(); // lowercase search

            // Assert
            Assert.Equal(2, handleMethods.Count);
            Assert.Contains(handleMethods, m => m.Identifier.ValueText == "HandleTest");
            Assert.Contains(handleMethods, m => m.Identifier.ValueText == "HandleLowercase");
        }

        [Fact]
        public void GetCandidatesByAttribute_Should_Work_With_Attribute_Suffix()
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

        [NotificationAttribute]
        public void HandleNotification(string notification) { }
    }
}";
            var receiver = new RelaySyntaxReceiver();
            VisitAllNodes(source, receiver);

            // Act
            var handleMethods = receiver.GetCandidatesByAttribute("Handle").ToList();
            var notificationMethods = receiver.GetCandidatesByAttribute("Notification").ToList();

            // Assert
            Assert.Single(handleMethods);
            Assert.Equal("HandleTest", handleMethods.First().Identifier.ValueText);

            Assert.Single(notificationMethods);
            Assert.Equal("HandleNotification", notificationMethods.First().Identifier.ValueText);
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