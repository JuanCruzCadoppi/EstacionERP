using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Xml.Linq;
using EstacionERP.Application.Common;
using EstacionERP.Application.Configuracion;
using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Infrastructure.Arca;

public record Credenciales(string Token, string Sign, string Cuit);

/// <summary>
/// Cliente del WSAA (autenticación). Firma un "ticket de requerimiento de acceso" (TRA) con el
/// certificado y obtiene token + sign válidos por ~12 horas. El ticket se guarda en la base.
/// </summary>
public class WsaaClient
{
    private static readonly XNamespace NsWsaa = "http://wsaa.view.sua.dvadac.desein.afip.gov";

    private readonly HttpClient _http;
    private readonly IEstacionDbContext _db;

    public WsaaClient(HttpClient http, IEstacionDbContext db)
    {
        _http = http;
        _db = db;
    }

    public async Task<Credenciales> ObtenerCredencialesAsync(string servicio, bool forzarNuevo = false, CancellationToken ct = default)
    {
        var config = await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct)
                     ?? throw new ArcaException("faltan los datos fiscales de la empresa.");
        if (!config.TieneCertificado)
            throw new ArcaException("falta importar el certificado digital de ARCA.");

        var ahora = DateTime.UtcNow;
        var vigente = await _db.TicketsAcceso.FirstOrDefaultAsync(t =>
            t.Servicio == servicio && t.Entorno == config.Entorno && t.Cuit == config.Cuit, ct);

        if (vigente is not null && !forzarNuevo && vigente.Expira > ahora.AddMinutes(10))
            return new Credenciales(vigente.Token, vigente.Sign, config.Cuit);

        var cms = FirmarTra(ArmarTra(servicio, DateTimeOffset.Now), config.CertificadoPem!, config.ClavePrivadaPem!);

        var cuerpo = new XElement(NsWsaa + "loginCms", new XElement(NsWsaa + "in0", cms));
        XDocument respuesta;
        try
        {
            respuesta = await Soap.EnviarAsync(_http, ArcaEndpoints.Wsaa(config.Entorno), string.Empty, Soap.Sobre(cuerpo), ct);
        }
        catch (ArcaSoapFaultException ex) when (ex.Codigo.Contains("alreadyAuthenticated"))
        {
            throw new ArcaException("ARCA indica que ya entregó un ticket vigente para este certificado y no se encuentra guardado. " +
                                    "Esperá unos minutos (hasta que venza, máximo 12 horas) y volvé a intentar.", ex);
        }
        catch (ArcaSoapFaultException ex)
        {
            throw new ArcaException(TraducirFault(ex), ex);
        }

        var retorno = respuesta.Descendants().FirstOrDefault(e => e.Name.LocalName == "loginCmsReturn")?.Value
                      ?? throw new ArcaException("respuesta inesperada del WSAA.");
        var ticket = XDocument.Parse(retorno);
        var token = ticket.Descendants("token").First().Value;
        var sign = ticket.Descendants("sign").First().Value;
        var expira = DateTimeOffset.Parse(ticket.Descendants("expirationTime").First().Value, CultureInfo.InvariantCulture).UtcDateTime;

        if (vigente is null)
        {
            vigente = new TicketAcceso { Servicio = servicio, Entorno = config.Entorno, Cuit = config.Cuit };
            _db.TicketsAcceso.Add(vigente);
        }
        vigente.Token = token;
        vigente.Sign = sign;
        vigente.Expira = expira;
        vigente.ModificadoEn = ahora;
        await _db.SaveChangesAsync(ct);

        return new Credenciales(token, sign, config.Cuit);
    }

    /// <summary>Ticket de Requerimiento de Acceso (formato definido por ARCA).</summary>
    public static string ArmarTra(string servicio, DateTimeOffset ahora)
    {
        string F(DateTimeOffset d) => d.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        var tra = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("loginTicketRequest", new XAttribute("version", "1.0"),
                new XElement("header",
                    new XElement("uniqueId", ahora.ToUnixTimeSeconds()),
                    // Margen para diferencias de reloj entre la PC y ARCA.
                    new XElement("generationTime", F(ahora.AddMinutes(-10))),
                    new XElement("expirationTime", F(ahora.AddMinutes(10)))),
                new XElement("service", servicio)));
        return tra.Declaration + tra.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>Firma el TRA en formato CMS (PKCS#7) y lo devuelve en Base64.</summary>
    public static string FirmarTra(string tra, string certificadoPem, string clavePrivadaPem)
    {
        using var cert = CertificadoArca.ParaFirmar(certificadoPem, clavePrivadaPem);
        var cms = new SignedCms(new ContentInfo(Encoding.UTF8.GetBytes(tra)), detached: false);
        var firmante = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert)
        {
            IncludeOption = System.Security.Cryptography.X509Certificates.X509IncludeOption.EndCertOnly
        };
        cms.ComputeSignature(firmante);
        return Convert.ToBase64String(cms.Encode());
    }

    private static string TraducirFault(ArcaSoapFaultException ex)
    {
        var c = ex.Codigo;
        if (c.Contains("cms.cert.untrusted") || c.Contains("cms.cert.notFound"))
            return "ARCA no reconoce el certificado. Revisá que sea del mismo entorno (homologación/producción) que elegiste.";
        if (c.Contains("cms.cert.expired"))
            return "el certificado está vencido. Generá uno nuevo.";
        if (c.Contains("coe.notAuthorized"))
            return "el certificado no tiene autorizado el servicio de factura electrónica (wsfe). Creá la autorización en ARCA.";
        if (c.Contains("xml.generationTime.invalid") || c.Contains("xml.expirationTime.invalid"))
            return "la fecha y hora de esta PC no coinciden con las de ARCA. Ajustá el reloj de Windows (sincronizar hora).";
        return ex.Message;
    }
}
