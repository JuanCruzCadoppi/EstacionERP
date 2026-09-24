using EstacionERP.Domain.Enums;

namespace EstacionERP.Infrastructure.Arca;

/// <summary>Direcciones oficiales de los web services de ARCA.</summary>
public static class ArcaEndpoints
{
    public static string Wsaa(EntornoArca e) => e == EntornoArca.Produccion
        ? "https://wsaa.afip.gov.ar/ws/services/LoginCms"
        : "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

    public static string Wsfe(EntornoArca e) => e == EntornoArca.Produccion
        ? "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
        : "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
}
