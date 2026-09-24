using System.Text.RegularExpressions;
using EstacionERP.Application.Common;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Usuarios;

public class UsuarioDatos
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public Rol Rol { get; set; } = Rol.Operador;
    public bool Activo { get; set; } = true;
    public List<int> UnidadesNegocioIds { get; set; } = new();

    /// <summary>Obligatoria para usuarios nuevos. Al editar: vacía = no se cambia.</summary>
    public string? NuevaPassword { get; set; }
}

public record UsuarioResumen(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    string Rol,
    string Unidades,
    DateTime? UltimoAcceso,
    bool Activo);

public interface IUsuarioService
{
    Task<List<UsuarioResumen>> ListarAsync(bool incluirInactivos = false, CancellationToken ct = default);
    Task<UsuarioDatos?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<Resultado<int>> GuardarAsync(UsuarioDatos datos, CancellationToken ct = default);
}

public partial class UsuarioService : IUsuarioService
{
    private readonly IEstacionDbContext _db;
    private readonly ISesionActual _sesion;

    public UsuarioService(IEstacionDbContext db, ISesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    [GeneratedRegex("^[a-z0-9._]{3,30}$")]
    private static partial Regex FormatoUsuario();

    public async Task<List<UsuarioResumen>> ListarAsync(bool incluirInactivos = false, CancellationToken ct = default)
    {
        var q = _db.Usuarios.AsNoTracking()
            .Include(u => u.Unidades).ThenInclude(x => x.UnidadNegocio)
            .AsQueryable();
        if (!incluirInactivos) q = q.Where(u => u.Activo);

        var lista = await q.OrderBy(u => u.NombreUsuario).ToListAsync(ct);
        return lista.Select(u => new UsuarioResumen(
            u.Id,
            u.NombreUsuario,
            u.NombreCompleto,
            u.Rol.Texto(),
            u.Rol == Rol.Administrador
                ? "Todas"
                : string.Join(", ", u.Unidades.OrderBy(x => x.UnidadNegocioId).Select(x => x.UnidadNegocio!.Nombre)),
            u.UltimoAcceso?.ToLocalTime(),
            u.Activo)).ToList();
    }

    public async Task<UsuarioDatos?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var u = await _db.Usuarios.AsNoTracking().Include(x => x.Unidades).FirstOrDefaultAsync(x => x.Id == id, ct);
        return u is null ? null : new UsuarioDatos
        {
            Id = u.Id,
            NombreUsuario = u.NombreUsuario,
            NombreCompleto = u.NombreCompleto,
            Rol = u.Rol,
            Activo = u.Activo,
            UnidadesNegocioIds = u.Unidades.Select(x => x.UnidadNegocioId).ToList()
        };
    }

    public async Task<Resultado<int>> GuardarAsync(UsuarioDatos d, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.GestionarUsuarios))
            return Resultado<int>.Error("No tenés permiso para gestionar usuarios.");

        d.NombreUsuario = (d.NombreUsuario ?? string.Empty).Trim().ToLower();
        d.NombreCompleto = (d.NombreCompleto ?? string.Empty).Trim();
        d.NuevaPassword = string.IsNullOrEmpty(d.NuevaPassword) ? null : d.NuevaPassword;
        d.UnidadesNegocioIds = d.UnidadesNegocioIds.Distinct().ToList();

        var errores = new List<string>();
        if (!FormatoUsuario().IsMatch(d.NombreUsuario))
            errores.Add("El usuario debe tener entre 3 y 30 caracteres: letras sin acentos, números, punto o guion bajo.");
        if (d.NombreCompleto.Length == 0)
            errores.Add("El nombre completo es obligatorio.");
        if (d.Id == 0 && d.NuevaPassword is null)
            errores.Add("Para un usuario nuevo tenés que indicar una contraseña inicial.");
        if (d.NuevaPassword is not null && AuthService.ValidarNueva(d.NuevaPassword) is { } errorPass)
            errores.Add(errorPass);
        if (d.Rol != Rol.Administrador && d.UnidadesNegocioIds.Count == 0)
            errores.Add("Asigná al menos una unidad de negocio.");

        if (await _db.Usuarios.AnyAsync(u => u.Id != d.Id && u.NombreUsuario == d.NombreUsuario, ct))
            errores.Add($"Ya existe el usuario \"{d.NombreUsuario}\".");

        Usuario usuario;
        if (d.Id == 0)
        {
            usuario = new Usuario();
        }
        else
        {
            usuario = await _db.Usuarios.Include(u => u.Unidades).FirstOrDefaultAsync(u => u.Id == d.Id, ct)
                      ?? throw new InvalidOperationException($"No existe el usuario {d.Id}.");

            if (d.Id == _sesion.Usuario?.Id && !d.Activo)
                errores.Add("No podés desactivar tu propio usuario.");

            // Siempre tiene que quedar al menos un administrador activo.
            var dejaDeSerAdminActivo = usuario.Rol == Rol.Administrador && usuario.Activo
                                       && (d.Rol != Rol.Administrador || !d.Activo);
            if (dejaDeSerAdminActivo &&
                !await _db.Usuarios.AnyAsync(u => u.Id != d.Id && u.Rol == Rol.Administrador && u.Activo, ct))
                errores.Add("Tiene que quedar al menos un administrador activo.");
        }

        if (errores.Count > 0)
            return Resultado<int>.Error(errores.ToArray());

        if (d.Id == 0)
            _db.Usuarios.Add(usuario);

        usuario.NombreUsuario = d.NombreUsuario;
        usuario.NombreCompleto = d.NombreCompleto;
        usuario.Rol = d.Rol;
        usuario.Activo = d.Activo;
        if (d.Id != 0) usuario.ModificadoEn = DateTime.UtcNow;

        if (d.NuevaPassword is not null)
        {
            usuario.PasswordHash = PasswordHasher.Hashear(d.NuevaPassword);
            // Contraseña puesta por el administrador: el usuario la cambia al entrar.
            usuario.DebeCambiarPassword = true;
        }

        // Sincroniza unidades asignadas (el administrador no necesita, accede a todas).
        var nuevas = d.Rol == Rol.Administrador ? new List<int>() : d.UnidadesNegocioIds;
        foreach (var quitar in usuario.Unidades.Where(x => !nuevas.Contains(x.UnidadNegocioId)).ToList())
            usuario.Unidades.Remove(quitar);
        foreach (var agregar in nuevas.Where(id => usuario.Unidades.All(x => x.UnidadNegocioId != id)))
            usuario.Unidades.Add(new UsuarioUnidadNegocio { UnidadNegocioId = agregar });

        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(usuario.Id);
    }
}
