using AycaMusic.Data;
using AycaMusic.Services;

namespace AycaMusic.Tests.Services;

public class AuthServiceTests
{
    // ===== ValidatePassword Tests =====

    [Fact]
    public void ValidatePassword_ValidPassword_ShouldReturnTrue()
    {
        var (isValid, errors) = AuthService.ValidatePassword("Test1234!");
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidatePassword_TooShort_ShouldFail()
    {
        var (isValid, errors) = AuthService.ValidatePassword("Te1!");
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("8 karakter"));
    }

    [Fact]
    public void ValidatePassword_NoUpperCase_ShouldFail()
    {
        var (isValid, errors) = AuthService.ValidatePassword("test1234!");
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("büyük harf"));
    }

    [Fact]
    public void ValidatePassword_NoLowerCase_ShouldFail()
    {
        var (isValid, errors) = AuthService.ValidatePassword("TEST1234!");
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("küçük harf"));
    }

    [Fact]
    public void ValidatePassword_NoDigit_ShouldFail()
    {
        var (isValid, errors) = AuthService.ValidatePassword("TestTest!");
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("rakam"));
    }

    [Fact]
    public void ValidatePassword_NoSpecialChar_ShouldFail()
    {
        var (isValid, errors) = AuthService.ValidatePassword("Test1234a");
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("özel karakter"));
    }

    [Fact]
    public void ValidatePassword_EmptyPassword_ShouldReturnMultipleErrors()
    {
        var (isValid, errors) = AuthService.ValidatePassword("");
        Assert.False(isValid);
        Assert.True(errors.Count >= 4); // length + upper + digit + special
    }

    [Fact]
    public void ValidatePassword_ExactlyEightChars_ShouldPass()
    {
        var (isValid, errors) = AuthService.ValidatePassword("Abcde1@x");
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidatePassword_AllSpecialChars_ShouldFailUpperLowerDigit()
    {
        var (isValid, errors) = AuthService.ValidatePassword("!@#$%^&*");
        Assert.False(isValid);
        Assert.True(errors.Count >= 2);
    }

    [Theory]
    [InlineData("Abc123!x", true)]
    [InlineData("abcdefgh", false)]
    [InlineData("ABCDEFGH", false)]
    [InlineData("12345678", false)]
    [InlineData("!@#$%^&*()", false)]
    public void ValidatePassword_VariousCombinations(string password, bool expectedValid)
    {
        var (isValid, _) = AuthService.ValidatePassword(password);
        Assert.Equal(expectedValid, isValid);
    }

    // ===== HashPassword Tests =====

    [Fact]
    public void HashPassword_SameInput_ShouldReturnSameHash()
    {
        var hash1 = AppDbContext.HashPassword("MyPassword123");
        var hash2 = AppDbContext.HashPassword("MyPassword123");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_DifferentInputs_ShouldReturnDifferentHashes()
    {
        var hash1 = AppDbContext.HashPassword("Password1");
        var hash2 = AppDbContext.HashPassword("Password2");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_ShouldNotReturnPlainText()
    {
        var password = "SecurePass123!";
        var hash = AppDbContext.HashPassword(password);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void HashPassword_EmptyString_ShouldReturnValidHash()
    {
        var hash = AppDbContext.HashPassword("");
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_CaseSensitive_ShouldReturnDifferentHashes()
    {
        var hash1 = AppDbContext.HashPassword("password");
        var hash2 = AppDbContext.HashPassword("Password");
        Assert.NotEqual(hash1, hash2);
    }
}
