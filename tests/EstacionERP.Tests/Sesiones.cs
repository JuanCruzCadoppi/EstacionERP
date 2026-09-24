using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Tests;

/// <summary>Sesiones armadas para los tests.</summary>
public static class Sesiones
{
    private static readonly int[] Todas = { UnidadNegocio.PlayaId, UnidadNegocio.RepuestosId, UnidadNegocio.LavaderoId };

    public static ISesionActual Admin() =>
        Crear(new UsuarioSesion(Usuario.AdministradorInicialId, "admin", "Administrador", Rol.Administrador, Todas, false));

    public static ISesionActual Encargado(params int[] unidades) =>
        Crear(new UsuarioSesion(50, "encargado", "Encargado", Rol.Encargado, unidades, false));

    public static ISesionActual Operador(params int[] unidades) =>
        Crear(new UsuarioSesion(60, "operador", "Operador", Rol.Operador, unidades, false));

    private static ISesionActual Crear(UsuarioSesion u)
    {
        var s = new SesionActual();
        s.Iniciar(u);
        return s;
    }
}
