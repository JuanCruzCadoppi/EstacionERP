using EstacionERP.Application.Common;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Seguridad;

public interface IAuthService
{
    Task<Resultado<UsuarioSesion>> IngresarAsync(string nombreUsuario, string password, CancellationToken ct = default);
    Task<Resultado> CambiarPasswordAsync(int usuarioId, string actual, string nueva, string confirmacion, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    public const int LargoMinimoPassword = 6;

    private readonly IEstacionDbContext _db;
    public AuthService(IEstacionDbContext db) => _db = db;

    public async Task<Resultado<UsuarioSesion>> IngresarAsync(string nombreUsuario, string password, CancellationToken ct = default)
    {
        var nombre = (nombreUsuario ?? string.Empty).Trim().ToLower();
        if (nombre.Length == 0 || string.IsNullOrEmpty(password))
            return Resultado<UsuarioSesion>.Error("Ingresá usuario y contraseña.");

        var usuario = await _db.Usuarios
            .Include(u => u.Unidades)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombre, ct);

        // Mismo mensaje si no existe o si la clave es incorrecta (no dar pistas).
        if (usuario is null || !PasswordHasher.Verificar(password, usuario.PasswordHash))
            return Resultado<UsuarioSesion>.Error("Usuario o contraseña incorrectos.");

        if (!usuario.Activo)
            return Resultado<UsuarioSesion>.Error("El usuario está desactivado. Consultá con el administrador.");

        List<int> unidades;
        if (usuario.Rol == Rol.Administrador)
            unidades = await _db.UnidadesNegocio.Where(x => x.Activa).Select(x => x.Id).ToListAsync(ct);
        else
            unidades = usuario.Unidades.Select(x => x.UnidadNegocioId).ToList();

        usuario.UltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Resultado<UsuarioSesion>.Ok(new UsuarioSesion(
            usuario.Id, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol,
            unidades, usuario.DebeCambiarPassword));
    }

    public async Task<Resultado> CambiarPasswordAsync(int usuarioId, string actual, string nueva, string confirmacion, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is null) return Resultado.Error("El usuario no existe.");

        if (!PasswordHasher.Verificar(actual, usuario.PasswordHash))
            return Resultado.Error("La contraseña actual no es correcta.");

        var error = ValidarNueva(nueva);
        if (error is not null) return Resultado.Error(error);

        if (nueva != confirmacion)
            return Resultado.Error("La confirmación no coincide con la nueva contraseña.");

        if (nueva == actual)
            return Resultado.Error("La nueva contraseña tiene que ser distinta de la actual.");

        usuario.PasswordHash = PasswordHasher.Hashear(nueva);
        usuario.DebeCambiarPassword = false;
        usuario.ModificadoEn = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public static string? ValidarNueva(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < LargoMinimoPassword
            ? $"La contraseña debe tener al menos {LargoMinimoPassword} caracteres."
            : null;
}
