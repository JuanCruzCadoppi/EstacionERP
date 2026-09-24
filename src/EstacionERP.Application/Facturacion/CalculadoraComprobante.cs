using EstacionERP.Domain.Enums;
using EstacionERP.Domain.Validaciones;

namespace EstacionERP.Application.Facturacion;

/// <summary>Línea a calcular. El precio unitario es FINAL (con IVA incluido).</summary>
public record LineaCalculo(decimal Cantidad, decimal PrecioUnitario, AlicuotaIva Alicuota, bool EsServicio);

public record LineaCalculada(decimal Neto, decimal Iva, decimal Total);

public record SubtotalIva(AlicuotaIva Alicuota, decimal BaseImponible, decimal Importe);

public record ResultadoCalculo(
    IReadOnlyList<LineaCalculada> Lineas,
    IReadOnlyList<SubtotalIva> Alicuotas,
    decimal Neto,
    decimal Iva,
    decimal Total,
    ConceptoComprobante Concepto);

/// <summary>
/// Calcula importes de un comprobante como los pide ARCA.
/// El IVA se calcula POR ALÍCUOTA sobre el total del grupo (no ítem por ítem) para que
/// base × alícuota coincida con el importe informado y ARCA no lo rechace por redondeo.
/// </summary>
public static class CalculadoraComprobante
{
    public static ResultadoCalculo Calcular(IReadOnlyList<LineaCalculo> lineas, TipoComprobante tipo)
    {
        var discrimina = ReglasComprobante.DiscriminaIva(tipo);

        var totales = lineas.Select(l => Redondear(l.Cantidad * l.PrecioUnitario)).ToList();
        var total = totales.Sum();

        var concepto = lineas.Count == 0 || lineas.All(l => !l.EsServicio)
            ? ConceptoComprobante.Productos
            : lineas.All(l => l.EsServicio) ? ConceptoComprobante.Servicios : ConceptoComprobante.ProductosYServicios;

        if (!discrimina)
        {
            // Factura C: no se discrimina IVA; todo es "neto" para ARCA.
            var lineasC = totales.Select(t => new LineaCalculada(t, 0m, t)).ToList();
            return new ResultadoCalculo(lineasC, Array.Empty<SubtotalIva>(), total, 0m, total, concepto);
        }

        // Subtotales por alícuota.
        var alicuotas = lineas.Select((l, i) => (l.Alicuota, Total: totales[i]))
            .GroupBy(x => x.Alicuota)
            .OrderBy(g => g.Key.Porcentaje())
            .Select(g =>
            {
                var totalGrupo = g.Sum(x => x.Total);
                var baseImp = Redondear(totalGrupo / (1 + g.Key.Porcentaje() / 100m));
                return new SubtotalIva(g.Key, baseImp, totalGrupo - baseImp);
            })
            .ToList();

        // Detalle por línea (solo para mostrar / imprimir).
        var detalle = lineas.Select((l, i) =>
        {
            var neto = Redondear(totales[i] / (1 + l.Alicuota.Porcentaje() / 100m));
            return new LineaCalculada(neto, totales[i] - neto, totales[i]);
        }).ToList();

        return new ResultadoCalculo(
            detalle,
            alicuotas,
            alicuotas.Sum(a => a.BaseImponible),
            alicuotas.Sum(a => a.Importe),
            total,
            concepto);
    }

    public static decimal Redondear(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
