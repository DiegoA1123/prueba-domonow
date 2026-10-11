# ADR-0004: Autenticación con JWT y usuario operador de prueba

- **Estado**: Aceptado
- **Fecha**: 2026-10-10
- **Relacionado**: ADR-0001, ADR-0003

## Contexto

El enunciado (§2) indica que "un **usuario autorizado** registra la utilización de un parqueadero" y la rúbrica evalúa "buenas prácticas de seguridad". La IA propuso dejar la autenticación fuera de alcance para invertir el tiempo en dominio y concurrencia; **el ingeniero lo rechazó**: un requisito explícito del enunciado no se convierte en supuesto si hay forma razonable de cumplirlo.

## Decisión

- **JWT Bearer** emitido por la propia API (`POST /api/auth/login`), firmado con HMAC-SHA256. Expiración corta (60 min). Sin refresh tokens.
- **Usuarios = operadores** (personal de portería/administración) persistidos en tabla `operators` con contraseña **hasheada** (`PasswordHasher<T>` de ASP.NET Core Identity: PBKDF2 con salt). Un operador de prueba se siembra en Development con credenciales tomadas de `dotnet user-secrets`, nunca del repo.
- Todos los endpoints de parqueaderos exigen `[Authorize]`; la escritura (ingreso/salida) exige el rol `Operator`.
- **DDD**: la autenticación es un **subdominio genérico** (no es el core del negocio). No vive en el bounded context `Parking`:
  - `Application/Authentication/Login/` — caso de uso `LoginHandler`.
  - Puertos: `IOperatorCredentialStore` (validar credenciales) e `IAccessTokenIssuer` (emitir token).
  - Adaptadores en `Infrastructure/Identity/` (EF Core + `PasswordHasher` + `JsonWebTokenHandler`, el handler moderno que reemplaza a `JwtSecurityTokenHandler`).
  - El dominio `Parking` no conoce usuarios ni tokens.
- Clave de firma y credenciales de prueba: `dotnet user-secrets` (Development) / variables de entorno (otros entornos). El arranque falla si la clave no está configurada o mide < 32 bytes.
- **Frontend**: `core/auth/` con servicio de sesión (token en memoria + `sessionStorage`), interceptor que añade `Authorization: Bearer`, guard de rutas y página de login. Un `401` limpia la sesión y redirige a login.

## Alternativas consideradas

| Opción | Por qué no |
|--------|-----------|
| Fuera de alcance (propuesta de la IA) | Ignora un requisito explícito ("usuario autorizado") y un criterio evaluado (seguridad) |
| API key estática | No identifica usuarios; no es autenticación de usuarios |
| ASP.NET Core Identity completo / OIDC externo (Keycloak, Entra ID) | Correcto en producción, desproporcionado para una prueba de 6 h; se menciona en "Evolución de la solución" |
| Usuarios en `appsettings.json` | Mezcla secretos con configuración versionada |

## Consecuencias

- (+) Cumple el requisito de usuario autorizado y demuestra prácticas de seguridad (hash, secretos fuera del repo, expiración, roles).
- (+) Abre la puerta a auditoría: registrar qué operador hizo el ingreso/salida (opcional si sobra tiempo).
- (−) ~1 h adicional de implementación entre backend y frontend; se compensa recortando opcionales.
- (−) Sin refresh tokens ni revocación: documentado como mejora futura.
