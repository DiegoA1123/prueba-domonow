using DomoNow.Parking.Domain.Common;

namespace DomoNow.Parking.Application.Authentication;

public static class AuthErrors
{
    public static readonly Error CredentialsRequired = Error.Validation(
        "Auth.CredentialsRequired", "El usuario y la contraseña son obligatorios.");

    // Mensaje genérico a propósito: no revela si el usuario existe.
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "Usuario o contraseña incorrectos.");
}
