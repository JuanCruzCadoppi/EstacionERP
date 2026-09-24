using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using EstacionERP.Application.Configuracion;
using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Infrastructure.Arca;

namespace EstacionERP.Tests;

/// <summary>Simula lo que hace ARCA: firma un certificado a partir del CSR.</summary>
public static class ArcaCertificadoSimulado
{
    public static string EmitirDesdeCsr(string csrPem, string clavePem)
    {
        var req = CertificateRequest.LoadSigningRequestPem(csrPem, HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.Default, RSASignaturePadding.Pkcs1);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(clavePem);
        var nuevo = new CertificateRequest(req.SubjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = nuevo.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(2));
        return cert.ExportCertificatePem();
    }
}

public class CertificadoArcaTests
{
    [Fact]
    public void Solicitud_tiene_el_sujeto_que_pide_arca()
    {
        var s = CertificadoArca.GenerarSolicitud("20123456786", "ESTACION SA", "estacionerp");
        Assert.Contains("BEGIN CERTIFICATE REQUEST", s.CsrPem);
        Assert.Contains("PRIVATE KEY", s.ClavePrivadaPem);

        var req = CertificateRequest.LoadSigningRequestPem(s.CsrPem, HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.Default, RSASignaturePadding.Pkcs1);
        var sujeto = req.SubjectName.Format(false);
        Assert.Contains("C=AR", sujeto);
        Assert.Contains("O=ESTACION SA", sujeto);
        Assert.Contains("CN=estacionerp", sujeto);
        Assert.Contains("CUIT 20123456786", sujeto);
    }

    [Fact]
    public void Certificado_emitido_coincide_con_la_clave_y_trae_el_cuit()
    {
        var s = CertificadoArca.GenerarSolicitud("20123456786", "ESTACION SA", "estacionerp");
        var certPem = ArcaCertificadoSimulado.EmitirDesdeCsr(s.CsrPem, s.ClavePrivadaPem);
        var cert = CertificadoArca.LeerCertificado(certPem);

        Assert.True(CertificadoArca.ClaveCoincide(cert, s.ClavePrivadaPem));
        Assert.Equal("20123456786", cert.Cuit);
        Assert.True(cert.Vence > DateTime.Now.AddYears(1));

        var otra = CertificadoArca.GenerarSolicitud("20123456786", "ESTACION SA", "otro");
        Assert.False(CertificadoArca.ClaveCoincide(cert, otra.ClavePrivadaPem));
    }

    [Fact]
    public void Lee_certificado_guardado_con_bloc_de_notas()
    {
        var s = CertificadoArca.GenerarSolicitud("20123456786", "ESTACION SA", "estacionerp");
        var certPem = ArcaCertificadoSimulado.EmitirDesdeCsr(s.CsrPem, s.ClavePrivadaPem);
        // BOM, saltos de línea de Windows y espacios sobrantes, como queda al copiar de la web.
        var comoWindows = "\uFEFF  " + certPem.Replace("\n", "\r\n") + "\r\n\r\n";
        var cert = CertificadoArca.LeerCertificado(comoWindows);
        Assert.True(CertificadoArca.ClaveCoincide(cert, s.ClavePrivadaPem));
        Assert.StartsWith("-----BEGIN CERTIFICATE-----", cert.Pem);
    }

    [Fact]
    public void Tra_firmado_es_un_cms_valido_con_el_xml_adentro()
    {
        var s = CertificadoArca.GenerarSolicitud("20123456786", "ESTACION SA", "estacionerp");
        var certPem = ArcaCertificadoSimulado.EmitirDesdeCsr(s.CsrPem, s.ClavePrivadaPem);

        var tra = WsaaClient.ArmarTra("wsfe", DateTimeOffset.Now);
        var cmsBase64 = WsaaClient.FirmarTra(tra, certPem, s.ClavePrivadaPem);

        var cms = new SignedCms();
        cms.Decode(Convert.FromBase64String(cmsBase64));
        cms.CheckSignature(verifySignatureOnly: true);
        var contenido = Encoding.UTF8.GetString(cms.ContentInfo.Content);
        Assert.Contains("<service>wsfe</service>", contenido);
        Assert.Contains("loginTicketRequest", contenido);
    }
}

