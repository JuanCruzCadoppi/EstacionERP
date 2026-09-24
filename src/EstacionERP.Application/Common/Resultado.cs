namespace EstacionERP.Application.Common;

/// <summary>
/// Resultado de una operación: éxito o lista de errores para mostrar al usuario.
/// </summary>
public class Resultado
{
    public bool Exito => Errores.Count == 0;
    public List<string> Errores { get; } = new();

    public static Resultado Ok() => new();

    public static Resultado Error(params string[] errores)
    {
        var r = new Resultado();
        r.Errores.AddRange(errores);
        return r;
    }
}

public class Resultado<T> : Resultado
{
    public T? Valor { get; private init; }

    public static Resultado<T> Ok(T valor) => new() { Valor = valor };

    public static new Resultado<T> Error(params string[] errores)
    {
        var r = new Resultado<T>();
        r.Errores.AddRange(errores);
        return r;
    }
}
