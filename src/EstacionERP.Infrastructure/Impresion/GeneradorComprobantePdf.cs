using System.Globalization;
using EstacionERP.Application.Impresion;
using EstacionERP.Domain.Enums;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace EstacionERP.Infrastructure.Impresion;

/// <summary>Genera el PDF del comprobante en hoja A4 o en ticket de 80 mm.</summary>
public class GeneradorComprobantePdf : IGeneradorComprobantePdf
{
    private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");
    private static readonly XColor Gris = XColor.FromArgb(230, 230, 230);
    private static readonly XBrush Rojo = new XSolidBrush(XColor.FromArgb(180, 0, 0));

    public GeneradorComprobantePdf() => FuentesPdf.Registrar();

    public byte[] Generar(ComprobanteImprimible c, FormatoImpresion formato) => formato switch
    {
        FormatoImpresion.Ticket80 => Ticket(c),
        _ => A4(c)
    };

    private static string Pesos(decimal v) => "$ " + v.ToString("N2", Ar);
    private static string Cant(decimal v) => v == Math.Round(v) ? v.ToString("N0", Ar) : v.ToString("0.###", Ar);
    private static string F(DateTime? d) => d?.ToString("dd/MM/yyyy", Ar) ?? "-";

    // =====================================================================================
    //  HOJA A4
    // =====================================================================================

    private const double AnchoA4 = 210, AltoA4 = 297, Mg = 10;
    private const double AltoPie = 72;          // lugar que ocupan totales + CAE + QR en la última hoja

    private static byte[] A4(ComprobanteImprimible c)
    {
        var l = new Lienzo(AnchoA4);
        var columnas = c.MuestraNeto
            ? new[] { ("Código", 20.0), ("Producto / Servicio", 0.0), ("Cantidad", 18.0), ("Precio unit.", 26.0), ("IVA", 14.0), ("Subtotal", 28.0) }
            : new[] { ("Código", 20.0), ("Producto / Servicio", 0.0), ("Cantidad", 18.0), ("Precio unit.", 28.0), ("Subtotal", 30.0) };
        var anchoTabla = AnchoA4 - 2 * Mg;
        var anchoDescripcion = anchoTabla - columnas.Sum(x => x.Item2);
        var anchos = columnas.Select(x => x.Item2 == 0 ? anchoDescripcion : x.Item2).ToArray();

        var fTabla = Lienzo.Fuente(8);
        var fTablaN = Lienzo.Fuente(8, true);
        var limiteHoja = AltoA4 - Mg - 8;           // deja lugar para "Pág. x/y"

        var y = CabeceraA4(l, c);
        y = EncabezadoTabla(l, y, columnas.Select(x => x.Item1).ToArray(), anchos, fTablaN);

        foreach (var r in c.Renglones)
        {
            var descLineas = l.Cortar(r.Descripcion, fTabla, anchos[1] - 2);
            var altoFila = Math.Max(1, descLineas.Count) * Lienzo.AltoLinea(fTabla) + 2;
            if (y + altoFila > limiteHoja)
            {
                l.NuevaPagina();
                y = CabeceraA4(l, c);
                y = EncabezadoTabla(l, y, columnas.Select(x => x.Item1).ToArray(), anchos, fTablaN);
            }

            var valores = c.MuestraNeto
                ? new[] { r.Codigo ?? "", r.Descripcion, Cant(r.Cantidad), r.PrecioUnitario.ToString("N2", Ar), r.Alicuota ?? "", r.Subtotal.ToString("N2", Ar) }
                : new[] { r.Codigo ?? "", r.Descripcion, Cant(r.Cantidad), r.PrecioUnitario.ToString("N2", Ar), r.Subtotal.ToString("N2", Ar) };

            var x = Mg;
            for (var i = 0; i < valores.Length; i++)
            {
                var al = i <= 1 ? Alineacion.Izquierda : i == 4 && c.MuestraNeto ? Alineacion.Centro : Alineacion.Derecha;
                l.Texto(x + 1, y + 1, anchos[i] - 2, valores[i], fTabla, al);
                x += anchos[i];
            }
            y += altoFila;
            l.Linea(Mg, y, AnchoA4 - Mg, y, 0.25);
        }

        // El pie va siempre abajo de la última hoja; si no entra, hoja nueva.
        if (y > AltoA4 - Mg - AltoPie - 2)
        {
            l.NuevaPagina();
            CabeceraA4(l, c);
        }
        PieA4(l, c);
        return Renderizar(l, AnchoA4, _ => AltoA4, numerarPaginas: true);
    }

