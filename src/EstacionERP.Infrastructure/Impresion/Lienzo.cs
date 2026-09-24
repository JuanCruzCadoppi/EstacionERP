using PdfSharp.Drawing;
using QRCoder;

namespace EstacionERP.Infrastructure.Impresion;

public enum Alineacion { Izquierda, Centro, Derecha }

/// <summary>
/// Graba las operaciones de dibujo (en milímetros) para poder calcular primero el alto total
/// (ticket de largo variable) o repartir en hojas (A4), y después dibujarlas en el PDF.
/// </summary>
internal sealed class Lienzo
{
    private const double PtPorMm = 72.0 / 25.4;
    private readonly XGraphics _medidor;
    private readonly List<List<Action<XGraphics>>> _paginas = new() { new() };

    public Lienzo(double anchoMm)
    {
        AnchoMm = anchoMm;
        _medidor = XGraphics.CreateMeasureContext(new XSize(Pt(anchoMm), Pt(1000)), XGraphicsUnit.Point, XPageDirection.Downwards);
    }

    public double AnchoMm { get; }
    public int CantidadPaginas => _paginas.Count;
    public IReadOnlyList<List<Action<XGraphics>>> Paginas => _paginas;
    private List<Action<XGraphics>> Actual => _paginas[^1];

    public static double Pt(double mm) => mm * PtPorMm;

    public static XFont Fuente(double puntos, bool negrita = false) =>
        new(FuentesPdf.Familia, puntos, negrita ? XFontStyleEx.Bold : XFontStyleEx.Regular);

    public void NuevaPagina() => _paginas.Add(new());

    /// <summary>Alto de una línea de texto en mm.</summary>
    public static double AltoLinea(XFont f) => f.GetHeight() / PtPorMm;

    public double AnchoTexto(string texto, XFont f) => _medidor.MeasureString(texto, f).Width / PtPorMm;

    /// <summary>Corta el texto en renglones que entren en el ancho indicado.</summary>
    public List<string> Cortar(string? texto, XFont f, double anchoMm)
    {
        var renglones = new List<string>();
        if (string.IsNullOrEmpty(texto)) return renglones;

        foreach (var parrafo in texto.Replace("\r", "").Split('\n'))
        {
            var actual = string.Empty;
            foreach (var palabra in parrafo.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var prueba = actual.Length == 0 ? palabra : actual + " " + palabra;
                if (AnchoTexto(prueba, f) <= anchoMm)
                {
                    actual = prueba;
                    continue;
                }
                if (actual.Length > 0) renglones.Add(actual);

                // Palabra más larga que el ancho: se corta por caracteres.
                actual = palabra;
                while (AnchoTexto(actual, f) > anchoMm && actual.Length > 1)
                {
                    var n = actual.Length - 1;
                    while (n > 1 && AnchoTexto(actual[..n], f) > anchoMm) n--;
                    renglones.Add(actual[..n]);
                    actual = actual[n..];
                }
            }
            renglones.Add(actual);
        }
        return renglones;
    }

    /// <summary>Escribe texto con salto de línea automático. Devuelve el alto usado (mm).</summary>
    public double Texto(double x, double y, double ancho, string? texto, XFont f,
        Alineacion al = Alineacion.Izquierda, XBrush? color = null)
    {
        var lineas = Cortar(texto, f, ancho);
        var alto = AltoLinea(f);
        var pincel = color ?? XBrushes.Black;
        for (var i = 0; i < lineas.Count; i++)
        {
            var linea = lineas[i];
            var yy = y + i * alto;
            var formato = al switch
            {
                Alineacion.Centro => XStringFormats.TopCenter,
                Alineacion.Derecha => XStringFormats.TopRight,
                _ => XStringFormats.TopLeft
            };
            var rect = new XRect(Pt(x), Pt(yy), Pt(ancho), Pt(alto));
            Actual.Add(g => g.DrawString(linea, f, pincel, rect, formato));
        }
        return lineas.Count * alto;
    }

    /// <summary>Alto que ocuparía el texto (mm), sin dibujarlo.</summary>
    public double AltoTexto(string? texto, XFont f, double ancho) => Cortar(texto, f, ancho).Count * AltoLinea(f);

    /// <summary>Texto a la izquierda y valor a la derecha en el mismo renglón.</summary>
    public double Par(double x, double y, double ancho, string etiqueta, string valor, XFont f, XFont? fValor = null)
    {
        fValor ??= f;
        var anchoValor = AnchoTexto(valor, fValor) + 1;
        Texto(x + ancho - anchoValor, y, anchoValor, valor, fValor, Alineacion.Derecha);
        var alto = Texto(x, y, Math.Max(10, ancho - anchoValor - 1), etiqueta, f);
        return Math.Max(alto, AltoLinea(fValor));
    }

    public void Linea(double x1, double y1, double x2, double y2, double grosorPt = 0.6, bool punteada = false)
    {
        var pen = new XPen(XColors.Black, grosorPt);
        if (punteada) pen.DashStyle = XDashStyle.Dash;
        Actual.Add(g => g.DrawLine(pen, Pt(x1), Pt(y1), Pt(x2), Pt(y2)));
    }

    public void Rect(double x, double y, double ancho, double alto, XColor? relleno = null, bool borde = true)
    {
        var pen = borde ? new XPen(XColors.Black, 0.6) : null;
        XBrush? brush = relleno is { } c ? new XSolidBrush(c) : null;
        var r = new XRect(Pt(x), Pt(y), Pt(ancho), Pt(alto));
        Actual.Add(g =>
        {
            if (brush is not null && pen is not null) g.DrawRectangle(pen, brush, r);
            else if (brush is not null) g.DrawRectangle(brush, r);
            else if (pen is not null) g.DrawRectangle(pen, r);
        });
    }

    /// <summary>Código QR dibujado como vectores (nítido en impresoras térmicas).</summary>
    public void Qr(double x, double y, double lado, string contenido)
    {
        using var gen = new QRCodeGenerator();
        using var datos = gen.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);
        var matriz = datos.ModuleMatrix;
        var n = matriz.Count;
        var modulo = lado / n;
        var rects = new List<XRect>();
        for (var fila = 0; fila < n; fila++)
        {
            var col = 0;
            while (col < n)
            {
                if (!matriz[fila][col]) { col++; continue; }
                var inicio = col;
                while (col < n && matriz[fila][col]) col++;
                // Se agranda un poco cada módulo para que no queden líneas blancas entre ellos.
                rects.Add(new XRect(Pt(x + inicio * modulo), Pt(y + fila * modulo),
                    Pt((col - inicio) * modulo) + 0.2, Pt(modulo) + 0.2));
            }
        }
        Actual.Add(g =>
        {
            foreach (var r in rects) g.DrawRectangle(XBrushes.Black, r);
        });
    }
}
