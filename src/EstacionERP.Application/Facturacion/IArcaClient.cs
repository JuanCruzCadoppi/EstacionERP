using EstacionERP.Domain.Enums;

namespace EstacionERP.Application.Facturacion;

public record AlicuotaCae(AlicuotaIva Alicuota, decimal BaseImponible, decimal Importe);

public record ComprobanteAsociadoCae(TipoComprobante Tipo, int PuntoVenta, long Numero, string CuitEmisor, DateTime Fecha);

/// <summary>Datos que se envían a ARCA para pedir el CAE (FECAESolicitar).</summary>
public record SolicitudCae(
    int PuntoVenta,
    TipoComprobante Tipo,
    long Numero,
    ConceptoComprobante Concepto,
    TipoDocumento DocTipo,
    long DocNro,
    CondicionIva CondicionIvaReceptor,
    DateTime Fecha,
    decimal ImporteTotal,
    decimal ImporteNoGravado,
    decimal ImporteNeto,
    decimal ImporteExento,
    decimal ImporteTributos,
    decimal ImporteIva,
    DateTime? FechaServicioDesde,
    DateTime? FechaServicioHasta,
    DateTime? FechaVencimientoPago,
    IReadOnlyList<AlicuotaCae> Alicuotas,
    ComprobanteAsociadoCae? Asociado = null);

public record MensajeArca(int Codigo, string Mensaje)
{
    public override string ToString() => $"[{Codigo}] {Mensaje}";
}

public record RespuestaCae(
    bool Aprobado,
    string? Cae,
    DateTime? CaeVencimiento,
    IReadOnlyList<MensajeArca> Observaciones,
    IReadOnlyList<MensajeArca> Errores);

public record ComprobanteConsultado(
    long Numero,
    string? Cae,
    DateTime? CaeVencimiento,
    decimal ImporteTotal,
    long DocNro,
    DateTime Fecha);

public record EstadoServidoresArca(string AppServer, string DbServer, string AuthServer)
{
    public bool TodoOk => AppServer == "OK" && DbServer == "OK" && AuthServer == "OK";
}

/// <summary>
/// Acceso a los web services de ARCA (WSAA + WSFEv1). La implementación está en Infrastructure.
/// Lanza <see cref="ArcaComunicacionException"/> si no hay conexión y <see cref="ArcaException"/>
/// ante errores de configuración o autenticación.
/// </summary>
public interface IArcaClient
{
    Task<EstadoServidoresArca> VerificarServidoresAsync(CancellationToken ct = default);
    Task ProbarAutenticacionAsync(CancellationToken ct = default);
    Task<long> UltimoAutorizadoAsync(int puntoVenta, TipoComprobante tipo, CancellationToken ct = default);
    Task<RespuestaCae> SolicitarCaeAsync(SolicitudCae solicitud, CancellationToken ct = default);
    Task<ComprobanteConsultado?> ConsultarAsync(int puntoVenta, TipoComprobante tipo, long numero, CancellationToken ct = default);
}

/// <summary>Error informado por ARCA o de configuración (certificado, CUIT, permisos del servicio).</summary>
public class ArcaException : Exception
{
    public ArcaException(string mensaje, Exception? interna = null) : base(mensaje, interna) { }
}

/// <summary>No se pudo hablar con ARCA (sin internet, timeout, servidor caído).</summary>
public class ArcaComunicacionException : ArcaException
{
    public ArcaComunicacionException(string mensaje, Exception? interna = null) : base(mensaje, interna) { }
}