    private static double EncabezadoTabla(Lienzo l, double y, string[] titulos, double[] anchos, XFont f)
    {
        var alto = Lienzo.AltoLinea(f) + 2.5;
        l.Rect(Mg, y, AnchoA4 - 2 * Mg, alto, Gris);
        var x = Mg;
        for (var i = 0; i < titulos.Length; i++)
        {
            var al = i <= 1 ? Alineacion.Izquierda : Alineacion.Derecha;
            if (titulos[i] == "IVA") al = Alineacion.Centro;
            l.Texto(x + 1, y + 1.2, anchos[i] - 2, titulos[i], f, al);
            x += anchos[i];
        }
        return y + alto;
    }

    /// <summary>Recuadro del emisor, letra, datos del comprobante y del receptor. Devuelve la Y siguiente.</summary>
    private static double CabeceraA4(Lienzo l, ComprobanteImprimible c)
    {
        var f8 = Lienzo.Fuente(8.5);
        var f8n = Lienzo.Fuente(8.5, true);
        var y0 = Mg;

        if (c.EsDePrueba)
            l.Texto(Mg, 3.5, AnchoA4 - 2 * Mg, "COMPROBANTE DE PRUEBA (HOMOLOGACIÓN) - SIN VALIDEZ FISCAL",
                Lienzo.Fuente(9, true), Alineacion.Centro, Rojo);

        // Recuadro principal
        const double altoCab = 38;
        l.Rect(Mg, y0, AnchoA4 - 2 * Mg, altoCab);
        const double centro = AnchoA4 / 2;
        const double lado = 15;
        l.Rect(centro - lado / 2, y0, lado, lado, XColors.White);
        l.Texto(centro - lado / 2, y0 + 1, lado, c.Letra.ToString(), Lienzo.Fuente(24, true), Alineacion.Centro);
        l.Texto(centro - lado / 2, y0 + lado - 4, lado, $"COD. {c.Codigo:D3}", Lienzo.Fuente(6.5, true), Alineacion.Centro);
        l.Linea(centro, y0 + lado, centro, y0 + altoCab);

        // Emisor (izquierda)
        var xi = Mg + 3;
        var ai = centro - lado / 2 - xi - 2;
        var y = y0 + 3;
        y += l.Texto(xi, y, ai, c.EmisorNombreFantasia ?? c.EmisorRazonSocial, Lienzo.Fuente(13, true)) + 1;
        ai = centro - xi - 3;
        if (!string.IsNullOrWhiteSpace(c.EmisorNombreFantasia))
            y += l.Texto(xi, y, ai, "Razón social: " + c.EmisorRazonSocial, f8);
        if (!string.IsNullOrWhiteSpace(c.EmisorDomicilio))
            y += l.Texto(xi, y, ai, "Domicilio comercial: " + c.EmisorDomicilio, f8);
        l.Texto(xi, y, ai, "Condición frente al IVA: " + c.EmisorCondicionIva, f8n);

        // Comprobante (derecha)
        var xd = centro + lado / 2 + 3;
        var ad = AnchoA4 - Mg - xd - 3;
        y = y0 + 3;
        y += l.Texto(xd, y, ad, c.TipoTexto, Lienzo.Fuente(16, true)) + 1.5;
        y += l.Texto(xd, y, ad, $"Punto de venta: {c.PuntoVenta:D5}    Comp. Nro: {c.Numero:D8}", f8n);
        y += l.Texto(xd, y, ad, "Fecha de emisión: " + F(c.Fecha), f8n) + 1;
        y += l.Texto(xd, y, ad, "CUIT: " + c.EmisorCuit, f8);
        if (!string.IsNullOrWhiteSpace(c.EmisorIngresosBrutos))
            y += l.Texto(xd, y, ad, "Ingresos Brutos: " + c.EmisorIngresosBrutos, f8);
        if (c.EmisorInicioActividades is not null)
            l.Texto(xd, y, ad, "Fecha de inicio de actividades: " + F(c.EmisorInicioActividades), f8);

        y = y0 + altoCab + 1.5;

        // Período (servicios)
        if (c.EsServicio)
        {
            l.Rect(Mg, y, AnchoA4 - 2 * Mg, 7);
            l.Texto(Mg + 3, y + 1.8, AnchoA4 - 2 * Mg - 6,
                $"Período facturado desde: {F(c.ServicioDesde)}    Hasta: {F(c.ServicioHasta)}    Fecha de vto. para el pago: {F(c.VencimientoPago)}", f8n);
            y += 8.5;
        }

        // Receptor
        var ancho = AnchoA4 - 2 * Mg;
        var mitad = ancho / 2;
        var yr = y + 2;
        var hIzq = c.ReceptorDocumento is null ? 0
            : l.Texto(Mg + 3, yr, mitad - 4, $"{c.ReceptorDocumentoTipo}: {c.ReceptorDocumento}", f8n);
        hIzq += l.Texto(Mg + 3, yr + hIzq, mitad - 4, "Condición frente al IVA: " + c.ReceptorCondicionIva, f8);
        var hDer = l.Texto(Mg + mitad, yr, mitad - 3, "Apellido y nombre / Razón social: " + c.ReceptorNombre, f8n);
        if (!string.IsNullOrWhiteSpace(c.ReceptorDomicilio))
            hDer += l.Texto(Mg + mitad, yr + hDer, mitad - 3, "Domicilio: " + c.ReceptorDomicilio, f8);
        var altoRec = Math.Max(hIzq, hDer) + 4;
        l.Rect(Mg, y, ancho, altoRec);
        return y + altoRec + 2;
    }

