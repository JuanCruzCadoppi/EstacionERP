namespace EstacionERP.Domain.Validaciones;

/// <summary>
/// Validaciones de CUIT/CUIL/DNI argentinos.
/// </summary>
public static class DocumentoValidador
{
    private static readonly int[] Pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
    private static readonly string[] PrefijosValidos = { "20", "23", "24", "27", "30", "33", "34" };

    /// <summary>Deja solo los dígitos (quita guiones, puntos y espacios).</summary>
    public static string SoloDigitos(string? valor) =>
        new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>
    /// Valida un CUIT/CUIL: 11 dígitos, prefijo válido y dígito verificador correcto.
    /// </summary>
    public static bool CuitEsValido(string? cuit)
    {
        var d = SoloDigitos(cuit);
        if (d.Length != 11) return false;
        if (!PrefijosValidos.Contains(d[..2])) return false;

        var suma = 0;
        for (var i = 0; i < 10; i++)
            suma += (d[i] - '0') * Pesos[i];

        var resto = suma % 11;
        var verificador = resto switch
        {
            0 => 0,
            1 => -1, // combinación inválida para ese prefijo
            _ => 11 - resto
        };

        return verificador >= 0 && verificador == d[10] - '0';
    }

    /// <summary>DNI: 7 u 8 dígitos.</summary>
    public static bool DniEsValido(string? dni)
    {
        var d = SoloDigitos(dni);
        return d.Length is 7 or 8 && d.TrimStart('0').Length > 0;
    }

    /// <summary>Formatea 20123456786 como 20-12345678-6.</summary>
    public static string FormatearCuit(string? cuit)
    {
        var d = SoloDigitos(cuit);
        return d.Length == 11 ? $"{d[..2]}-{d[2..10]}-{d[10]}" : d;
    }
}
