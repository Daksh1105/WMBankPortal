using WMBankPortal.Domain.Common;
using WMBankPortal.Domain.Exceptions;
using Xunit;

namespace WMBankPortal.UnitTests;

public class DomainPrimitivesTests
{
    [Fact]
    public void Result_Success_ShouldHaveIsSuccessTrueAndNoError()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Result_Failure_ShouldHaveIsFailureTrueAndCorrectError()
    {
        // Arrange
        var error = Error.Validation("TEST_ERROR", "A test validation error.");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void GenericResult_Success_ShouldReturnExpectedValue()
    {
        // Act
        var result = Result.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericResult_Failure_AccessingValueShouldThrowInvalidOperationException()
    {
        // Arrange
        var result = Result.Failure<string>(Error.NotFound("USER_NOT_FOUND", "User does not exist."));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Theory]
    [InlineData(10, 5, 2)]
    [InlineData(11, 5, 3)]
    [InlineData(0, 5, 0)]
    public void PagedResult_ShouldCalculateTotalPagesCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Act
        var pagedResult = new PagedResult<string>(Array.Empty<string>(), totalCount, 1, pageSize);

        // Assert
        Assert.Equal(expectedPages, pagedResult.TotalPages);
    }

    [Fact]
    public void InvalidStateTransitionException_ShouldFormatDiagnosticMessage()
    {
        // Act
        var ex = new InvalidStateTransitionException("BankingApplication", "Approved", "Submitted", "Terminal state reached");

        // Assert
        Assert.Contains("BankingApplication", ex.Message);
        Assert.Contains("Approved", ex.Message);
        Assert.Contains("Submitted", ex.Message);
        Assert.Contains("Terminal state reached", ex.Message);
    }
}