    private static void PieA4(Lienzo l, ComprobanteImprimible c)
    {
        var y0 = AltoA4 - Mg - AltoPie;
        var ancho = AnchoA4 - 2 * Mg;
        var f8 = Lienzo.Fuente(8.5);
        var f8n = Lienzo.Fuente(8.5, true);

        // Totales (derecha)
        const double anchoTot = 78;
        var xt = AnchoA4 - Mg - anchoTot;
        var filas = new List<(string, string)>();
        if (c.MuestraNeto)
        {
            filas.Add(("Importe neto gravado:", Pesos(c.Neto)));
            foreach (var (al, imp) in c.Iva) filas.Add(($"IVA {al}:", Pesos(imp)));
        }
        else
        {
            filas.Add(("Subtotal:", Pesos(c.Total - c.OtrosTributos)));
        }
        filas.Add(("Importe otros tributos:", Pesos(c.OtrosTributos)));

        var altoTot = filas.Count * Lienzo.AltoLinea(f8) + Lienzo.AltoLinea(Lienzo.Fuente(11, true)) + 6;
        l.Rect(xt, y0, anchoTot, altoTot);
        var y = y0 + 2;
        foreach (var (et, v) in filas) y += l.Par(xt + 3, y, anchoTot - 6, et, v, f8);
        y += 1;
        l.Par(xt + 3, y, anchoTot - 6, "IMPORTE TOTAL:", Pesos(c.Total), Lienzo.Fuente(11, true));

        // Transparencia fiscal y leyendas (izquierda)
        var anchoIzq = ancho - anchoTot - 4;
        y = y0;
        if (c.TransparenciaIvaContenido is { } ivaCont)
        {
            var hT = Lienzo.AltoLinea(f8n) + 2 * Lienzo.AltoLinea(f8) + 4;
            l.Rect(Mg, y, anchoIzq, hT);
            var yy = y + 1.5;
            yy += l.Texto(Mg + 2, yy, anchoIzq - 4, "Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)", f8n);
            yy += l.Par(Mg + 2, yy, anchoIzq - 4, "IVA contenido:", Pesos(ivaCont), f8);
            l.Par(Mg + 2, yy, anchoIzq - 4, "Otros impuestos nacionales indirectos:", Pesos(c.TransparenciaOtrosImpuestos), f8);
            y += hT + 2;
        }
        foreach (var ley in c.Leyendas)
            y += l.Texto(Mg, y, anchoIzq, ley, Lienzo.Fuente(7.5)) + 1;

        // QR + CAE (abajo)
        const double qr = 30;
        var yq = AltoA4 - Mg - 6 - qr;
        l.Linea(Mg, yq - 2, AnchoA4 - Mg, yq - 2, 0.4);
        l.Qr(Mg, yq, qr, c.QrUrl);
        var xa = Mg + qr + 4;
        l.Texto(xa, yq + 3, 70, "ARCA", Lienzo.Fuente(16, true));
        l.Texto(xa, yq + 11, 80, "Comprobante autorizado", Lienzo.Fuente(10, true));
        l.Texto(xa, yq + 16.5, 80, "Consultá su validez escaneando el código QR.", Lienzo.Fuente(7.5));

        var xc = AnchoA4 - Mg - 70;
        l.Par(xc, yq + 11, 70, "CAE N°:", c.Cae, f8n);
        l.Par(xc, yq + 16, 70, "Fecha de vto. de CAE:", F(c.CaeVencimiento), f8n);
        if (c.EsDePrueba)
            l.Texto(xc, yq + 22, 70, "PRUEBA - SIN VALIDEZ FISCAL", Lienzo.Fuente(8.5, true), Alineacion.Derecha, Rojo);
    }

