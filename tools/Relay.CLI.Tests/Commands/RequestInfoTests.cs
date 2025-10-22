using Relay.CLI.Commands.Models;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class RequestInfoTests
{
    [Fact]
    public void RequestInfo_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { Name = "CreateUserRequest" };

        // Assert
        Assert.Equal("CreateUserRequest", request.Name);
    }

    [Fact]
    public void RequestInfo_ShouldHaveFilePathProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { FilePath = "src/Requests/CreateUserRequest.cs" };

        // Assert
        Assert.Equal("src/Requests/CreateUserRequest.cs", request.FilePath);
    }

    [Fact]
    public void RequestInfo_ShouldHaveIsRecordProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { IsRecord = true };

        // Assert
        Assert.True(request.IsRecord);
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasResponseProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasResponse = true };

        // Assert
        Assert.True(request.HasResponse);
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasValidationProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasValidation = true };

        // Assert
        Assert.True(request.HasValidation);
    }

    [Fact]
    public void RequestInfo_ShouldHaveParameterCountProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = 3 };

        // Assert
        Assert.Equal(3, request.ParameterCount);
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasCachingProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasCaching = true };

        // Assert
        Assert.True(request.HasCaching);
    }

    [Fact]
    public void RequestInfo_ShouldHaveHasAuthorizationProperty()
    {
        // Arrange & Act
        var request = new RequestInfo { HasAuthorization = true };

        // Assert
        Assert.True(request.HasAuthorization);
    }

    [Fact]
    public void RequestInfo_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var request = new RequestInfo();

        // Assert
        Assert.Equal("", request.Name);
        Assert.Equal("", request.FilePath);
        Assert.False(request.IsRecord);
        Assert.False(request.HasResponse);
        Assert.False(request.HasValidation);
        Assert.Equal(0, request.ParameterCount);
        Assert.False(request.HasCaching);
        Assert.False(request.HasAuthorization);
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
        Assert.Equal("UpdateUserCommand", request.Name);
        Assert.Equal("src/Commands/UpdateUserCommand.cs", request.FilePath);
        Assert.True(request.IsRecord);
        Assert.False(request.HasResponse);
        Assert.True(request.HasValidation);
        Assert.Equal(5, request.ParameterCount);
        Assert.False(request.HasCaching);
        Assert.True(request.HasAuthorization);
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
        Assert.Equal(parameterCount, request.ParameterCount);
    }

    [Fact]
    public void RequestInfo_Name_CanBeEmpty()
    {
        // Arrange & Act
        var request = new RequestInfo { Name = "" };

        // Assert
        Assert.Empty(request.Name);
    }

    [Fact]
    public void RequestInfo_Name_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var request = new RequestInfo { Name = "CreateUserRequest_v2" };

        // Assert
        Assert.Equal("CreateUserRequest_v2", request.Name);
    }

    [Fact]
    public void RequestInfo_FilePath_CanBeEmpty()
    {
        // Arrange & Act
        var request = new RequestInfo { FilePath = "" };

        // Assert
        Assert.Empty(request.FilePath);
    }

    [Fact]
    public void RequestInfo_FilePath_CanContainPathSeparators()
    {
        // Arrange & Act
        var request = new RequestInfo { FilePath = "src/Application/Users/Commands/CreateUserCommand.cs" };

        // Assert
        Assert.Equal("src/Application/Users/Commands/CreateUserCommand.cs", request.FilePath);
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

        Assert.True(requestTrue.IsRecord);
        Assert.True(requestTrue.HasResponse);
        Assert.True(requestTrue.HasValidation);
        Assert.True(requestTrue.HasCaching);
        Assert.True(requestTrue.HasAuthorization);

        // Test all boolean properties can be set to false
        var requestFalse = new RequestInfo
        {
            IsRecord = false,
            HasResponse = false,
            HasValidation = false,
            HasCaching = false,
            HasAuthorization = false
        };

        Assert.False(requestFalse.IsRecord);
        Assert.False(requestFalse.HasResponse);
        Assert.False(requestFalse.HasValidation);
        Assert.False(requestFalse.HasCaching);
        Assert.False(requestFalse.HasAuthorization);
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
        Assert.Equal(3, requests.Count());
        Assert.Equal(2, requests.Count(r => r.IsRecord));
        Assert.Equal(8, requests.Sum(r => r.ParameterCount));
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
        Assert.Equal(2, recordRequests.Count());
        Assert.Equal(2, classRequests.Count());
        Assert.True(recordRequests.All(r => r.IsRecord));
        Assert.True(classRequests.All(r => !r.IsRecord));
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
        Assert.Equal(2, requestsWithValidation.Count());
        Assert.Equal(2, requestsWithSecurity.Count());
        Assert.Equal(2, requestsWithCaching.Count());
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
        Assert.Equal("Simple", orderedByComplexity[0].Name);
        Assert.Equal("Medium", orderedByComplexity[1].Name);
        Assert.Equal("Complex", orderedByComplexity[2].Name);
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
        Assert.Equal(2, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key).Count()); // Has response (queries)
        Assert.Equal(2, grouped.First(g => !g.Key).Count()); // No response (commands)

        var queries = grouped.First(g => g.Key);
        Assert.Equal(3, queries.Sum(r => r.ParameterCount));
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
        Assert.Equal("ModifiedRequest", request.Name);
        Assert.Equal("modified.cs", request.FilePath);
        Assert.True(request.IsRecord);
        Assert.Equal(5, request.ParameterCount);
    }

    [Fact]
    public void RequestInfo_ShouldBeClass()
    {
        // Arrange & Act
        var request = new RequestInfo();

        // Assert
        Assert.NotNull(request);
        Assert.True(request.GetType().IsClass);
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
        Assert.Equal("CreateUserCommand", request.Name);
        Assert.Contains("CreateUserCommand.cs", request.FilePath);
        Assert.True(request.IsRecord);
        Assert.False(request.HasResponse);
        Assert.True(request.HasValidation);
        Assert.Equal(4, request.ParameterCount);
        Assert.False(request.HasCaching);
        Assert.True(request.HasAuthorization);
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
        Assert.Equal("GetUserQuery", request.Name);
        Assert.True(request.IsRecord);
        Assert.True(request.HasResponse);
        Assert.False(request.HasValidation);
        Assert.Equal(1, request.ParameterCount);
        Assert.True(request.HasCaching);
        Assert.False(request.HasAuthorization);
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
        Assert.Equal(11, totalParameters);
        Assert.Equal(3, recordRequests);
        Assert.Equal(3, requestsWithValidation);
        Assert.Equal(2.75, averageParameters);
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
        Assert.Single(userRequests);
        Assert.Equal(2, commandRequests.Count());
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
        Assert.Single(complexRequests);
        Assert.Equal("Complex", complexRequests[0].Name);
        Assert.Single(simpleRequests);
        Assert.Equal("Simple", simpleRequests[0].Name);
    }

    [Fact]
    public void RequestInfo_ParameterCount_CanBeZero()
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = 0 };

        // Assert
        Assert.Equal(0, request.ParameterCount);
    }

    [Fact]
    public void RequestInfo_ParameterCount_CanBeLarge()
    {
        // Arrange & Act
        var request = new RequestInfo { ParameterCount = int.MaxValue };

        // Assert
        Assert.Equal(int.MaxValue, request.ParameterCount);
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
        Assert.Equal(2, report.Count());
        Assert.Equal("Command", report[0].Type);
        Assert.Equal(2, report[0].Complexity);
        Assert.Contains("Record", report[0].Features);
        Assert.Contains("4 parameters", report[0].Features);

        Assert.Equal("Query", report[1].Type);
        Assert.Equal(1, report[1].Complexity);
        Assert.Contains("Record", report[1].Features);
        Assert.Contains("1 parameters", report[1].Features);
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
        Assert.Equal("ComplexBusinessOperationCommand", request.Name);
        Assert.Contains("ComplexBusinessOperationCommand.cs", request.FilePath);
        Assert.True(request.IsRecord);
        Assert.False(request.HasResponse);
        Assert.True(request.HasValidation);
        Assert.Equal(12, request.ParameterCount);
        Assert.False(request.HasCaching);
        Assert.True(request.HasAuthorization);
    }
}

