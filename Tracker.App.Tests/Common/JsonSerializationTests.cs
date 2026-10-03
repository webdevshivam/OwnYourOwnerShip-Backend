using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Tracker.App.Common.Models;
using Xunit;

namespace Tracker.App.Tests.Common;

public class JsonSerializationTests
{
    [Fact]
    public void ApiResponse_WhenSerializedWithWhenWritingNull_ShouldOmitNullProperties()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var response = ApiResponse.Ok("Success message");
        // Data is null, Errors is null, Pagination is null

        // Act
        var json = JsonSerializer.Serialize(response, options);

        // Assert: Ensure null fields are completely omitted from the JSON string
        json.Should().NotContain("\"data\":");
        json.Should().NotContain("\"errors\":");
        json.Should().NotContain("\"pagination\":");
        json.Should().Contain("\"success\":true");
        json.Should().Contain("\"message\":\"Success message\"");
    }

    [Fact]
    public void GenericApiResponse_WhenSerializedWithWhenWritingNull_ShouldOmitNullFields()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var response = ApiResponse<string>.Fail("Validation failed", 400, null);

        // Act
        var json = JsonSerializer.Serialize(response, options);

        // Assert: Ensure null data, errors, and pagination fields are not written
        json.Should().NotContain("\"data\":");
        json.Should().NotContain("\"errors\":");
        json.Should().NotContain("\"pagination\":");
        json.Should().Contain("\"success\":false");
        json.Should().Contain("\"statusCode\":400");
    }
}