    // =====================================================================================
    //  TICKET 80 mm
    // =====================================================================================

    private const double AnchoTicket = 80, MgT = 4;

    private static byte[] Ticket(ComprobanteImprimible c)
    {
        var l = new Lienzo(AnchoTicket);
        var a = AnchoTicket - 2 * MgT;
        var x = MgT;
        var f = Lienzo.Fuente(8);
        var fn = Lienzo.Fuente(8, true);
        var fc = Lienzo.Fuente(7);
        var y = 3.0;

        void Separador()
        {
            y += 1.2;
            l.Linea(x, y, x + a, y, 0.5, punteada: true);
            y += 1.8;
        }

        if (c.EsDePrueba)
        {
            y += l.Texto(x, y, a, "*** COMPROBANTE DE PRUEBA ***", fn, Alineacion.Centro, Rojo);
            y += l.Texto(x, y, a, "SIN VALIDEZ FISCAL", fn, Alineacion.Centro, Rojo) + 1;
        }

        // Emisor
        y += l.Texto(x, y, a, c.EmisorNombreFantasia ?? c.EmisorRazonSocial, Lienzo.Fuente(11, true), Alineacion.Centro) + 0.5;
        if (!string.IsNullOrWhiteSpace(c.EmisorNombreFantasia))
            y += l.Texto(x, y, a, c.EmisorRazonSocial, f, Alineacion.Centro);
        y += l.Texto(x, y, a, "CUIT: " + c.EmisorCuit, f, Alineacion.Centro);
        if (!string.IsNullOrWhiteSpace(c.EmisorIngresosBrutos))
            y += l.Texto(x, y, a, "IIBB: " + c.EmisorIngresosBrutos, f, Alineacion.Centro);
        if (c.EmisorInicioActividades is not null)
            y += l.Texto(x, y, a, "Inicio de actividades: " + F(c.EmisorInicioActividades), f, Alineacion.Centro);
        if (!string.IsNullOrWhiteSpace(c.EmisorDomicilio))
            y += l.Texto(x, y, a, c.EmisorDomicilio, f, Alineacion.Centro);
        y += l.Texto(x, y, a, c.EmisorCondicionIva, f, Alineacion.Centro);

        Separador();

        // Comprobante
        y += l.Texto(x, y, a, $"{c.TipoTexto} {c.Letra}", Lienzo.Fuente(12, true), Alineacion.Centro);
        y += l.Texto(x, y, a, $"Cód. {c.Codigo:D3}", fc, Alineacion.Centro);
        y += l.Texto(x, y, a, $"P.V. {c.PuntoVenta:D5}  Nro. {c.Numero:D8}", fn, Alineacion.Centro);
        y += l.Texto(x, y, a, "Fecha: " + F(c.Fecha), f, Alineacion.Centro);
        if (c.EsServicio)
        {
            y += l.Texto(x, y, a, $"Período: {F(c.ServicioDesde)} al {F(c.ServicioHasta)}", fc, Alineacion.Centro);
            y += l.Texto(x, y, a, "Vto. pago: " + F(c.VencimientoPago), fc, Alineacion.Centro);
        }

        Separador();

        // Receptor
        y += l.Texto(x, y, a, "Cliente: " + c.ReceptorNombre, fn);
        if (c.ReceptorDocumento is not null)
            y += l.Texto(x, y, a, $"{c.ReceptorDocumentoTipo}: {c.ReceptorDocumento}", f);
        y += l.Texto(x, y, a, "Cond. IVA: " + c.ReceptorCondicionIva, f);
        if (!string.IsNullOrWhiteSpace(c.ReceptorDomicilio))
            y += l.Texto(x, y, a, "Domicilio: " + c.ReceptorDomicilio, f);

        Separador();

        // Detalle
        foreach (var r in c.Renglones)
        {
            y += l.Texto(x, y, a, r.Descripcion, f);
            var detalle = $"  {Cant(r.Cantidad)} x {r.PrecioUnitario.ToString("N2", Ar)}" + (r.Alicuota is null ? "" : $"  ({r.Alicuota})");
            y += l.Par(x, y, a, detalle, r.Subtotal.ToString("N2", Ar), fc, f) + 0.6;
        }

        Separador();

        // Totales
        if (c.MuestraNeto)
        {
            y += l.Par(x, y, a, "Neto gravado", Pesos(c.Neto), f);
            foreach (var (al, imp) in c.Iva) y += l.Par(x, y, a, "IVA " + al, Pesos(imp), f);
        }
        if (c.OtrosTributos != 0)
            y += l.Par(x, y, a, "Otros tributos", Pesos(c.OtrosTributos), f);
        y += 0.5;
        y += l.Par(x, y, a, "TOTAL", Pesos(c.Total), Lienzo.Fuente(12, true)) + 1;

        if (c.TransparenciaIvaContenido is { } ivaCont)
        {
            Separador();
            y += l.Texto(x, y, a, "Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)", Lienzo.Fuente(7, true));
            y += l.Par(x, y, a, "IVA contenido", Pesos(ivaCont), fc);
            y += l.Par(x, y, a, "Otros imp. nacionales indirectos", Pesos(c.TransparenciaOtrosImpuestos), fc);
        }

        foreach (var ley in c.Leyendas)
        {
            y += 1;
            y += l.Texto(x, y, a, ley, Lienzo.Fuente(6.5));
        }

        Separador();

        // QR y CAE
        const double qr = 38;
        l.Qr(x + (a - qr) / 2, y, qr, c.QrUrl);
        y += qr + 1;
        y += l.Texto(x, y, a, "Comprobante autorizado por ARCA", fn, Alineacion.Centro);
        y += l.Texto(x, y, a, "CAE: " + c.Cae, f, Alineacion.Centro);
        y += l.Texto(x, y, a, "Vto. CAE: " + F(c.CaeVencimiento), f, Alineacion.Centro);
        if (c.EsDePrueba)
            y += l.Texto(x, y, a, "PRUEBA - SIN VALIDEZ FISCAL", fn, Alineacion.Centro, Rojo);

        var alto = y + 8;       // margen final para el corte de papel
        return Renderizar(l, AnchoTicket, _ => alto, numerarPaginas: false);
    }

    // =====================================================================================

    private static byte[] Renderizar(Lienzo l, double anchoMm, Func<int, double> altoMm, bool numerarPaginas)
    {
        using var doc = new PdfDocument();
        doc.Info.Title = "Comprobante";
        doc.Info.Creator = "Estación ERP";
        var total = l.CantidadPaginas;

        for (var i = 0; i < total; i++)
        {
            var page = doc.AddPage();
            page.Width = XUnit.FromMillimeter(anchoMm);
            page.Height = XUnit.FromMillimeter(altoMm(i));
            using var g = XGraphics.FromPdfPage(page);
            foreach (var accion in l.Paginas[i]) accion(g);

            if (numerarPaginas)
            {
                var f = Lienzo.Fuente(7.5);
                var texto = $"Pág. {i + 1}/{total}";
                g.DrawString(texto, f, XBrushes.Black,
                    new XRect(0, Lienzo.Pt(altoMm(i) - Mg + 1), Lienzo.Pt(anchoMm), Lienzo.Pt(5)), XStringFormats.TopCenter);
            }
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }
}
