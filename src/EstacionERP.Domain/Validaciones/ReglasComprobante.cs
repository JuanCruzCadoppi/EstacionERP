using EstacionERP.Domain.Enums;

namespace EstacionERP.Domain.Validaciones;

/// <summary>Reglas fiscales para elegir el tipo de comprobante.</summary>
public static class ReglasComprobante
{
    /// <summary>
    /// Desde este monto el consumidor final tiene que identificarse (DNI/CUIL/CUIT).
    /// Valor vigente según la normativa de ARCA; revisar si cambia.
    /// </summary>
    public const decimal MontoIdentificacionConsumidorFinal = 10_000_000m;

    /// <summary>
    /// Responsable Inscripto: A a inscriptos y monotributistas, B al resto.
    /// Monotributista o exento: siempre C.
    /// </summary>
    public static TipoComprobante TipoFactura(CondicionIva emisor, CondicionIva receptor)
    {
        if (emisor != CondicionIva.ResponsableInscripto)
            return TipoComprobante.FacturaC;

        return receptor is CondicionIva.ResponsableInscripto or CondicionIva.Monotributo or CondicionIva.MonotributistaSocial
            ? TipoComprobante.FacturaA
            : TipoComprobante.FacturaB;
    }

    public static TipoComprobante NotaCreditoPara(TipoComprobante factura) => Letra(factura) switch
    {
        'A' => TipoComprobante.NotaCreditoA,
        'B' => TipoComprobante.NotaCreditoB,
        _ => TipoComprobante.NotaCreditoC
    };

    public static char Letra(TipoComprobante t) => t switch
    {
        TipoComprobante.FacturaA or TipoComprobante.NotaDebitoA or TipoComprobante.NotaCreditoA => 'A',
        TipoComprobante.FacturaB or TipoComprobante.NotaDebitoB or TipoComprobante.NotaCreditoB => 'B',
        _ => 'C'
    };

    /// <summary>En A y B se discrimina IVA ante ARCA; en C no.</summary>
    public static bool DiscriminaIva(TipoComprobante t) => Letra(t) != 'C';

    public static bool EsNotaCredito(TipoComprobante t) =>
        t is TipoComprobante.NotaCreditoA or TipoComprobante.NotaCreditoB or TipoComprobante.NotaCreditoC;
}
