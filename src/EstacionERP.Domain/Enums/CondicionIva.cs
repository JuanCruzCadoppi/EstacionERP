namespace EstacionERP.Domain.Enums;

/// <summary>
/// Condición frente al IVA del receptor. Los valores coinciden con los códigos de ARCA
/// (FEParamGetCondicionIvaReceptor).
/// </summary>
public enum CondicionIva
{
    ResponsableInscripto = 1,
    Exento = 4,
    ConsumidorFinal = 5,
    Monotributo = 6,
    NoCategorizado = 7,
    MonotributistaSocial = 13,
    NoAlcanzado = 15
}
