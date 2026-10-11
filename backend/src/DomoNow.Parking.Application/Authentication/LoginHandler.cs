using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Application.Authentication;

public sealed class LoginHandler(IOperatorCredentialStore credentials, IAccessTokenIssuer tokens)
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrEmpty(command.Password))
        {
            return AuthErrors.CredentialsRequired;
        }

        var authenticated = await credentials.ValidateAsync(command.Username.Trim(), command.Password, cancellationToken);
        if (authenticated is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var token = tokens.Issue(authenticated);
        return new LoginResponse(token.Value, token.ExpiresAt, authenticated.Username, authenticated.Role);
    }
}
