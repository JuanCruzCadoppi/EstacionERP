using EstacionERP.Application.Common;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Productos;

public interface IProductoService
{
    Task<List<ProductoResumen>> BuscarAsync(string? texto, int? unidadNegocioId, bool incluirInactivos = false, CancellationToken ct = default);
    Task<ProductoDatos?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<Resultado<int>> GuardarAsync(ProductoDatos datos, CancellationToken ct = default);
    Task<int> ContarParaAumentoAsync(FiltroAumento filtro, CancellationToken ct = default);
    Task<Resultado<int>> AumentarPreciosAsync(FiltroAumento filtro, decimal porcentaje, bool tambienCosto, CancellationToken ct = default);
}

public class ProductoService : IProductoService
{
    private readonly IEstacionDbContext _db;
    private readonly ISesionActual _sesion;

    public ProductoService(IEstacionDbContext db, ISesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    /// <summary>Unidades a las que el usuario tiene acceso.</summary>
    private IReadOnlyList<int> UnidadesPermitidas => _sesion.Usuario?.UnidadesNegocioIds ?? Array.Empty<int>();

    public async Task<List<ProductoResumen>> BuscarAsync(string? texto, int? unidadNegocioId, bool incluirInactivos = false, CancellationToken ct = default)
    {
        var permitidas = UnidadesPermitidas.ToList();
        var q = _db.Productos.AsNoTracking().Include(p => p.UnidadNegocio)
            .Where(p => permitidas.Contains(p.UnidadNegocioId));

        if (unidadNegocioId is not null) q = q.Where(p => p.UnidadNegocioId == unidadNegocioId);
        if (!incluirInactivos) q = q.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim().ToUpper();
            q = q.Where(p =>
                p.Codigo.ToUpper().Contains(t) ||
                p.Descripcion.ToUpper().Contains(t) ||
                (p.CodigoBarras != null && p.CodigoBarras == t) ||
                (p.CodigoFabricante != null && p.CodigoFabricante.ToUpper().Contains(t)) ||
                (p.Marca != null && p.Marca.ToUpper().Contains(t)));
        }

        var lista = await q.OrderBy(p => p.Descripcion).Take(1000).ToListAsync(ct);
        return lista.Select(p => new ProductoResumen(
            p.Id, p.Codigo, p.Descripcion, p.UnidadNegocioId, p.UnidadNegocio!.Nombre,
            p.Tipo.Texto(), p.Marca, p.Rubro, p.AlicuotaIva.Texto(),
            p.Costo, p.PrecioVenta, Margen(p.Costo, p.PrecioVenta, p.AlicuotaIva), p.Activo)).ToList();
    }

