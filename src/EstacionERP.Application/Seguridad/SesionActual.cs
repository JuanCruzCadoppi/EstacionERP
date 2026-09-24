using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Seguridad;

/// <summary>Datos del usuario que inició sesión.</summary>
public record UsuarioSesion(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    Rol Rol,
    IReadOnlyList<int> UnidadesNegocioIds,
    bool DebeCambiarPassword);

public interface ISesionActual
{
    UsuarioSesion? Usuario { get; }
    bool Iniciada { get; }
    void Iniciar(UsuarioSesion usuario);
    void Cerrar();
    void MarcarPasswordCambiada();

    /// <summary>¿El usuario tiene este permiso según su rol?</summary>
    bool Puede(Permiso permiso);

    /// <summary>¿El usuario puede operar en esta unidad de negocio?</summary>
    bool TieneAcceso(int unidadNegocioId);
}

/// <summary>
/// Sesión en memoria (una por programa abierto). Se registra como singleton.
/// </summary>
public class SesionActual : ISesionActual
{
    public UsuarioSesion? Usuario { get; private set; }
    public bool Iniciada => Usuario is not null;

    public void Iniciar(UsuarioSesion usuario) => Usuario = usuario;
    public void Cerrar() => Usuario = null;

    public void MarcarPasswordCambiada()
    {
        if (Usuario is not null)
            Usuario = Usuario with { DebeCambiarPassword = false };
    }

    public bool Puede(Permiso permiso) => Usuario is not null && Permisos.Tiene(Usuario.Rol, permiso);

    public bool TieneAcceso(int unidadNegocioId) =>
        Usuario is not null &&
        (Usuario.Rol == Rol.Administrador || Usuario.UnidadesNegocioIds.Contains(unidadNegocioId));
}
