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

    public static IReadOnlyList<Opcion<Rol>> Roles { get; } =
        Enum.GetValues<Rol>().Select(v => new Opcion<Rol>(v, v.Texto())).ToList();

    public static IReadOnlyList<Opcion<TipoProducto>> TiposProducto { get; } =
        Enum.GetValues<TipoProducto>().Select(v => new Opcion<TipoProducto>(v, v.Texto())).ToList();

    public static IReadOnlyList<Opcion<UnidadMedida>> UnidadesMedida { get; } =
        Enum.GetValues<UnidadMedida>().Select(v => new Opcion<UnidadMedida>(v, v.Texto())).ToList();

    /// <summary>Alícuotas ordenadas por porcentaje (21% primero porque es la más usada).</summary>
    public static IReadOnlyList<Opcion<AlicuotaIva>> Alicuotas { get; } =
        new[] { AlicuotaIva.Veintiuno, AlicuotaIva.DiezCinco, AlicuotaIva.VeintiSiete, AlicuotaIva.Cinco, AlicuotaIva.DosCinco, AlicuotaIva.Cero }
            .Select(v => new Opcion<AlicuotaIva>(v, v.Texto())).ToList();

    /// <summary>Condiciones posibles para el emisor (la estación).</summary>
    public static IReadOnlyList<Opcion<CondicionIva>> CondicionesEmisor { get; } =
        new[] { CondicionIva.ResponsableInscripto, CondicionIva.Monotributo, CondicionIva.Exento }
            .Select(v => new Opcion<CondicionIva>(v, v.Texto())).ToList();

    public static IReadOnlyList<Opcion<EntornoArca>> Entornos { get; } =
        Enum.GetValues<EntornoArca>().Select(v => new Opcion<EntornoArca>(v, v.Texto())).ToList();
}
