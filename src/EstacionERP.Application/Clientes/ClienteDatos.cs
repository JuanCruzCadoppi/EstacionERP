using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Clientes;

/// <summary>
/// Datos que se cargan en el formulario de cliente (alta y modificación).
/// </summary>
public class ClienteDatos
{
    public int Id { get; set; }
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Dni;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreFantasia { get; set; }
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ConsumidorFinal;
    public string? Domicilio { get; set; }
    public string? Localidad { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public bool TieneCuentaCorriente { get; set; }
    public decimal LimiteCredito { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Fila liviana para mostrar en la grilla de clientes.
/// </summary>
public record ClienteResumen(
    int Id,
    string Documento,
    string RazonSocial,
    string CondicionIva,
    string? Localidad,
    string? Telefono,
    bool TieneCuentaCorriente,
    decimal LimiteCredito,
    bool Activo);
