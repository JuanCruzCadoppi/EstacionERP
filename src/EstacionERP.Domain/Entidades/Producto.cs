using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Producto o servicio vendible. Pertenece a una unidad de negocio.
/// El stock NO se guarda acá: se calcula desde los movimientos de stock (etapa siguiente).
/// </summary>
public class Producto : Entidad
{
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? CodigoFabricante { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Rubro { get; set; }

    public int UnidadNegocioId { get; set; }
    public UnidadNegocio? UnidadNegocio { get; set; }

    public TipoProducto Tipo { get; set; } = TipoProducto.Bien;
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Unidad;
    public AlicuotaIva AlicuotaIva { get; set; } = AlicuotaIva.Veintiuno;

    /// <summary>Costo de compra sin IVA.</summary>
    public decimal Costo { get; set; }

    /// <summary>Precio de venta final, con IVA incluido.</summary>
    public decimal PrecioVenta { get; set; }

    public bool ControlaStock { get; set; } = true;
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Precio sin IVA calculado a partir del precio final.</summary>
    public decimal PrecioNeto => Math.Round(PrecioVenta / (1 + AlicuotaIva.Porcentaje() / 100m), 2);
}
