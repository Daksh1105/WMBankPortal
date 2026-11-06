using WMBankPortal.Application.Features.Auth.DTOs;
using WMBankPortal.Application.Features.Auth.Validators;
using Xunit;

namespace WMBankPortal.UnitTests;

public class AuthValidatorsTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public async Task RegisterRequestValidator_WithValidStrongPassword_ShouldPass()
    {
        // Arrange: 10+ chars, uppercase, lowercase, digit, special char
        var request = new RegisterRequest("testuser@wmportal.com", "Password123!Secure", "Password123!Secure");

        // Act
        var result = await _registerValidator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("Short1!")]                      // Less than 10 characters
    [InlineData("password123!")]                 // Missing uppercase
    [InlineData("PASSWORD123!")]                 // Missing lowercase
    [InlineData("PasswordSecure!")]              // Missing digit
    [InlineData("Password123456")]               // Missing special character
    public async Task RegisterRequestValidator_WithWeakPassword_ShouldFail(string weakPassword)
    {
        // Arrange
        var request = new RegisterRequest("testuser@wmportal.com", weakPassword, weakPassword);

        // Act
        var result = await _registerValidator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task RegisterRequestValidator_WhenPasswordsMismatch_ShouldFail()
    {
        // Arrange
        var request = new RegisterRequest("testuser@wmportal.com", "Password123!Secure", "MismatchPassword123!");

        // Act
        var result = await _registerValidator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ConfirmPassword");
    }

    [Theory]
    [InlineData("invalid-email-format")]
    [InlineData("")]
    public async Task LoginRequestValidator_WithInvalidEmail_ShouldFail(string invalidEmail)
    {
        // Arrange
        var request = new LoginRequest(invalidEmail, "Password123!");

        // Act
        var result = await _loginValidator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}