public class ConfiguracionFiscalServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    public void Dispose() => _bd.Dispose();

    private static ConfiguracionFiscalDatos Datos() => new()
    {
        Cuit = "20-12345678-6", RazonSocial = "estación laguna larga", CondicionIva = CondicionIva.ResponsableInscripto
    };

    [Fact]
    public async Task Flujo_completo_de_certificado()
    {
        using var db = _bd.CrearContexto();
        var s = new ConfiguracionFiscalService(db, Sesiones.Admin(), new ArcaFalso());

        Assert.True((await s.GuardarAsync(Datos())).Exito);
        var csr = await s.GenerarSolicitudCertificadoAsync("estacionerp");
        Assert.True(csr.Exito);
        Assert.True((await s.EstadoCertificadoAsync()).TieneSolicitudPendiente);

        var clave = db.ConfiguracionesFiscales.Single().ClavePendientePem!;
        var certPem = ArcaCertificadoSimulado.EmitirDesdeCsr(csr.Valor!, clave);

        var r = await s.ImportarCertificadoAsync(certPem);
        Assert.True(r.Exito, string.Join(", ", r.Errores));

        var estado = await s.EstadoCertificadoAsync();
        Assert.True(estado.TieneCertificado);
        Assert.False(estado.TieneSolicitudPendiente);
        Assert.NotNull(estado.Vence);
    }

    [Fact]
    public async Task Rechaza_certificado_de_otra_clave_o_de_otro_cuit()
    {
        using var db = _bd.CrearContexto();
        var s = new ConfiguracionFiscalService(db, Sesiones.Admin(), new ArcaFalso());
        await s.GuardarAsync(Datos());
        await s.GenerarSolicitudCertificadoAsync("estacionerp");

        var ajena = CertificadoArca.GenerarSolicitud("20123456786", "X", "y");
        var certAjeno = ArcaCertificadoSimulado.EmitirDesdeCsr(ajena.CsrPem, ajena.ClavePrivadaPem);
        Assert.False((await s.ImportarCertificadoAsync(certAjeno)).Exito);

        var otroCuit = CertificadoArca.GenerarSolicitud("30500010912", "X", "y");
        var certOtroCuit = ArcaCertificadoSimulado.EmitirDesdeCsr(otroCuit.CsrPem, otroCuit.ClavePrivadaPem);
        var r = await s.ImportarCertificadoAsync(certOtroCuit);
        Assert.False(r.Exito);
        Assert.Contains("CUIT", r.Errores[0]);

        Assert.False((await s.ImportarCertificadoAsync("cualquier cosa")).Exito);
    }

    [Fact]
    public async Task Solo_el_administrador_configura_y_valida_cuit()
    {
        using var db = _bd.CrearContexto();
        Assert.False((await new ConfiguracionFiscalService(db, Sesiones.Encargado(1), new ArcaFalso()).GuardarAsync(Datos())).Exito);

        var s = new ConfiguracionFiscalService(db, Sesiones.Admin(), new ArcaFalso());
        var mal = Datos(); mal.Cuit = "20-12345678-0";
        Assert.False((await s.GuardarAsync(mal)).Exito);
    }

    [Fact]
    public async Task Puntos_de_venta()
    {
        using var db = _bd.CrearContexto();
        var s = new ConfiguracionFiscalService(db, Sesiones.Admin(), new ArcaFalso());
        var r = await s.GuardarPuntoVentaAsync(new PuntoVentaFila(0, 1, "Playa", UnidadNegocio.PlayaId, "", true));
        Assert.True(r.Exito);
        Assert.False((await s.GuardarPuntoVentaAsync(new PuntoVentaFila(0, 1, "Otra", UnidadNegocio.RepuestosId, "", true))).Exito);
        Assert.False((await s.GuardarPuntoVentaAsync(new PuntoVentaFila(0, 0, "Cero", UnidadNegocio.RepuestosId, "", true))).Exito);
        Assert.Single(await s.ListarPuntosVentaAsync());
    }
}

/// <summary>Servidor HTTP falso que responde como ARCA.</summary>
public class ArcaHttpFalso : HttpMessageHandler
{
    public List<(string Url, string SoapAction, string Cuerpo)> Pedidos { get; } = new();
    public Func<string, string, string> Responder { get; set; } = (_, _) => "";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var cuerpo = await request.Content!.ReadAsStringAsync(ct);
        var accion = request.Headers.TryGetValues("SOAPAction", out var v) ? v.First().Trim('"') : "";
        Pedidos.Add((request.RequestUri!.ToString(), accion, cuerpo));
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Responder(accion, cuerpo), Encoding.UTF8, "text/xml") };
    }
}

