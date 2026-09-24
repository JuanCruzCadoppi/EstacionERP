using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace EstacionERP.Desktop.Impresion;

/// <summary>
/// Imprime el PDF del comprobante en una impresora de Windows (común o térmica).
/// El PDF se convierte a imagen de alta resolución con el lector de PDF que trae Windows 10/11.
/// </summary>
public static class ImpresoraWindows
{
    private const int Dpi = 300;

    /// <summary>Impresoras instaladas en esta PC (locales y de red).</summary>
    public static List<string> Instaladas()
    {
        try
        {
            using var servidor = new LocalPrintServer();
            return servidor.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
                .Select(q => q.FullName).Distinct().OrderBy(n => n).ToList();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }

    /// <summary>Imprime y devuelve el nombre de la impresora usada.</summary>
    public static async Task<string> ImprimirAsync(byte[] pdf, string? impresora, string titulo)
    {
        var paginas = await RenderizarAsync(pdf);

        using var servidor = new LocalPrintServer();
        var cola = Buscar(servidor, impresora) ?? Predeterminada(servidor)
                   ?? throw new InvalidOperationException("No hay ninguna impresora instalada en esta PC.");

        var documento = new FixedDocument();
        foreach (var (imagen, ancho, alto) in paginas)
        {
            var hoja = new FixedPage { Width = ancho, Height = alto, Background = Brushes.White };
            hoja.Children.Add(new Image { Source = imagen, Width = ancho, Height = alto, Stretch = Stretch.Fill });
            var contenido = new PageContent();
            ((IAddChild)contenido).AddChild(hoja);
            documento.Pages.Add(contenido);
        }
        var (_, anchoPag, altoPag) = paginas[0];
        documento.DocumentPaginator.PageSize = new Size(anchoPag, altoPag);

        var ticket = cola.DefaultPrintTicket.Clone();
        ticket.PageMediaSize = new PageMediaSize(anchoPag, altoPag);
        ticket.PageOrientation = PageOrientation.Portrait;
        ticket.CopyCount = 1;

        var escritor = PrintQueue.CreateXpsDocumentWriter(cola);
        escritor.Write(documento, ticket);
        return cola.FullName;
    }

    /// <summary>Guarda el PDF en una carpeta temporal y lo abre con el visor predeterminado.</summary>
    public static void Abrir(byte[] pdf, string nombreArchivo)
    {
        var carpeta = Path.Combine(Path.GetTempPath(), "EstacionERP");
        Directory.CreateDirectory(carpeta);
        var ruta = Path.Combine(carpeta, nombreArchivo);
        File.WriteAllBytes(ruta, pdf);
        Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
    }

    private static PrintQueue? Buscar(LocalPrintServer servidor, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;
        try
        {
            return servidor.GetPrintQueues(new[] { EnumeratedPrintQueueTypes.Local, EnumeratedPrintQueueTypes.Connections })
                .FirstOrDefault(q => string.Equals(q.FullName, nombre, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(q.Name, nombre, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No se encontró la impresora \"{nombre}\" en esta PC. Revisá la configuración del punto de venta.");
        }
        catch (PrintQueueException)
        {
            return null;
        }
    }

    private static PrintQueue? Predeterminada(LocalPrintServer servidor)
    {
        try
        {
            return servidor.DefaultPrintQueue;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Convierte cada página del PDF en imagen (tamaño en unidades WPF de 1/96").</summary>
    private static async Task<List<(BitmapSource Imagen, double Ancho, double Alto)>> RenderizarAsync(byte[] pdf)
    {
        using var entrada = new InMemoryRandomAccessStream();
        using (var escritor = new DataWriter(entrada))
        {
            escritor.WriteBytes(pdf);
            await escritor.StoreAsync();
            await escritor.FlushAsync();
            escritor.DetachStream();
        }
        entrada.Seek(0);

        var doc = await PdfDocument.LoadFromStreamAsync(entrada);
        var resultado = new List<(BitmapSource, double, double)>();
        for (uint i = 0; i < doc.PageCount; i++)
        {
            using var pagina = doc.GetPage(i);
            var tam = pagina.Size;   // en DIPs (1/96 de pulgada)
            using var salida = new InMemoryRandomAccessStream();
            await pagina.RenderToStreamAsync(salida, new PdfPageRenderOptions
            {
                DestinationWidth = (uint)Math.Round(tam.Width * Dpi / 96.0),
                DestinationHeight = (uint)Math.Round(tam.Height * Dpi / 96.0)
            });

            salida.Seek(0);
            var ms = new MemoryStream();
            await salida.AsStreamForRead().CopyToAsync(ms);
            ms.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            resultado.Add((bmp, tam.Width, tam.Height));
        }

        if (resultado.Count == 0) throw new InvalidOperationException("El PDF del comprobante no tiene páginas.");
        return resultado;
    }
}
