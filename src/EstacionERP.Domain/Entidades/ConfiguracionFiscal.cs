using EstacionERP.Domain.Common;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Entidades;

/// <summary>
/// Datos fiscales del emisor (la estación) y credenciales para los web services de ARCA.
/// Hay un solo registro (Id = 1).
/// </summary>
public class ConfiguracionFiscal : Entidad
{
    public const int IdUnico = 1;

    public string Cuit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreFantasia { get; set; }
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ResponsableInscripto;
    public string? Domicilio { get; set; }
    public string? Localidad { get; set; }
    public string? IngresosBrutos { get; set; }
    public DateTime? InicioActividades { get; set; }

    public EntornoArca Entorno { get; set; } = EntornoArca.Homologacion;

    /// <summary>Alias con el que se registró el certificado en ARCA.</summary>
    public string? AliasCertificado { get; set; }

    /// <summary>Clave privada RSA (PEM) del certificado vigente.</summary>
    public string? ClavePrivadaPem { get; set; }

    /// <summary>Clave generada con la última solicitud, esperando que se importe el certificado.</summary>
    public string? ClavePendientePem { get; set; }

    /// <summary>Última solicitud de certificado (CSR) generada para subir a ARCA.</summary>
    public string? SolicitudCertificadoPem { get; set; }

    /// <summary>Certificado emitido por ARCA (PEM).</summary>
    public string? CertificadoPem { get; set; }

    public DateTime? CertificadoVence { get; set; }

    public bool TieneCertificado => !string.IsNullOrEmpty(CertificadoPem) && !string.IsNullOrEmpty(ClavePrivadaPem);
}
