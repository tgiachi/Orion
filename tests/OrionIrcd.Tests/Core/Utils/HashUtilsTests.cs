using OrionIrcd.Core.Utils;

namespace OrionIrcd.Tests.Core.Utils;

public class HashUtilsTests
{
    [Fact]
    public void HashPassword_WhenCalledTwice_ProducesDifferentHashes()
    {
        const string password = "MySecurePassword123!";

        var firstHash = HashUtils.HashPassword(password);
        var secondHash = HashUtils.HashPassword(password);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ThrowsArgumentException()
        => Assert.Throws<ArgumentException>(() => HashUtils.HashPassword(string.Empty));

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        var isValid = HashUtils.VerifyPassword("password", "invalid-hash-format");

        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithMismatchedPassword_ReturnsFalse()
    {
        var hash = HashUtils.HashPassword("MySecurePassword123!");

        var isValid = HashUtils.VerifyPassword("WrongPassword", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithMatchingPassword_ReturnsTrue()
    {
        const string password = "MySecurePassword123!";
        var hash = HashUtils.HashPassword(password);

        var isValid = HashUtils.VerifyPassword(password, hash);

        Assert.True(isValid);
    }
}
