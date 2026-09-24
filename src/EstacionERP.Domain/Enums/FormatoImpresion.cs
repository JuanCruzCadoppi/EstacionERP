namespace EstacionERP.Domain.Enums;

/// <summary>Formato en que se imprime el comprobante en cada punto de venta.</summary>
public enum FormatoImpresion
{
    /// <summary>Hoja A4 (impresora común).</summary>
    A4 = 1,
    /// <summary>Ticket de 80 mm (impresora térmica).</summary>
    Ticket80 = 2
}
