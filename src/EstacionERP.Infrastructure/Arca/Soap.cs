using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using EstacionERP.Application.Facturacion;

namespace EstacionERP.Infrastructure.Arca;

/// <summary>Envío de mensajes SOAP 1.1 con manejo de errores de red y SOAP Faults.</summary>
internal static class Soap
{
    public static readonly XNamespace Envelope = "http://schemas.xmlsoap.org/soap/envelope/";

    public static XDocument Sobre(XElement cuerpo) => new(
        new XDeclaration("1.0", "utf-8", null),
        new XElement(Envelope + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soap", Envelope),
            new XElement(Envelope + "Body", cuerpo)));

    public static async Task<XDocument> EnviarAsync(HttpClient http, string url, string soapAction, XDocument sobre, CancellationToken ct)
    {
        using var contenido = new StringContent(sobre.Declaration + sobre.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = contenido };
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{soapAction}\"");

        HttpResponseMessage response;
        string texto;
        try
        {
            response = await http.SendAsync(request, ct);
            texto = await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new ArcaComunicacionException("no hay conexión con los servidores de ARCA (" + ex.Message + ").", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ArcaComunicacionException("ARCA no respondió a tiempo.", ex);
        }

        using (response)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Parse(texto);
            }
            catch (Exception)
            {
                if (!response.IsSuccessStatusCode)
                    throw new ArcaComunicacionException($"el servidor de ARCA respondió con error HTTP {(int)response.StatusCode}.");
                throw new ArcaException("la respuesta de ARCA no es un XML válido.");
            }

            var fault = doc.Descendants(Envelope + "Fault").FirstOrDefault();
            if (fault is not null)
            {
                var codigo = fault.Element("faultcode")?.Value ?? string.Empty;
                var mensaje = fault.Element("faultstring")?.Value ?? "error desconocido";
                throw new ArcaSoapFaultException(codigo, mensaje);
            }

            if (!response.IsSuccessStatusCode)
                throw new ArcaComunicacionException($"el servidor de ARCA respondió con error HTTP {(int)response.StatusCode}.");

            return doc;
        }
    }
}

/// <summary>Error SOAP devuelto por ARCA (ej. coe.alreadyAuthenticated, cms.cert.untrusted).</summary>
public class ArcaSoapFaultException : ArcaException
{
    public string Codigo { get; }

    public ArcaSoapFaultException(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;
}
