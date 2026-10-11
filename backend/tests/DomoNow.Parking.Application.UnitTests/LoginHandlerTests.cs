using DomoNow.Parking.Application.Authentication;

namespace DomoNow.Parking.Application.UnitTests;

public sealed class LoginHandlerTests
{
    private static readonly AuthenticatedOperator Portero = new(Guid.NewGuid(), "portero", Roles.Operator);

    private sealed class StubCredentialStore : IOperatorCredentialStore
    {
        public Task<AuthenticatedOperator?> ValidateAsync(string username, string password, CancellationToken cancellationToken) =>
            Task.FromResult(username == "portero" && password == "correct-password" ? Portero : null);
    }

    private sealed class StubTokenIssuer : IAccessTokenIssuer
    {
        public AccessToken Issue(AuthenticatedOperator authenticatedOperator) =>
            new($"token-for-{authenticatedOperator.Username}", DateTimeOffset.UnixEpoch);
    }

    private readonly LoginHandler _handler = new(new StubCredentialStore(), new StubTokenIssuer());

    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsToken()
    {
        var result = await _handler.HandleAsync(new LoginCommand("portero", "correct-password"), CancellationToken.None);

        result.Value.AccessToken.ShouldBe("token-for-portero");
        result.Value.Role.ShouldBe(Roles.Operator);
    }

    [Fact]
    public async Task HandleAsync_WithWrongPassword_ReturnsUnauthorized()
    {
        var result = await _handler.HandleAsync(new LoginCommand("portero", "wrong"), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("portero", "")]
    public async Task HandleAsync_WithMissingFields_ReturnsValidationError(string? username, string? password)
    {
        var result = await _handler.HandleAsync(new LoginCommand(username, password), CancellationToken.None);

        result.Error.ShouldBe(AuthErrors.CredentialsRequired);
    }
}
