namespace EstacionERP.Domain.Enums;

/// <summary>
/// Textos legibles para mostrar los enums en pantalla.
/// </summary>
public static class Descripciones
{
    public static string Texto(this TipoDocumento v) => v switch
    {
        TipoDocumento.Cuit => "CUIT",
        TipoDocumento.Cuil => "CUIL",
        TipoDocumento.Dni => "DNI",
        TipoDocumento.SinIdentificar => "Sin identificar",
        _ => v.ToString()
    };

    public static string Texto(this CondicionIva v) => v switch
    {
        CondicionIva.ResponsableInscripto => "IVA Responsable Inscripto",
        CondicionIva.Exento => "IVA Sujeto Exento",
        CondicionIva.ConsumidorFinal => "Consumidor Final",
        CondicionIva.Monotributo => "Responsable Monotributo",
        CondicionIva.NoCategorizado => "Sujeto No Categorizado",
        CondicionIva.MonotributistaSocial => "Monotributista Social",
        CondicionIva.NoAlcanzado => "IVA No Alcanzado",
        _ => v.ToString()
    };

    public static string Texto(this AlicuotaIva v) => v switch
    {
        AlicuotaIva.Cero => "0%",
        AlicuotaIva.DosCinco => "2,5%",
        AlicuotaIva.Cinco => "5%",
        AlicuotaIva.DiezCinco => "10,5%",
        AlicuotaIva.Veintiuno => "21%",
        AlicuotaIva.VeintiSiete => "27%",
        _ => v.ToString()
    };

    /// <summary>Porcentaje numérico de la alícuota (ej. 21 para 21%).</summary>
    public static decimal Porcentaje(this AlicuotaIva v) => v switch
    {
        AlicuotaIva.Cero => 0m,
        AlicuotaIva.DosCinco => 2.5m,
        AlicuotaIva.Cinco => 5m,
        AlicuotaIva.DiezCinco => 10.5m,
        AlicuotaIva.Veintiuno => 21m,
        AlicuotaIva.VeintiSiete => 27m,
        _ => throw new ArgumentOutOfRangeException(nameof(v))
    };

    public static string Texto(this Rol v) => v switch
    {
        Rol.Administrador => "Administrador",
        Rol.Encargado => "Encargado",
        Rol.Operador => "Operador",
        _ => v.ToString()
    };

    public static string Texto(this TipoProducto v) => v switch
    {
        TipoProducto.Bien => "Artículo",
        TipoProducto.Servicio => "Servicio",
        TipoProducto.Combustible => "Combustible",
        _ => v.ToString()
    };

    public static string Texto(this UnidadMedida v) => v switch
    {
        UnidadMedida.Unidad => "Unidad",
        UnidadMedida.Litro => "Litro",
        UnidadMedida.Kilogramo => "Kilogramo",
        UnidadMedida.Metro => "Metro",
        _ => v.ToString()
    };

    public static string Texto(this TipoComprobante v) => v switch
    {
        TipoComprobante.FacturaA => "Factura A",
        TipoComprobante.NotaDebitoA => "Nota de Débito A",
        TipoComprobante.NotaCreditoA => "Nota de Crédito A",
        TipoComprobante.FacturaB => "Factura B",
        TipoComprobante.NotaDebitoB => "Nota de Débito B",
        TipoComprobante.NotaCreditoB => "Nota de Crédito B",
        TipoComprobante.FacturaC => "Factura C",
        TipoComprobante.NotaDebitoC => "Nota de Débito C",
        TipoComprobante.NotaCreditoC => "Nota de Crédito C",
        _ => v.ToString()
    };

    public static string Texto(this EstadoComprobante v) => v switch
    {
        EstadoComprobante.Pendiente => "Pendiente",
        EstadoComprobante.Autorizado => "Autorizado",
        EstadoComprobante.Rechazado => "Rechazado",
        _ => v.ToString()
    };

    public static string Texto(this EntornoArca v) => v switch
    {
        EntornoArca.Homologacion => "Homologación (pruebas)",
        EntornoArca.Produccion => "Producción",
        _ => v.ToString()
    };

    public static string Texto(this FormatoImpresion v) => v switch
    {
        FormatoImpresion.A4 => "Hoja A4",
        FormatoImpresion.Ticket80 => "Ticket 80 mm",
        _ => v.ToString()
    };

    public static string TextoCorto(this TipoDocumento v) => v switch
    {
        TipoDocumento.Cuit => "CUIT",
        TipoDocumento.Cuil => "CUIL",
        TipoDocumento.Dni => "DNI",
        _ => "Doc."
    };
}
