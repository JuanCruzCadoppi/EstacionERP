using EstacionERP.Application.Common;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Impresion;

public record DatosImpresion(
    ComprobanteImprimible Comprobante,
    FormatoImpresion Formato,
    string? Impresora,
    bool ImprimirAlEmitir,
    byte[] Pdf);

public interface IImpresionService
{
    /// <summary>Genera el PDF del comprobante en el formato del punto de venta (o en el indicado).</summary>
    Task<Resultado<DatosImpresion>> PrepararAsync(int comprobanteId, FormatoImpresion? formato = null, CancellationToken ct = default);
}

public class ImpresionService : IImpresionService
{
    private readonly IEstacionDbContext _db;
    private readonly ISesionActual _sesion;
    private readonly IGeneradorComprobantePdf _generador;

    public ImpresionService(IEstacionDbContext db, ISesionActual sesion, IGeneradorComprobantePdf generador)
    {
        _db = db;
        _sesion = sesion;
        _generador = generador;
    }

    public async Task<Resultado<DatosImpresion>> PrepararAsync(int comprobanteId, FormatoImpresion? formato = null, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.Facturar))
            return Resultado<DatosImpresion>.Error("No tenés permiso para imprimir comprobantes.");

        var c = await _db.Comprobantes.AsNoTracking()
            .Include(x => x.Items).Include(x => x.Alicuotas).Include(x => x.PuntoVenta)
            .FirstOrDefaultAsync(x => x.Id == comprobanteId, ct);
        if (c is null || !_sesion.TieneAcceso(c.UnidadNegocioId))
            return Resultado<DatosImpresion>.Error("No se encontró el comprobante.");
        if (c.Estado != EstadoComprobante.Autorizado)
            return Resultado<DatosImpresion>.Error("Solo se pueden imprimir comprobantes autorizados por ARCA (con CAE).");

        var emisor = await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct);
        if (emisor is null)
            return Resultado<DatosImpresion>.Error("Faltan los datos fiscales de la empresa.");

        var imprimible = ArmadorImpresion.Armar(c, emisor);
        var f = formato ?? c.PuntoVenta?.FormatoImpresion ?? FormatoImpresion.A4;
        var pdf = _generador.Generar(imprimible, f);

        return Resultado<DatosImpresion>.Ok(new DatosImpresion(
            imprimible, f,
            string.IsNullOrWhiteSpace(c.PuntoVenta?.Impresora) ? null : c.PuntoVenta!.Impresora,
            c.PuntoVenta?.ImprimirAlEmitir ?? true,
            pdf));
    }
}
