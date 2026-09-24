using EstacionERP.Domain.Enums;

namespace EstacionERP.Desktop.Comun;

/// <summary>Elemento para ComboBox: valor + texto a mostrar.</summary>
public record Opcion<T>(T Valor, string Texto)
{
    public override string ToString() => Texto;
}

public static class Opciones
{
    public static IReadOnlyList<Opcion<TipoDocumento>> TiposDocumento { get; } =
        Enum.GetValues<TipoDocumento>().Select(v => new Opcion<TipoDocumento>(v, v.Texto())).ToList();

    public static IReadOnlyList<Opcion<CondicionIva>> CondicionesIva { get; } =
        Enum.GetValues<CondicionIva>().Select(v => new Opcion<CondicionIva>(v, v.Texto())).ToList();
}
