using System.Reflection;
using PdfSharp.Fonts;

namespace EstacionERP.Infrastructure.Impresion;

/// <summary>
/// Tipografía incluida en el programa (Liberation Sans, licencia SIL OFL 1.1) para que la
/// factura se vea igual en cualquier PC, sin depender de las fuentes instaladas.
/// </summary>
internal sealed class FuentesPdf : IFontResolver
{
    public const string Familia = "EstacionSans";
    private const string Regular = "EstacionSans-Regular";
    private const string Negrita = "EstacionSans-Bold";

    private static readonly object Candado = new();
    private static bool _registrada;

    public static void Registrar()
    {
        lock (Candado)
        {
            if (_registrada) return;
            if (GlobalFontSettings.FontResolver is not FuentesPdf)
                GlobalFontSettings.FontResolver = new FuentesPdf();
            _registrada = true;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(bold ? Negrita : Regular);

    public byte[]? GetFont(string faceName)
    {
        var archivo = faceName == Negrita ? "LiberationSans-Bold.ttf" : "LiberationSans-Regular.ttf";
        var asm = typeof(FuentesPdf).Assembly;
        var nombre = asm.GetManifestResourceNames().First(n => n.EndsWith(archivo, StringComparison.Ordinal));
        using var s = asm.GetManifestResourceStream(nombre)!;
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }
}
