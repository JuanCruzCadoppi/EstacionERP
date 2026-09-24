using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

public class Usuario : Entidad
{
    public const int AdministradorInicialId = 1;

    /// <summary>Nombre de ingreso, siempre en minúsculas.</summary>
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la contraseña. Nunca se guarda la contraseña en texto plano.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public Rol Rol { get; set; } = Rol.Operador;

    /// <summary>Obliga a cambiar la contraseña en el próximo ingreso.</summary>
    public bool DebeCambiarPassword { get; set; } = true;

    public DateTime? UltimoAcceso { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Unidades de negocio en las que puede operar (el administrador tiene todas).</summary>
    public ICollection<UsuarioUnidadNegocio> Unidades { get; set; } = new List<UsuarioUnidadNegocio>();
}

/// <summary>Tabla intermedia usuario ↔ unidad de negocio.</summary>
public class UsuarioUnidadNegocio
{
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int UnidadNegocioId { get; set; }
    public UnidadNegocio? UnidadNegocio { get; set; }
}
