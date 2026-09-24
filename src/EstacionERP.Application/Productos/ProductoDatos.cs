using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Productos;

public class ProductoDatos
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string? CodigoFabricante { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Marca { get; set; }
    public string? Rubro { get; set; }
    public int UnidadNegocioId { get; set; }
    public TipoProducto Tipo { get; set; } = TipoProducto.Bien;
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Unidad;
    public AlicuotaIva AlicuotaIva { get; set; } = AlicuotaIva.Veintiuno;
    public decimal Costo { get; set; }
    public decimal PrecioVenta { get; set; }
    public bool ControlaStock { get; set; } = true;
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; } = true;
}

public record ProductoResumen(
    int Id,
    string Codigo,
    string Descripcion,
    int UnidadNegocioId,
    string UnidadNegocio,
    string Tipo,
    string? Marca,
    string? Rubro,
    string AlicuotaIva,
    decimal Costo,
    decimal PrecioVenta,
    decimal? MargenPorcentaje,
    bool Activo);

/// <summary>Filtro para la actualización masiva de precios.</summary>
public class FiltroAumento
{
    public int? UnidadNegocioId { get; set; }
    public string? Rubro { get; set; }
    public string? Marca { get; set; }
}
