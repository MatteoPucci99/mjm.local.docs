using Mjm.LocalDocs.Core.Abstractions;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;
using NSubstitute;

namespace Mjm.LocalDocs.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ApiTokenService"/>.
/// </summary>
public sealed class ApiTokenServiceTests
{
    private readonly IApiTokenRepository _repository;
    private readonly ApiTokenService _sut;

    public ApiTokenServiceTests()
    {
        _repository = Substitute.For<IApiTokenRepository>();
        _sut = new ApiTokenService(_repository);
    }

    #region Helper Methods

    private static ApiToken CreateTestToken(
        string id = "token-1",
        string name = "Test Token",
        bool isRevoked = false,
        DateTimeOffset? expiresAt = null)
    {
        return new ApiToken
        {
            Id = id,
            Name = name,
            TokenHash = "testhash123",
            TokenPrefix = "testpref",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            LastUsedAt = null,
            IsRevoked = isRevoked
        };
    }

    #endregion

    #region CreateTokenAsync Tests

    [Fact]
    public async Task CreateTokenAsync_WithValidName_CreatesTokenAndReturnsPlainText()
    {
        // Arrange
        var name = "My Test Token";
        _repository.CreateAsync(Arg.Any<ApiToken>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ApiToken>());

        // Act
        var (token, plainTextToken) = await _sut.CreateTokenAsync(name);

        // Assert
        Assert.NotNull(token);
        Assert.Equal(name, token.Name);
        Assert.NotEmpty(token.Id);
        Assert.NotEmpty(token.TokenHash);
        Assert.NotEmpty(token.TokenPrefix!);
        Assert.False(token.IsRevoked);
        Assert.Null(token.ExpiresAt);

        Assert.NotEmpty(plainTextToken);
        Assert.StartsWith(token.TokenPrefix!, plainTextToken);

        await _repository.Received(1).CreateAsync(
            Arg.Is<ApiToken>(t => t.Name == name && !t.IsRevoked),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTokenAsync_WithExpiration_SetsExpiresAt()
    {
        // Arrange
        var name = "Expiring Token";
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        _repository.CreateAsync(Arg.Any<ApiToken>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ApiToken>());

        // Act
        var (token, _) = await _sut.CreateTokenAsync(name, expiresAt);

        // Assert
        Assert.Equal(expiresAt, token.ExpiresAt);
    }

    [Fact]
    public async Task CreateTokenAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateTokenAsync(string.Empty));
    }

    [Fact]
    public async Task CreateTokenAsync_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateTokenAsync("   "));
    }

    #endregion

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsToken()
    {
        // Arrange
        var plainTextToken = "validToken123";
        var token = CreateTestToken();

        _repository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);
        _repository.UpdateAsync(Arg.Any<ApiToken>(), Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.ValidateTokenAsync(plainTextToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<ApiToken>(t => t.LastUsedAt != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var plainTextToken = "invalidToken";
        _repository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ApiToken?)null);

        // Act
        var result = await _sut.ValidateTokenAsync(plainTextToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithRevokedToken_ReturnsNull()
    {
        // Arrange
        var plainTextToken = "revokedToken";
        var token = CreateTestToken(isRevoked: true);

        _repository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.ValidateTokenAsync(plainTextToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var plainTextToken = "expiredToken";
        var token = CreateTestToken(expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        _repository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.ValidateTokenAsync(plainTextToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithEmptyToken_ReturnsNull()
    {
        // Arrange & Act
        var result = await _sut.ValidateTokenAsync(string.Empty);

        // Assert
        Assert.Null(result);
        await _repository.DidNotReceive().GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateTokenAsync_WhenUpdateLastUsedIsFalse_DoesNotUpdateLastUsed()
    {
        // Arrange
        var plainTextToken = "validToken123";
        var token = CreateTestToken();

        _repository.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.ValidateTokenAsync(plainTextToken, updateLastUsed: false);

        // Assert
        Assert.NotNull(result);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<ApiToken>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetAllTokensAsync Tests

    [Fact]
    public async Task GetAllTokensAsync_ReturnsAllTokens()
    {
        // Arrange
        var tokens = new List<ApiToken>
        {
            CreateTestToken("token-1", "Token 1"),
            CreateTestToken("token-2", "Token 2")
        };
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(tokens);

        // Act
        var result = await _sut.GetAllTokensAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetTokenByIdAsync Tests

    [Fact]
    public async Task GetTokenByIdAsync_WhenExists_ReturnsToken()
    {
        // Arrange
        var tokenId = "token-1";
        var token = CreateTestToken(tokenId);
        _repository.GetByIdAsync(tokenId, Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.GetTokenByIdAsync(tokenId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tokenId, result.Id);
    }

    [Fact]
    public async Task GetTokenByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var tokenId = "non-existent";
        _repository.GetByIdAsync(tokenId, Arg.Any<CancellationToken>())
            .Returns((ApiToken?)null);

        // Act
        var result = await _sut.GetTokenByIdAsync(tokenId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region RevokeTokenAsync Tests

    [Fact]
    public async Task RevokeTokenAsync_WhenTokenExists_RevokesAndReturnsTrue()
    {
        // Arrange
        var tokenId = "token-1";
        var token = CreateTestToken(tokenId);
        _repository.GetByIdAsync(tokenId, Arg.Any<CancellationToken>())
            .Returns(token);
        _repository.UpdateAsync(Arg.Any<ApiToken>(), Arg.Any<CancellationToken>())
            .Returns(token);

        // Act
        var result = await _sut.RevokeTokenAsync(tokenId);

        // Assert
        Assert.True(result);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<ApiToken>(t => t.IsRevoked),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeTokenAsync_WhenTokenNotFound_ReturnsFalse()
    {
        // Arrange
        var tokenId = "non-existent";
        _repository.GetByIdAsync(tokenId, Arg.Any<CancellationToken>())
            .Returns((ApiToken?)null);

        // Act
        var result = await _sut.RevokeTokenAsync(tokenId);

        // Assert
        Assert.False(result);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<ApiToken>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteTokenAsync Tests

    [Fact]
    public async Task DeleteTokenAsync_WhenTokenExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var tokenId = "token-1";
        _repository.DeleteAsync(tokenId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.DeleteTokenAsync(tokenId);

        // Assert
        Assert.True(result);
        await _repository.Received(1).DeleteAsync(tokenId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteTokenAsync_WhenTokenNotFound_ReturnsFalse()
    {
        // Arrange
        var tokenId = "non-existent";
        _repository.DeleteAsync(tokenId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.DeleteTokenAsync(tokenId);

        // Assert
        Assert.False(result);
    }

    #endregion
}
