using System.Security.Cryptography;

namespace EstacionERP.Application.Seguridad;

/// <summary>
/// Hash de contraseñas con PBKDF2-SHA256 (incluido en .NET, sin librerías externas).
/// Formato guardado: PBKDF2-SHA256$iteraciones$salt(base64)$hash(base64)
/// </summary>
public static class PasswordHasher
{
    private const int Iteraciones = 100_000;
    private const int LargoSalt = 16;
    private const int LargoHash = 32;
    private const string Prefijo = "PBKDF2-SHA256";

    public static string Hashear(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(LargoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, HashAlgorithmName.SHA256, LargoHash);
        return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string? hashGuardado)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashGuardado)) return false;

        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != Prefijo || !int.TryParse(partes[1], out var iteraciones))
            return false;

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            // Comparación en tiempo constante (evita ataques por tiempo de respuesta).
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
