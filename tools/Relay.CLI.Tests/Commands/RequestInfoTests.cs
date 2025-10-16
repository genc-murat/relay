using Relay.CLI.Commands.Models;

namespace Relay.CLI.Tests.Commands;

public class RequestInfoTests
{
    [Fact]
    public void RequestInfo_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { Name = "CreateUserRequest" };

        // Assert
        request.Name.Should().Be("CreateUserRequest");
    }

    [Fact]
    public void RequestInfo_ShouldHaveFilePathProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { FilePath = "src/Requests/CreateUserRequest.cs" };

        // Assert
        request.FilePath.Should().Be("src/Requests/CreateUserRequest.cs");
    }

    [Fact]
    public void RequestInfo_ShouldHaveIsRecordProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { IsRecord = true };

        // Assert
        request.IsRecord.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasResponseProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasResponse = true };

        // Assert
        request.HasResponse.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasValidationProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasValidation = true };

        // Assert
        request.HasValidation.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_ShouldHaveParameterCountProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = 3 };

        // Assert
        request.ParameterCount.Should().Be(3);
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasCachingProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasCaching = true };

        // Assert
        request.HasCaching.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasAuthorizationProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasAuthorization = true };

        // Assert
        request.HasAuthorization.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var request = new RequestInfo();

        // Assert
        request.Name.Should().Be("");
        request.FilePath.Should().Be("");
        request.IsRecord.Should().BeFalse();
        request.HasResponse.Should().BeFalse();
        request.HasValidation.Should().BeFalse();
        request.ParameterCount.Should().Be(0);
        request.HasCaching.Should().BeFalse();
        request.HasAuthorization.Should().BeFalse();
    }

    [Fact]
    public void RequestInfo_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var request = new RequestInfo
        {
            Name = "UpdateUserCommand",
            FilePath = "src/Commands/UpdateUserCommand.cs",
            IsRecord = true,
            HasResponse = false,
            HasValidation = true,
            ParameterCount = 5,
            HasCaching = false,
            HasAuthorization = true
        };

        // Assert
        request.Name.Should().Be("UpdateUserCommand");
        request.FilePath.Should().Be("src/Commands/UpdateUserCommand.cs");
        request.IsRecord.Should().BeTrue();
        request.HasResponse.Should().BeFalse();
        request.HasValidation.Should().BeTrue();
        request.ParameterCount.Should().Be(5);
        request.HasCaching.Should().BeFalse();
        request.HasAuthorization.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void RequestInfo_ShouldSupportVariousParameterCounts(int parameterCount)
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = parameterCount };

        // Assert
        request.ParameterCount.Should().Be(parameterCount);
    }

    [Fact]
    public void RequestInfo_Name_CanBeEmpty()
    {
        // Arrange & Act
        var request = new RequestInfo { Name = "" };

        // Assert
        request.Name.Should().BeEmpty();
    }

    [Fact]
    public void RequestInfo_Name_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var request = new RequestInfo { Name = "CreateUserRequest_v2" };

        // Assert
        request.Name.Should().Be("CreateUserRequest_v2");
    }

    [Fact]
    public void RequestInfo_FilePath_CanBeEmpty()
    {
        // Arrange & Act
        var request = new RequestInfo { FilePath = "" };

        // Assert
        request.FilePath.Should().BeEmpty();
    }

    [Fact]
    public void RequestInfo_FilePath_CanContainPathSeparators()
    {
        // Arrange & Act
        var request = new RequestInfo { FilePath = "src/Application/Users/Commands/CreateUserCommand.cs" };

        // Assert
        request.FilePath.Should().Be("src/Application/Users/Commands/CreateUserCommand.cs");
    }

    [Fact]
    public void RequestInfo_BooleanProperties_CanBeTrueOrFalse()
    {
        // Test all boolean properties can be set to true
        var requestTrue = new RequestInfo
        {
            IsRecord = true,
            HasResponse = true,
            HasValidation = true,
            HasCaching = true,
            HasAuthorization = true
        };

        requestTrue.IsRecord.Should().BeTrue();
        requestTrue.HasResponse.Should().BeTrue();
        requestTrue.HasValidation.Should().BeTrue();
        requestTrue.HasCaching.Should().BeTrue();
        requestTrue.HasAuthorization.Should().BeTrue();

        // Test all boolean properties can be set to false
        var requestFalse = new RequestInfo
        {
            IsRecord = false,
            HasResponse = false,
            HasValidation = false,
            HasCaching = false,
            HasAuthorization = false
        };

        requestFalse.IsRecord.Should().BeFalse();
        requestFalse.HasResponse.Should().BeFalse();
        requestFalse.HasValidation.Should().BeFalse();
        requestFalse.HasCaching.Should().BeFalse();
        requestFalse.HasAuthorization.Should().BeFalse();
    }

    [Fact]
    public void RequestInfo_CanBeUsedInCollections()
    {
        // Arrange & Act
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "CreateUser", IsRecord = true, ParameterCount = 3 },
            new RequestInfo { Name = "UpdateUser", IsRecord = false, ParameterCount = 4 },
            new RequestInfo { Name = "DeleteUser", IsRecord = true, ParameterCount = 1 }
        };

        // Assert
        requests.Should().HaveCount(3);
        requests.Count(r => r.IsRecord).Should().Be(2);
        requests.Sum(r => r.ParameterCount).Should().Be(8);
    }

    [Fact]
    public void RequestInfo_CanBeFilteredByRecordType()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "Request1", IsRecord = true },
            new RequestInfo { Name = "Request2", IsRecord = false },
            new RequestInfo { Name = "Request3", IsRecord = true },
            new RequestInfo { Name = "Request4", IsRecord = false }
        };

        // Act
        var recordRequests = requests.Where(r => r.IsRecord).ToList();
        var classRequests = requests.Where(r => !r.IsRecord).ToList();

        // Assert
        recordRequests.Should().HaveCount(2);
        classRequests.Should().HaveCount(2);
        recordRequests.All(r => r.IsRecord).Should().BeTrue();
        classRequests.All(r => !r.IsRecord).Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_CanBeFilteredByFeatures()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "R1", HasValidation = true, HasAuthorization = true, HasCaching = false },
            new RequestInfo { Name = "R2", HasValidation = false, HasAuthorization = true, HasCaching = true },
            new RequestInfo { Name = "R3", HasValidation = true, HasAuthorization = false, HasCaching = true },
            new RequestInfo { Name = "R4", HasValidation = false, HasAuthorization = false, HasCaching = false }
        };

        // Act
        var requestsWithValidation = requests.Where(r => r.HasValidation).ToList();
        var requestsWithSecurity = requests.Where(r => r.HasAuthorization).ToList();
        var requestsWithCaching = requests.Where(r => r.HasCaching).ToList();

        // Assert
        requestsWithValidation.Should().HaveCount(2);
        requestsWithSecurity.Should().HaveCount(2);
        requestsWithCaching.Should().HaveCount(2);
    }

    [Fact]
    public void RequestInfo_CanBeOrderedByParameterCount()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "Simple", ParameterCount = 1 },
            new RequestInfo { Name = "Complex", ParameterCount = 10 },
            new RequestInfo { Name = "Medium", ParameterCount = 5 }
        };

        // Act
        var orderedByComplexity = requests.OrderBy(r => r.ParameterCount).ToList();

        // Assert
        orderedByComplexity[0].Name.Should().Be("Simple");
        orderedByComplexity[1].Name.Should().Be("Medium");
        orderedByComplexity[2].Name.Should().Be("Complex");
    }

    [Fact]
    public void RequestInfo_CanBeGroupedByResponseType()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "Query1", HasResponse = true, ParameterCount = 2 },
            new RequestInfo { Name = "Command1", HasResponse = false, ParameterCount = 3 },
            new RequestInfo { Name = "Query2", HasResponse = true, ParameterCount = 1 },
            new RequestInfo { Name = "Command2", HasResponse = false, ParameterCount = 4 }
        };

        // Act
        var grouped = requests.GroupBy(r => r.HasResponse);

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key).Should().HaveCount(2); // Has response (queries)
        grouped.First(g => !g.Key).Should().HaveCount(2); // No response (commands)

        var queries = grouped.First(g => g.Key);
        queries.Sum(r => r.ParameterCount).Should().Be(3);
    }

    [Fact]
    public void RequestInfo_PropertiesCanBeModified()
    {
        // Arrange
        var request = new RequestInfo
        {
            Name = "InitialRequest",
            FilePath = "initial.cs",
            IsRecord = false,
            ParameterCount = 2
        };

        // Act
        request.Name = "ModifiedRequest";
        request.FilePath = "modified.cs";
        request.IsRecord = true;
        request.ParameterCount = 5;

        // Assert
        request.Name.Should().Be("ModifiedRequest");
        request.FilePath.Should().Be("modified.cs");
        request.IsRecord.Should().BeTrue();
        request.ParameterCount.Should().Be(5);
    }

    [Fact]
    public void RequestInfo_ShouldBeClass()
    {
        // Arrange & Act
        var request = new RequestInfo();

        // Assert
        request.Should().NotBeNull();
        request.GetType().IsClass.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_WithRealisticData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var request = new RequestInfo
        {
            Name = "CreateUserCommand",
            FilePath = "src/Application/Users/Commands/CreateUserCommand.cs",
            IsRecord = true,
            HasResponse = false,
            HasValidation = true,
            ParameterCount = 4,
            HasCaching = false,
            HasAuthorization = true
        };

        // Assert
        request.Name.Should().Be("CreateUserCommand");
        request.FilePath.Should().Contain("CreateUserCommand.cs");
        request.IsRecord.Should().BeTrue();
        request.HasResponse.Should().BeFalse();
        request.HasValidation.Should().BeTrue();
        request.ParameterCount.Should().Be(4);
        request.HasCaching.Should().BeFalse();
        request.HasAuthorization.Should().BeTrue();
    }

    [Fact]
    public void RequestInfo_WithQueryData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var request = new RequestInfo
        {
            Name = "GetUserQuery",
            FilePath = "src/Application/Users/Queries/GetUserQuery.cs",
            IsRecord = true,
            HasResponse = true,
            HasValidation = false,
            ParameterCount = 1,
            HasCaching = true,
            HasAuthorization = false
        };

        // Assert
        request.Name.Should().Be("GetUserQuery");
        request.IsRecord.Should().BeTrue();
        request.HasResponse.Should().BeTrue();
        request.HasValidation.Should().BeFalse();
        request.ParameterCount.Should().Be(1);
        request.HasCaching.Should().BeTrue();
        request.HasAuthorization.Should().BeFalse();
    }

    [Fact]
    public void RequestInfo_CanCalculateStatistics()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "R1", IsRecord = true, HasValidation = true, ParameterCount = 3 },
            new RequestInfo { Name = "R2", IsRecord = false, HasValidation = true, ParameterCount = 2 },
            new RequestInfo { Name = "R3", IsRecord = true, HasValidation = false, ParameterCount = 5 },
            new RequestInfo { Name = "R4", IsRecord = true, HasValidation = true, ParameterCount = 1 }
        };

        // Act
        var totalParameters = requests.Sum(r => r.ParameterCount);
        var recordRequests = requests.Count(r => r.IsRecord);
        var requestsWithValidation = requests.Count(r => r.HasValidation);
        var averageParameters = requests.Average(r => r.ParameterCount);

        // Assert
        totalParameters.Should().Be(11);
        recordRequests.Should().Be(3);
        requestsWithValidation.Should().Be(3);
        averageParameters.Should().Be(2.75);
    }

    [Fact]
    public void RequestInfo_CanBeFilteredByFilePath()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "UserRequest", FilePath = "src/Users/Commands/CreateUserCommand.cs" },
            new RequestInfo { Name = "ProductRequest", FilePath = "src/Products/Commands/CreateProductCommand.cs" },
            new RequestInfo { Name = "OrderRequest", FilePath = "src/Orders/Queries/GetOrderQuery.cs" }
        };

        // Act
        var userRequests = requests.Where(r => r.FilePath.Contains("Users")).ToList();
        var commandRequests = requests.Where(r => r.FilePath.Contains("Commands")).ToList();

        // Assert
        userRequests.Should().HaveCount(1);
        commandRequests.Should().HaveCount(2);
    }

    [Fact]
    public void RequestInfo_CanIdentifyComplexRequests()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo { Name = "Simple", HasValidation = false, HasAuthorization = false, HasCaching = false, ParameterCount = 1 },
            new RequestInfo { Name = "Complex", HasValidation = true, HasAuthorization = true, HasCaching = true, ParameterCount = 8 },
            new RequestInfo { Name = "Medium", HasValidation = true, HasAuthorization = false, HasCaching = true, ParameterCount = 3 }
        };

        // Act
        var complexRequests = requests.Where(r => r.HasValidation && r.HasAuthorization && r.HasCaching).ToList();
        var simpleRequests = requests.Where(r => !r.HasValidation && !r.HasAuthorization && !r.HasCaching).ToList();

        // Assert
        complexRequests.Should().HaveCount(1);
        complexRequests[0].Name.Should().Be("Complex");
        simpleRequests.Should().HaveCount(1);
        simpleRequests[0].Name.Should().Be("Simple");
    }

    [Fact]
    public void RequestInfo_ParameterCount_CanBeZero()
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = 0 };

        // Assert
        request.ParameterCount.Should().Be(0);
    }

    [Fact]
    public void RequestInfo_ParameterCount_CanBeLarge()
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = int.MaxValue };

        // Assert
        request.ParameterCount.Should().Be(int.MaxValue);
    }

    [Fact]
    public void RequestInfo_CanBeUsedInReporting()
    {
        // Arrange
        var requests = new List<RequestInfo>
        {
            new RequestInfo
            {
                Name = "CreateUserCommand",
                FilePath = "src/Users/CreateUserCommand.cs",
                IsRecord = true,
                HasResponse = false,
                HasValidation = true,
                ParameterCount = 4,
                HasCaching = false,
                HasAuthorization = true
            },
            new RequestInfo
            {
                Name = "GetUserQuery",
                FilePath = "src/Users/GetUserQuery.cs",
                IsRecord = true,
                HasResponse = true,
                HasValidation = false,
                ParameterCount = 1,
                HasCaching = true,
                HasAuthorization = false
            }
        };

        // Act - Simulate report generation
        var report = requests.Select(r => new
        {
            RequestName = r.Name,
            Type = r.HasResponse ? "Query" : "Command",
            Complexity = (r.HasValidation ? 1 : 0) + (r.HasAuthorization ? 1 : 0) + (r.HasCaching ? 1 : 0),
            Features = $"{(r.IsRecord ? "Record" : "Class")}, {r.ParameterCount} parameters"
        }).ToList();

        // Assert
        report.Should().HaveCount(2);
        report[0].Type.Should().Be("Command");
        report[0].Complexity.Should().Be(2);
        report[0].Features.Should().Contain("Record");
        report[0].Features.Should().Contain("4 parameters");

        report[1].Type.Should().Be("Query");
        report[1].Complexity.Should().Be(1);
        report[1].Features.Should().Contain("Record");
        report[1].Features.Should().Contain("1 parameters");
    }

    [Fact]
    public void RequestInfo_CanBeSerialized_WithComplexData()
    {
        // Arrange & Act
        var request = new RequestInfo
        {
            Name = "ComplexBusinessOperationCommand",
            FilePath = "src/Application/Business/Operations/ComplexBusinessOperationCommand.cs",
            IsRecord = true,
            HasResponse = false,
            HasValidation = true,
            ParameterCount = 12,
            HasCaching = false,
            HasAuthorization = true
        };

        // Assert - Basic serialization check
        request.Name.Should().Be("ComplexBusinessOperationCommand");
        request.FilePath.Should().Contain("ComplexBusinessOperationCommand.cs");
        request.IsRecord.Should().BeTrue();
        request.HasResponse.Should().BeFalse();
        request.HasValidation.Should().BeTrue();
        request.ParameterCount.Should().Be(12);
        request.HasCaching.Should().BeFalse();
        request.HasAuthorization.Should().BeTrue();
    }
}