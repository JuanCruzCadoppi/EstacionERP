using System.Globalization;
using System.Text;
using System.Text.Json;
using EstacionERP.Domain.Entidades;

namespace EstacionERP.Application.Facturacion;

/// <summary>
/// Genera la URL del código QR obligatorio en comprobantes electrónicos
/// (especificación de ARCA: JSON en Base64 sobre https://www.arca.gob.ar/fe/qr/?p=).
/// </summary>
public static class QrComprobante
{
    public const string UrlBase = "https://www.arca.gob.ar/fe/qr/?p=";

    public static string GenerarUrl(string cuitEmisor, Comprobante c)
    {
        if (c.Numero is null || c.Cae is null)
            throw new InvalidOperationException("Solo los comprobantes autorizados llevan QR.");

        var datos = new Dictionary<string, object>
        {
            ["ver"] = 1,
            ["fecha"] = c.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["cuit"] = long.Parse(cuitEmisor),
            ["ptoVta"] = c.PuntoVentaNumero,
            ["tipoCmp"] = (int)c.Tipo,
            ["nroCmp"] = c.Numero.Value,
            ["importe"] = c.ImporteTotal,
            ["moneda"] = "PES",
            ["ctz"] = 1,
            ["tipoDocRec"] = (int)c.ReceptorTipoDocumento,
            ["nroDocRec"] = long.TryParse(c.ReceptorNumeroDocumento, out var doc) ? doc : 0,
            ["tipoCodAut"] = "E",
            ["codAut"] = long.Parse(c.Cae)
        };

        var json = JsonSerializer.Serialize(datos);
        return UrlBase + Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}
