using CommunityToolkit.Mvvm.ComponentModel;
using EstacionERP.Application.Clientes;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Desktop.ViewModels;

/// <summary>
/// Formulario de alta / edición de un cliente.
/// </summary>
public partial class ClienteEditorViewModel : ObservableObject
{
    public int Id { get; }
    public bool EsNuevo => Id == 0;
    public string TituloFormulario => EsNuevo ? "Nuevo cliente" : "Editar cliente";

    [ObservableProperty] private TipoDocumento _tipoDocumento;
    [ObservableProperty] private string _numeroDocumento = string.Empty;
    [ObservableProperty] private string _razonSocial = string.Empty;
    [ObservableProperty] private string? _nombreFantasia;
    [ObservableProperty] private CondicionIva _condicionIva;
    [ObservableProperty] private string? _domicilio;
    [ObservableProperty] private string? _localidad;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private bool _tieneCuentaCorriente;
    [ObservableProperty] private decimal _limiteCredito;
    [ObservableProperty] private string? _observaciones;
    [ObservableProperty] private bool _activo;

    [ObservableProperty] private string? _errores;

    public bool DocumentoHabilitado => TipoDocumento != TipoDocumento.SinIdentificar;

    public ClienteEditorViewModel(ClienteDatos d)
    {
        Id = d.Id;
        _tipoDocumento = d.TipoDocumento;
        _numeroDocumento = d.NumeroDocumento;
        _razonSocial = d.RazonSocial;
        _nombreFantasia = d.NombreFantasia;
        _condicionIva = d.CondicionIva;
        _domicilio = d.Domicilio;
        _localidad = d.Localidad ?? (EsNuevo ? "Laguna Larga" : null);
        _telefono = d.Telefono;
        _email = d.Email;
        _tieneCuentaCorriente = d.TieneCuentaCorriente;
        _limiteCredito = d.LimiteCredito;
        _observaciones = d.Observaciones;
        _activo = d.Activo;
    }

    partial void OnTipoDocumentoChanged(TipoDocumento value)
    {
        OnPropertyChanged(nameof(DocumentoHabilitado));
        if (value == TipoDocumento.SinIdentificar)
        {
            NumeroDocumento = string.Empty;
            CondicionIva = CondicionIva.ConsumidorFinal;
        }
    }

    public ClienteDatos ADatos() => new()
    {
        Id = Id,
        TipoDocumento = TipoDocumento,
        NumeroDocumento = NumeroDocumento,
        RazonSocial = RazonSocial,
        NombreFantasia = NombreFantasia,
        CondicionIva = CondicionIva,
        Domicilio = Domicilio,
        Localidad = Localidad,
        Telefono = Telefono,
        Email = Email,
        TieneCuentaCorriente = TieneCuentaCorriente,
        LimiteCredito = LimiteCredito,
        Observaciones = Observaciones,
        Activo = Activo
    };
}
