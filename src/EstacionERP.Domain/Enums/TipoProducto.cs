namespace EstacionERP.Domain.Enums;

public enum TipoProducto
{
    /// <summary>Artículo físico con stock (repuesto, lubricante, shop).</summary>
    Bien = 1,
    /// <summary>Servicio sin stock (lavado, mano de obra).</summary>
    Servicio = 2,
    /// <summary>Combustible, vendido por litro desde tanque.</summary>
    Combustible = 3
}
