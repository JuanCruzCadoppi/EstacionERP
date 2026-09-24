using CommunityToolkit.Mvvm.ComponentModel;
using EstacionERP.Application.Productos;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Desktop.ViewModels;

public partial class ProductoEditorViewModel : ObservableObject
{
    public int Id { get; }
    public bool EsNuevo => Id == 0;
    public string TituloFormulario => EsNuevo ? "Nuevo producto" : "Editar producto";

    /// <summary>Si el usuario no puede cambiar precios, los campos de precio quedan bloqueados.</summary>
    public bool PuedeModificarPrecios { get; }

    [ObservableProperty] private string _codigo;
    [ObservableProperty] private string? _codigoBarras;
    [ObservableProperty] private string? _codigoFabricante;
    [ObservableProperty] private string _descripcion;
    [ObservableProperty] private string? _marca;
    [ObservableProperty] private string? _rubro;
    [ObservableProperty] private int _unidadNegocioId;
    [ObservableProperty] private TipoProducto _tipo;
    [ObservableProperty] private UnidadMedida _unidadMedida;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioNetoTexto), nameof(MargenTexto))]
    private AlicuotaIva _alicuotaIva;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MargenTexto))]
    private decimal _costo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrecioNetoTexto), nameof(MargenTexto))]
    private decimal _precioVenta;

    [ObservableProperty] private bool _controlaStock;
    [ObservableProperty] private decimal _stockMinimo;
    [ObservableProperty] private bool _activo;
    [ObservableProperty] private string? _errores;

    public bool StockHabilitado => Tipo == TipoProducto.Bien;
    public bool UnidadMedidaHabilitada => Tipo == TipoProducto.Bien;

    public string PrecioNetoTexto =>
        (PrecioVenta / (1 + AlicuotaIva.Porcentaje() / 100m)).ToString("C2");

    public string MargenTexto => ProductoService.Margen(Costo, PrecioVenta, AlicuotaIva) is { } m
        ? $"{m:N1} %"
        : "-";

    public ProductoEditorViewModel(ProductoDatos d, bool puedeModificarPrecios)
    {
        Id = d.Id;
        PuedeModificarPrecios = puedeModificarPrecios || d.Id == 0;
        _codigo = d.Codigo;
        _codigoBarras = d.CodigoBarras;
        _codigoFabricante = d.CodigoFabricante;
        _descripcion = d.Descripcion;
        _marca = d.Marca;
        _rubro = d.Rubro;
        _unidadNegocioId = d.UnidadNegocioId;
        _tipo = d.Tipo;
        _unidadMedida = d.UnidadMedida;
        _alicuotaIva = d.AlicuotaIva;
        _costo = d.Costo;
        _precioVenta = d.PrecioVenta;
        _controlaStock = d.ControlaStock;
        _stockMinimo = d.StockMinimo;
        _activo = d.Activo;
    }

    partial void OnTipoChanged(TipoProducto value)
    {
        OnPropertyChanged(nameof(StockHabilitado));
        OnPropertyChanged(nameof(UnidadMedidaHabilitada));
        switch (value)
        {
            case TipoProducto.Servicio:
                ControlaStock = false;
                StockMinimo = 0;
                UnidadMedida = UnidadMedida.Unidad;
                break;
            case TipoProducto.Combustible:
                ControlaStock = true;
                UnidadMedida = UnidadMedida.Litro;
                UnidadNegocioId = Domain.Entidades.UnidadNegocio.PlayaId;
                break;
            default:
                ControlaStock = true;
                break;
        }
    }

    public ProductoDatos ADatos() => new()
    {
        Id = Id, Codigo = Codigo, CodigoBarras = CodigoBarras, CodigoFabricante = CodigoFabricante,
        Descripcion = Descripcion, Marca = Marca, Rubro = Rubro, UnidadNegocioId = UnidadNegocioId,
        Tipo = Tipo, UnidadMedida = UnidadMedida, AlicuotaIva = AlicuotaIva, Costo = Costo,
        PrecioVenta = PrecioVenta, ControlaStock = ControlaStock, StockMinimo = StockMinimo, Activo = Activo
    };
}
