namespace EstacionERP.Domain.Enums;

/// <summary>
/// Rol del usuario. Define QUÉ puede hacer; las unidades de negocio asignadas definen DÓNDE.
/// </summary>
public enum Rol
{
    /// <summary>Dueño / administrador: todo, en todas las unidades, incluida la gestión de usuarios.</summary>
    Administrador = 1,
    /// <summary>Encargado: gestiona productos, precios y clientes de sus unidades.</summary>
    Encargado = 2,
    /// <summary>Operador (playero, vendedor, lavador): vende y carga clientes en sus unidades.</summary>
    Operador = 3
}
