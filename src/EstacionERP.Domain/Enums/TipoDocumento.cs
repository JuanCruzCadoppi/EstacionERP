namespace EstacionERP.Domain.Enums;

/// <summary>
/// Tipos de documento. Los valores coinciden con los códigos de ARCA (FEParamGetTiposDoc).
/// </summary>
public enum TipoDocumento
{
    Cuit = 80,
    Cuil = 86,
    Dni = 96,
    SinIdentificar = 99
}
