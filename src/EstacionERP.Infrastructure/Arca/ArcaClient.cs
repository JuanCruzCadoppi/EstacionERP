using System.Globalization;
using System.Net.Http;
using System.Xml.Linq;
using EstacionERP.Application.Common;
using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Infrastructure.Arca;

/// <summary>
/// Cliente del WSFEv1 (factura electrónica, RG 4291). Arma el XML a mano con el orden exacto
/// de campos que exige el WSDL de ARCA.
/// </summary>
public class ArcaClient : IArcaClient
{
    public const string Servicio = "wsfe";
    private static readonly XNamespace Ns = "http://ar.gov.afip.dif.FEV1/";

    /// <summary>600: token/sign inválido o vencido. 601: CUIT no incluida en el token.</summary>
    private static readonly int[] ErroresAutenticacion = { 600, 601 };
    /// <summary>602: "No existen datos en nuestros registros para los parámetros ingresados".</summary>
    private const int ErrorSinDatos = 602;

    private readonly HttpClient _http;
    private readonly IEstacionDbContext _db;
    private readonly WsaaClient _wsaa;

    public ArcaClient(HttpClient http, IEstacionDbContext db, WsaaClient wsaa)
    {
        _http = http;
        _db = db;
        _wsaa = wsaa;
    }