public class ArcaClientTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    public void Dispose() => _bd.Dispose();

    private const string Sobre = "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap:Body>{0}</soap:Body></soap:Envelope>";

    private static string LoginResponse() =>
        string.Format(Sobre, "<loginCmsResponse xmlns=\"http://wsaa.view.sua.dvadac.desein.afip.gov\"><loginCmsReturn>" +
            System.Security.SecurityElement.Escape(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?><loginTicketResponse version=\"1.0\"><header><expirationTime>" +
                DateTimeOffset.Now.AddHours(12).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz") +
                "</expirationTime></header><credentials><token>TOKEN123</token><sign>SIGN456</sign></credentials></loginTicketResponse>") +
            "</loginCmsReturn></loginCmsResponse>");

    private (ArcaClient cliente, ArcaHttpFalso http) Crear(Infrastructure.Persistencia.EstacionDbContext db)
    {
        var s = CertificadoArca.GenerarSolicitud("20123456786", "ESTACION SA", "estacionerp");
        db.ConfiguracionesFiscales.Add(new ConfiguracionFiscal
        {
            Id = 1, Cuit = "20123456786", RazonSocial = "ESTACION SA", Entorno = EntornoArca.Homologacion,
            ClavePrivadaPem = s.ClavePrivadaPem, CertificadoPem = ArcaCertificadoSimulado.EmitirDesdeCsr(s.CsrPem, s.ClavePrivadaPem)
        });
        db.SaveChanges();
        var http = new ArcaHttpFalso();
        var wsaa = new WsaaClient(new HttpClient(http), db);
        return (new ArcaClient(new HttpClient(http), db, wsaa), http);
    }

    [Fact]
    public async Task Pide_ticket_una_sola_vez_y_lo_reutiliza()
    {
        using var db = _bd.CrearContexto();
        var (arca, http) = Crear(db);
        http.Responder = (accion, _) => accion == ""
            ? LoginResponse()
            : string.Format(Sobre, "<FECompUltimoAutorizadoResponse xmlns=\"http://ar.gov.afip.dif.FEV1/\"><FECompUltimoAutorizadoResult><PtoVta>2</PtoVta><CbteTipo>6</CbteTipo><CbteNro>41</CbteNro></FECompUltimoAutorizadoResult></FECompUltimoAutorizadoResponse>");

        Assert.Equal(41, await arca.UltimoAutorizadoAsync(2, TipoComprobante.FacturaB));
        Assert.Equal(41, await arca.UltimoAutorizadoAsync(2, TipoComprobante.FacturaB));

        Assert.Single(http.Pedidos, p => p.Url.Contains("wsaahomo"));
        var wsfe = http.Pedidos.Last();
        Assert.Contains("wswhomo.afip.gov.ar/wsfev1", wsfe.Url);
        Assert.Equal("http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado", wsfe.SoapAction);
        Assert.Contains("<Token>TOKEN123</Token>", wsfe.Cuerpo);
        Assert.Contains("<Cuit>20123456786</Cuit>", wsfe.Cuerpo);
        Assert.Single(db.TicketsAcceso);
    }

    [Fact]
    public async Task Interpreta_respuesta_de_cae_aprobado()
    {
        using var db = _bd.CrearContexto();
        var (arca, http) = Crear(db);
        http.Responder = (accion, _) => accion == "" ? LoginResponse() : string.Format(Sobre,
            "<FECAESolicitarResponse xmlns=\"http://ar.gov.afip.dif.FEV1/\"><FECAESolicitarResult>" +
            "<FeCabResp><Cuit>20123456786</Cuit><PtoVta>2</PtoVta><CbteTipo>6</CbteTipo><FchProceso>20260924</FchProceso><CantReg>1</CantReg><Resultado>A</Resultado><Reproceso>N</Reproceso></FeCabResp>" +
            "<FeDetResp><FECAEDetResponse><Concepto>1</Concepto><DocTipo>99</DocTipo><DocNro>0</DocNro><CbteDesde>42</CbteDesde><CbteHasta>42</CbteHasta><CbteFch>20260924</CbteFch><Resultado>A</Resultado>" +
            "<Observaciones><Obs><Code>10217</Code><Msg>Observacion de prueba</Msg></Obs></Observaciones><CAE>76391234567890</CAE><CAEFchVto>20261004</CAEFchVto></FECAEDetResponse></FeDetResp>" +
            "</FECAESolicitarResult></FECAESolicitarResponse>");

        var r = await arca.SolicitarCaeAsync(SolicitudB());
        Assert.True(r.Aprobado);
        Assert.Equal("76391234567890", r.Cae);
        Assert.Equal(new DateTime(2026, 10, 4), r.CaeVencimiento);
        Assert.Equal(10217, r.Observaciones.Single().Codigo);
    }

    [Fact]
    public async Task Interpreta_rechazo_con_errores()
    {
        using var db = _bd.CrearContexto();
        var (arca, http) = Crear(db);
        http.Responder = (accion, _) => accion == "" ? LoginResponse() : string.Format(Sobre,
            "<FECAESolicitarResponse xmlns=\"http://ar.gov.afip.dif.FEV1/\"><FECAESolicitarResult>" +
            "<FeCabResp><Resultado>R</Resultado></FeCabResp>" +
            "<Errors><Err><Code>10016</Code><Msg>El numero no es el proximo</Msg></Err></Errors>" +
            "</FECAESolicitarResult></FECAESolicitarResponse>");

        var r = await arca.SolicitarCaeAsync(SolicitudB());
        Assert.False(r.Aprobado);
        Assert.Equal(10016, r.Errores.Single().Codigo);
    }

    [Fact]
    public async Task Token_vencido_pide_uno_nuevo_y_reintenta()
    {
        using var db = _bd.CrearContexto();
        var (arca, http) = Crear(db);
        var llamadasWsfe = 0;
        http.Responder = (accion, _) =>
        {
            if (accion == "") return LoginResponse();
            llamadasWsfe++;
            return llamadasWsfe == 1
                ? string.Format(Sobre, "<FECompUltimoAutorizadoResponse xmlns=\"http://ar.gov.afip.dif.FEV1/\"><FECompUltimoAutorizadoResult><Errors><Err><Code>600</Code><Msg>ValidacionDeToken: No validaron las fechas del token</Msg></Err></Errors></FECompUltimoAutorizadoResult></FECompUltimoAutorizadoResponse>")
                : string.Format(Sobre, "<FECompUltimoAutorizadoResponse xmlns=\"http://ar.gov.afip.dif.FEV1/\"><FECompUltimoAutorizadoResult><CbteNro>7</CbteNro></FECompUltimoAutorizadoResult></FECompUltimoAutorizadoResponse>");
        };

        Assert.Equal(7, await arca.UltimoAutorizadoAsync(1, TipoComprobante.FacturaB));
        Assert.Equal(2, http.Pedidos.Count(p => p.SoapAction == ""));
    }

    [Fact]
    public async Task Consulta_inexistente_devuelve_null()
    {
        using var db = _bd.CrearContexto();
        var (arca, http) = Crear(db);
        http.Responder = (accion, _) => accion == "" ? LoginResponse() : string.Format(Sobre,
            "<FECompConsultarResponse xmlns=\"http://ar.gov.afip.dif.FEV1/\"><FECompConsultarResult><Errors><Err><Code>602</Code><Msg>No existen datos en nuestros registros para los parametros ingresados.</Msg></Err></Errors></FECompConsultarResult></FECompConsultarResponse>");
        Assert.Null(await arca.ConsultarAsync(1, TipoComprobante.FacturaB, 99));
    }

    [Fact]
    public void Xml_respeta_el_orden_del_wsdl()
    {
        var xml = ArcaClient.ArmarFeCaeReq(SolicitudB() with
        {
            Concepto = ConceptoComprobante.Servicios,
            FechaServicioDesde = new DateTime(2026, 9, 24),
            FechaServicioHasta = new DateTime(2026, 9, 24),
            FechaVencimientoPago = new DateTime(2026, 9, 24)
        });
        var det = xml.Descendants().First(e => e.Name.LocalName == "FECAEDetRequest");
        var orden = det.Elements().Select(e => e.Name.LocalName).ToList();
        Assert.Equal(new[]
        {
            "Concepto", "DocTipo", "DocNro", "CbteDesde", "CbteHasta", "CbteFch", "ImpTotal", "ImpTotConc", "ImpNeto",
            "ImpOpEx", "ImpTrib", "ImpIVA", "FchServDesde", "FchServHasta", "FchVtoPago", "MonId", "MonCotiz",
            "CondicionIVAReceptorId", "Iva"
        }, orden);
        Assert.Equal("12100.00", det.Elements().First(e => e.Name.LocalName == "ImpTotal").Value);
        Assert.Equal("20260924", det.Elements().First(e => e.Name.LocalName == "CbteFch").Value);
        var alic = det.Descendants().First(e => e.Name.LocalName == "AlicIva");
        Assert.Equal("5", alic.Elements().First().Value);
    }

    private static SolicitudCae SolicitudB() => new(
        2, TipoComprobante.FacturaB, 42, ConceptoComprobante.Productos, TipoDocumento.SinIdentificar, 0,
        CondicionIva.ConsumidorFinal, new DateTime(2026, 9, 24), 12100m, 0, 10000m, 0, 0, 2100m, null, null, null,
        new[] { new AlicuotaCae(AlicuotaIva.Veintiuno, 10000m, 2100m) });
}
