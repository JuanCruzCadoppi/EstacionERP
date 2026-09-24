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
}
