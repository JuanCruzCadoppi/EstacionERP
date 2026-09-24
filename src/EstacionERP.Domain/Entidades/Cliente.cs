using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Cliente único para toda la estación. La cuenta corriente se abre por unidad de negocio,
/// pero el límite de crédito es global.
/// </summary>
public class Cliente : Entidad
{
    /// <summary>Id del cliente genérico "Consumidor Final" (creado por defecto).</summary>
    public const int ConsumidorFinalId = 1;

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Dni;

    /// <summary>Solo dígitos, sin guiones.</summary>
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

    public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
}