    private async Task<EntornoArca> EntornoAsync(CancellationToken ct) =>
        (await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct))?.Entorno ?? EntornoArca.Homologacion;

    // ------------------------------------------------------------------ Métodos públicos

    public async Task<EstadoServidoresArca> VerificarServidoresAsync(CancellationToken ct = default)
    {
        var doc = await Soap.EnviarAsync(_http, ArcaEndpoints.Wsfe(await EntornoAsync(ct)), Accion("FEDummy"),
            Soap.Sobre(new XElement(Ns + "FEDummy")), ct);
        var r = doc.Descendants(Ns + "FEDummyResult").First();
        return new EstadoServidoresArca(
            r.Element(Ns + "AppServer")?.Value ?? "?",
            r.Element(Ns + "DbServer")?.Value ?? "?",
            r.Element(Ns + "AuthServer")?.Value ?? "?");
    }

    public async Task ProbarAutenticacionAsync(CancellationToken ct = default) =>
        await _wsaa.ObtenerCredencialesAsync(Servicio, ct: ct);

    public async Task<long> UltimoAutorizadoAsync(int puntoVenta, TipoComprobante tipo, CancellationToken ct = default)
    {
        var resultado = await LlamarAsync("FECompUltimoAutorizado", auth => new XElement(Ns + "FECompUltimoAutorizado",
            auth,
            new XElement(Ns + "PtoVta", puntoVenta),
            new XElement(Ns + "CbteTipo", (int)tipo)), ct);

        var errores = LeerMensajes(resultado, "Errors", "Err");
        if (errores.Count > 0)
            throw new ArcaException(string.Join(" / ", errores));

        return long.Parse(resultado.Element(Ns + "CbteNro")!.Value, CultureInfo.InvariantCulture);
    }

    public async Task<RespuestaCae> SolicitarCaeAsync(SolicitudCae s, CancellationToken ct = default)
    {
        var resultado = await LlamarAsync("FECAESolicitar", auth => new XElement(Ns + "FECAESolicitar",
            auth, ArmarFeCaeReq(s)), ct);

        var errores = LeerMensajes(resultado, "Errors", "Err");
        var det = resultado.Descendants(Ns + "FECAEDetResponse").FirstOrDefault();
        var observaciones = det is null ? new List<MensajeArca>() : LeerMensajes(det, "Observaciones", "Obs");

        var aprobado = det?.Element(Ns + "Resultado")?.Value == "A";
        var cae = det?.Element(Ns + "CAE")?.Value;
        var vto = FechaArca(det?.Element(Ns + "CAEFchVto")?.Value);

        return new RespuestaCae(aprobado && !string.IsNullOrWhiteSpace(cae), string.IsNullOrWhiteSpace(cae) ? null : cae,
            vto, observaciones, errores);
    }

    public async Task<ComprobanteConsultado?> ConsultarAsync(int puntoVenta, TipoComprobante tipo, long numero, CancellationToken ct = default)
    {
        var resultado = await LlamarAsync("FECompConsultar", auth => new XElement(Ns + "FECompConsultar",
            auth,
            new XElement(Ns + "FeCompConsReq",
                new XElement(Ns + "CbteTipo", (int)tipo),
                new XElement(Ns + "CbteNro", numero),
                new XElement(Ns + "PtoVta", puntoVenta))), ct);

        var errores = LeerMensajes(resultado, "Errors", "Err");
        if (errores.Any(e => e.Codigo == ErrorSinDatos)) return null;
        if (errores.Count > 0) throw new ArcaException(string.Join(" / ", errores));

        var r = resultado.Element(Ns + "ResultGet");
        if (r is null) return null;

        return new ComprobanteConsultado(
            numero,
            r.Element(Ns + "CodAutorizacion")?.Value,
            FechaArca(r.Element(Ns + "FchVto")?.Value),
            decimal.Parse(r.Element(Ns + "ImpTotal")?.Value ?? "0", CultureInfo.InvariantCulture),
            long.Parse(r.Element(Ns + "DocNro")?.Value ?? "0", CultureInfo.InvariantCulture),
            FechaArca(r.Element(Ns + "CbteFch")?.Value) ?? DateTime.MinValue);
    }

    // ------------------------------------------------------------------ Armado del XML

    /// <summary>Arma FeCAEReq respetando el orden del WSDL (si el orden cambia, ARCA rechaza).</summary>
    public static XElement ArmarFeCaeReq(SolicitudCae s)
    {
        var detalle = new XElement(Ns + "FECAEDetRequest",
            new XElement(Ns + "Concepto", (int)s.Concepto),
            new XElement(Ns + "DocTipo", (int)s.DocTipo),
            new XElement(Ns + "DocNro", s.DocNro),
            new XElement(Ns + "CbteDesde", s.Numero),
            new XElement(Ns + "CbteHasta", s.Numero),
            new XElement(Ns + "CbteFch", Fecha(s.Fecha)),
            new XElement(Ns + "ImpTotal", Importe(s.ImporteTotal)),
            new XElement(Ns + "ImpTotConc", Importe(s.ImporteNoGravado)),
            new XElement(Ns + "ImpNeto", Importe(s.ImporteNeto)),
            new XElement(Ns + "ImpOpEx", Importe(s.ImporteExento)),
            new XElement(Ns + "ImpTrib", Importe(s.ImporteTributos)),
            new XElement(Ns + "ImpIVA", Importe(s.ImporteIva)));

        if (s.Concepto != ConceptoComprobante.Productos)
        {
            detalle.Add(
                new XElement(Ns + "FchServDesde", Fecha(s.FechaServicioDesde ?? s.Fecha)),
                new XElement(Ns + "FchServHasta", Fecha(s.FechaServicioHasta ?? s.Fecha)),
                new XElement(Ns + "FchVtoPago", Fecha(s.FechaVencimientoPago ?? s.Fecha)));
        }

        detalle.Add(
            new XElement(Ns + "MonId", "PES"),
            new XElement(Ns + "MonCotiz", "1"),
            new XElement(Ns + "CondicionIVAReceptorId", (int)s.CondicionIvaReceptor));

        if (s.Asociado is { } a)
        {
            detalle.Add(new XElement(Ns + "CbtesAsoc",
                new XElement(Ns + "CbteAsoc",
                    new XElement(Ns + "Tipo", (int)a.Tipo),
                    new XElement(Ns + "PtoVta", a.PuntoVenta),
                    new XElement(Ns + "Nro", a.Numero),
                    new XElement(Ns + "Cuit", a.CuitEmisor),
                    new XElement(Ns + "CbteFch", Fecha(a.Fecha)))));
        }

        if (s.Alicuotas.Count > 0)
        {
            detalle.Add(new XElement(Ns + "Iva",
                s.Alicuotas.Select(al => new XElement(Ns + "AlicIva",
                    new XElement(Ns + "Id", (int)al.Alicuota),
                    new XElement(Ns + "BaseImp", Importe(al.BaseImponible)),
                    new XElement(Ns + "Importe", Importe(al.Importe))))));
        }

        return new XElement(Ns + "FeCAEReq",
            new XElement(Ns + "FeCabReq",
                new XElement(Ns + "CantReg", 1),
                new XElement(Ns + "PtoVta", s.PuntoVenta),
                new XElement(Ns + "CbteTipo", (int)s.Tipo)),
            new XElement(Ns + "FeDetReq", detalle));
    }

    // ------------------------------------------------------------------ Infraestructura

    /// <summary>
    /// Llama a un método del WSFE con credenciales. Si ARCA dice que el token venció (error 600),
    /// pide uno nuevo y reintenta una vez.
    /// </summary>
    private async Task<XElement> LlamarAsync(string metodo, Func<XElement, XElement> armar, CancellationToken ct)
    {
        var entorno = await EntornoAsync(ct);
        for (var intento = 1; ; intento++)
        {
            var cred = await _wsaa.ObtenerCredencialesAsync(Servicio, forzarNuevo: intento > 1, ct);
            var auth = new XElement(Ns + "Auth",
                new XElement(Ns + "Token", cred.Token),
                new XElement(Ns + "Sign", cred.Sign),
                new XElement(Ns + "Cuit", cred.Cuit));

            XDocument doc;
            try
            {
                doc = await Soap.EnviarAsync(_http, ArcaEndpoints.Wsfe(entorno), Accion(metodo), Soap.Sobre(armar(auth)), ct);
            }
            catch (ArcaSoapFaultException ex)
            {
                throw new ArcaException(ex.Message, ex);
            }

            var resultado = doc.Descendants(Ns + (metodo + "Result")).FirstOrDefault()
                            ?? throw new ArcaException($"respuesta inesperada de ARCA en {metodo}.");

            var errores = LeerMensajes(resultado, "Errors", "Err");
            var errorAuth = errores.FirstOrDefault(e => ErroresAutenticacion.Contains(e.Codigo));
            if (errorAuth is null) return resultado;
            if (intento >= 2)
                throw new ArcaException("ARCA rechazó las credenciales: " + errorAuth.Mensaje +
                                        ". Revisá que el certificado tenga autorizado el servicio wsfe para este CUIT.");
        }
    }

    private static List<MensajeArca> LeerMensajes(XElement padre, string contenedor, string elemento) =>
        padre.Elements(Ns + contenedor).Elements(Ns + elemento)
            .Select(e => new MensajeArca(
                int.TryParse(e.Element(Ns + "Code")?.Value, out var c) ? c : 0,
                e.Element(Ns + "Msg")?.Value ?? string.Empty))
            .ToList();

    /// <summary>SOAPAction de cada método: http://ar.gov.afip.dif.FEV1/Metodo</summary>
    private static string Accion(string metodo) => Ns.NamespaceName + metodo;

    private static string Importe(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Fecha(DateTime d) => d.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private static DateTime? FechaArca(string? valor) =>
        DateTime.TryParseExact(valor, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
