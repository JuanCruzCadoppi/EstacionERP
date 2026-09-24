using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Seguridad;

/// <summary>Acciones sensibles que se controlan por rol.</summary>
public enum Permiso
{
    EditarClientes,
    DesactivarClientes,
    EditarProductos,
    ModificarPrecios,
    GestionarUsuarios,
    Facturar,
    ConfigurarFacturacion
}

public static class Permisos
{
    /// <summary>Matriz rol → permisos. Cambiando esto se cambia la seguridad de todo el sistema.</summary>
    public static bool Tiene(Rol rol, Permiso permiso) => rol switch
    {
        Rol.Administrador => true,
        Rol.Encargado => permiso is Permiso.EditarClientes or Permiso.DesactivarClientes
                                 or Permiso.EditarProductos or Permiso.ModificarPrecios or Permiso.Facturar,
        Rol.Operador => permiso is Permiso.EditarClientes or Permiso.Facturar,
        _ => false
    };
}
