using System.Text;
using System.Text.Json;
using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Domain.Validaciones;

namespace EstacionERP.Tests;

public class CalculadoraComprobanteTests
{
    [Fact]
    public void Factura_B_discrimina_iva_por_alicuota()
    {
        var r = CalculadoraComprobante.Calcular(new[]
        {
            new LineaCalculo(2, 6050m, AlicuotaIva.Veintiuno, false),   // 12.100
            new LineaCalculo(1, 1105m, AlicuotaIva.DiezCinco, false)    // 1.105
        }, TipoComprobante.FacturaB);

        Assert.Equal(13205m, r.Total);
        Assert.Equal(2, r.Alicuotas.Count);
        var a105 = r.Alicuotas.Single(a => a.Alicuota == AlicuotaIva.DiezCinco);
        Assert.Equal(1000m, a105.BaseImponible);
        Assert.Equal(105m, a105.Importe);
        var a21 = r.Alicuotas.Single(a => a.Alicuota == AlicuotaIva.Veintiuno);
        Assert.Equal(10000m, a21.BaseImponible);
        Assert.Equal(2100m, a21.Importe);
        Assert.Equal(r.Total, r.Neto + r.Iva);
    }

    [Fact]
    public void Redondeo_por_grupo_cierra_exacto_con_el_total()
    {
        // Precios "feos" que generan diferencias de centavos si se calcula ítem por ítem.
        var lineas = Enumerable.Range(0, 7).Select(i => new LineaCalculo(3, 333.33m, AlicuotaIva.Veintiuno, false)).ToList();
        var r = CalculadoraComprobante.Calcular(lineas, TipoComprobante.FacturaA);
        Assert.Equal(r.Total, r.Neto + r.Iva);
        var a = Assert.Single(r.Alicuotas);
        Assert.True(Math.Abs(a.BaseImponible * 0.21m - a.Importe) <= 0.01m);
    }

    [Fact]
    public void Factura_C_no_discrimina_iva()
    {
        var r = CalculadoraComprobante.Calcular(new[] { new LineaCalculo(1, 12100m, AlicuotaIva.Veintiuno, false) }, TipoComprobante.FacturaC);
        Assert.Equal(12100m, r.Neto);
        Assert.Equal(0m, r.Iva);
        Assert.Empty(r.Alicuotas);
    }

    [Theory]
    [InlineData(false, false, ConceptoComprobante.Productos)]
    [InlineData(true, true, ConceptoComprobante.Servicios)]
    [InlineData(true, false, ConceptoComprobante.ProductosYServicios)]
    public void Concepto_segun_items(bool s1, bool s2, ConceptoComprobante esperado)
    {
        var r = CalculadoraComprobante.Calcular(new[]
        {
            new LineaCalculo(1, 100, AlicuotaIva.Veintiuno, s1),
            new LineaCalculo(1, 100, AlicuotaIva.Veintiuno, s2)
        }, TipoComprobante.FacturaB);
        Assert.Equal(esperado, r.Concepto);
    }
}

public class ReglasComprobanteTests
{
    [Theory]
    [InlineData(CondicionIva.ResponsableInscripto, CondicionIva.ResponsableInscripto, TipoComprobante.FacturaA)]
    [InlineData(CondicionIva.ResponsableInscripto, CondicionIva.Monotributo, TipoComprobante.FacturaA)]
    [InlineData(CondicionIva.ResponsableInscripto, CondicionIva.ConsumidorFinal, TipoComprobante.FacturaB)]
    [InlineData(CondicionIva.ResponsableInscripto, CondicionIva.Exento, TipoComprobante.FacturaB)]
    [InlineData(CondicionIva.Monotributo, CondicionIva.ResponsableInscripto, TipoComprobante.FacturaC)]
    [InlineData(CondicionIva.Monotributo, CondicionIva.ConsumidorFinal, TipoComprobante.FacturaC)]
    public void Tipo_de_factura(CondicionIva emisor, CondicionIva receptor, TipoComprobante esperado) =>
        Assert.Equal(esperado, ReglasComprobante.TipoFactura(emisor, receptor));
}

public class QrComprobanteTests
{
    [Fact]
    public void Genera_url_con_json_en_base64()
    {
        var c = new Comprobante
        {
            Fecha = new DateTime(2026, 9, 24), PuntoVentaNumero = 3, Tipo = TipoComprobante.FacturaB, Numero = 125,
            ImporteTotal = 12100.50m, ReceptorTipoDocumento = TipoDocumento.Dni, ReceptorNumeroDocumento = "30111222",
            Cae = "76123456789012"
        };
        var url = QrComprobante.GenerarUrl("20123456786", c);
        Assert.StartsWith("https://www.arca.gob.ar/fe/qr/?p=", url);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(url[QrComprobante.UrlBase.Length..]));
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        Assert.Equal(1, r.GetProperty("ver").GetInt32());
        Assert.Equal("2026-09-24", r.GetProperty("fecha").GetString());
        Assert.Equal(20123456786, r.GetProperty("cuit").GetInt64());
        Assert.Equal(6, r.GetProperty("tipoCmp").GetInt32());
        Assert.Equal(125, r.GetProperty("nroCmp").GetInt64());
        Assert.Equal(12100.50m, r.GetProperty("importe").GetDecimal());
        Assert.Equal(76123456789012, r.GetProperty("codAut").GetInt64());
    }
}