    public async Task<ProductoDatos?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null || !_sesion.TieneAcceso(p.UnidadNegocioId)) return null;

        return new ProductoDatos
        {
            Id = p.Id, Codigo = p.Codigo, CodigoBarras = p.CodigoBarras, CodigoFabricante = p.CodigoFabricante,
            Descripcion = p.Descripcion, Marca = p.Marca, Rubro = p.Rubro, UnidadNegocioId = p.UnidadNegocioId,
            Tipo = p.Tipo, UnidadMedida = p.UnidadMedida, AlicuotaIva = p.AlicuotaIva, Costo = p.Costo,
            PrecioVenta = p.PrecioVenta, ControlaStock = p.ControlaStock, StockMinimo = p.StockMinimo, Activo = p.Activo
        };
    }

    public async Task<Resultado<int>> GuardarAsync(ProductoDatos d, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.EditarProductos))
            return Resultado<int>.Error("No tenés permiso para modificar productos.");
        if (!_sesion.TieneAcceso(d.UnidadNegocioId))
            return Resultado<int>.Error("No tenés acceso a esa unidad de negocio.");

        Normalizar(d);
        var errores = Validar(d);

        if (await _db.Productos.AnyAsync(p => p.Id != d.Id && p.UnidadNegocioId == d.UnidadNegocioId && p.Codigo == d.Codigo, ct))
            errores.Add($"Ya existe un producto con el código {d.Codigo} en esta unidad de negocio.");

        Producto producto;
        if (d.Id == 0)
        {
            producto = new Producto();
        }
        else
        {
            producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == d.Id, ct)
                       ?? throw new InvalidOperationException($"No existe el producto {d.Id}.");
            if (!_sesion.TieneAcceso(producto.UnidadNegocioId))
                errores.Add("No tenés acceso a la unidad de negocio de este producto.");
            if ((producto.PrecioVenta != d.PrecioVenta || producto.Costo != d.Costo) && !_sesion.Puede(Permiso.ModificarPrecios))
                errores.Add("No tenés permiso para modificar precios.");
        }

        if (errores.Count > 0) return Resultado<int>.Error(errores.ToArray());
        if (d.Id == 0) _db.Productos.Add(producto);
        else producto.ModificadoEn = DateTime.UtcNow;

        producto.Codigo = d.Codigo;
        producto.CodigoBarras = d.CodigoBarras;
        producto.CodigoFabricante = d.CodigoFabricante;
        producto.Descripcion = d.Descripcion;
        producto.Marca = d.Marca;
        producto.Rubro = d.Rubro;
        producto.UnidadNegocioId = d.UnidadNegocioId;
        producto.Tipo = d.Tipo;
        producto.UnidadMedida = d.UnidadMedida;
        producto.AlicuotaIva = d.AlicuotaIva;
        producto.Costo = d.Costo;
        producto.PrecioVenta = d.PrecioVenta;
        producto.ControlaStock = d.ControlaStock;
        producto.StockMinimo = d.StockMinimo;
        producto.Activo = d.Activo;

        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(producto.Id);
    }

    public async Task<int> ContarParaAumentoAsync(FiltroAumento filtro, CancellationToken ct = default) =>
        await ConsultaAumento(filtro).CountAsync(ct);

    public async Task<Resultado<int>> AumentarPreciosAsync(FiltroAumento filtro, decimal porcentaje, bool tambienCosto, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.ModificarPrecios))
            return Resultado<int>.Error("No tenés permiso para modificar precios.");
        if (porcentaje == 0 || porcentaje < -90 || porcentaje > 500)
            return Resultado<int>.Error("El porcentaje debe ser distinto de 0 y estar entre -90% y 500%.");

        var productos = await ConsultaAumento(filtro).ToListAsync(ct);
        var factor = 1 + porcentaje / 100m;
        var ahora = DateTime.UtcNow;

        foreach (var p in productos)
        {
            p.PrecioVenta = Math.Round(p.PrecioVenta * factor, 2, MidpointRounding.AwayFromZero);
            if (tambienCosto)
                p.Costo = Math.Round(p.Costo * factor, 4, MidpointRounding.AwayFromZero);
            p.ModificadoEn = ahora;
        }

        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(productos.Count);
    }

    // ---------------------------------------------------------------------

    private IQueryable<Producto> ConsultaAumento(FiltroAumento f)
    {
        var permitidas = UnidadesPermitidas.ToList();
        var q = _db.Productos.Where(p => p.Activo && permitidas.Contains(p.UnidadNegocioId));
        if (f.UnidadNegocioId is not null) q = q.Where(p => p.UnidadNegocioId == f.UnidadNegocioId);
        if (!string.IsNullOrWhiteSpace(f.Rubro)) { var r = f.Rubro.Trim().ToUpper(); q = q.Where(p => p.Rubro == r); }
        if (!string.IsNullOrWhiteSpace(f.Marca)) { var m = f.Marca.Trim().ToUpper(); q = q.Where(p => p.Marca == m); }
        return q;
    }

    /// <summary>Margen sobre costo: (precio sin IVA - costo) / costo.</summary>
    public static decimal? Margen(decimal costo, decimal precioFinal, AlicuotaIva alicuota)
    {
        if (costo <= 0) return null;
        var neto = precioFinal / (1 + alicuota.Porcentaje() / 100m);
        return Math.Round((neto - costo) / costo * 100m, 1);
    }

    private static void Normalizar(ProductoDatos d)
    {
        d.Codigo = (d.Codigo ?? string.Empty).Trim().ToUpper();
        d.Descripcion = (d.Descripcion ?? string.Empty).Trim().ToUpper();
        d.Marca = Limpiar(d.Marca)?.ToUpper();
        d.Rubro = Limpiar(d.Rubro)?.ToUpper();
        d.CodigoBarras = Limpiar(d.CodigoBarras);
        d.CodigoFabricante = Limpiar(d.CodigoFabricante)?.ToUpper();

        // Reglas por tipo.
        if (d.Tipo == TipoProducto.Servicio)
        {
            d.ControlaStock = false;
            d.StockMinimo = 0;
        }
        if (d.Tipo == TipoProducto.Combustible)
        {
            d.UnidadMedida = UnidadMedida.Litro;
            d.ControlaStock = true;
        }
    }

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static List<string> Validar(ProductoDatos d)
    {
        var e = new List<string>();
        if (d.Codigo.Length == 0) e.Add("El código es obligatorio.");
        else if (d.Codigo.Length > 30) e.Add("El código no puede superar los 30 caracteres.");
        if (d.Descripcion.Length == 0) e.Add("La descripción es obligatoria.");
        if (d.UnidadNegocioId == 0) e.Add("Elegí la unidad de negocio.");
        if (d.PrecioVenta < 0) e.Add("El precio de venta no puede ser negativo.");
        if (d.Costo < 0) e.Add("El costo no puede ser negativo.");
        if (d.StockMinimo < 0) e.Add("El stock mínimo no puede ser negativo.");
        if (d.Tipo == TipoProducto.Combustible && d.UnidadNegocioId != UnidadNegocio.PlayaId)
            e.Add("Los combustibles pertenecen a la unidad Playa / Combustibles.");
        return e;
    }
}
