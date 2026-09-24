using EstacionERP.Application.Common;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Domain.Validaciones;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Facturacion;

public interface IFacturacionService
{
    /// <summary>Tipo de factura que corresponde a un cliente según la condición del emisor.</summary>
    Task<TipoComprobante?> TipoParaClienteAsync(int clienteId, CancellationToken ct = default);
    Task<Resultado<ComprobanteEmitido>> EmitirAsync(SolicitudEmision solicitud, CancellationToken ct = default);
    Task<Resultado<ComprobanteEmitido>> ReintentarAsync(int comprobanteId, CancellationToken ct = default);
    Task<ResultadoReintentos> ReintentarPendientesAsync(CancellationToken ct = default);
    Task<List<ComprobanteResumen>> ListarAsync(DateTime desde, DateTime hasta, int? unidadNegocioId = null, CancellationToken ct = default);
}

/// <summary>
/// Emisión de comprobantes electrónicos.
///
/// Flujo: se guarda PENDIENTE → se pide el último número a ARCA → se pide el CAE con número+1.
/// Si se corta la conexión queda PENDIENTE con el número intentado; al reintentar primero se
/// consulta a ARCA si ese número llegó a autorizarse, para no duplicar comprobantes.
/// </summary>
public class FacturacionService : IFacturacionService
{
    /// <summary>ARCA: "El número de comprobante no es el próximo a autorizar".</summary>
    public const int ErrorNumeroNoCorrelativo = 10016;
    private const int MaximoIntentosNumeracion = 3;

    private readonly IEstacionDbContext _db;
    private readonly ISesionActual _sesion;
    private readonly IArcaClient _arca;
    private readonly Func<DateTime> _hoy;

    public FacturacionService(IEstacionDbContext db, ISesionActual sesion, IArcaClient arca)
        : this(db, sesion, arca, () => DateTime.Today) { }

    /// <summary>Constructor para tests (permite fijar la fecha).</summary>
    public FacturacionService(IEstacionDbContext db, ISesionActual sesion, IArcaClient arca, Func<DateTime> hoy)
    {
        _db = db;
        _sesion = sesion;
        _arca = arca;
        _hoy = hoy;
    }

    public async Task<TipoComprobante?> TipoParaClienteAsync(int clienteId, CancellationToken ct = default)
    {
        var config = await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct);
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId, ct);
        if (config is null || cliente is null) return null;
        return ReglasComprobante.TipoFactura(config.CondicionIva, cliente.CondicionIva);
    }

    public async Task<Resultado<ComprobanteEmitido>> EmitirAsync(SolicitudEmision s, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.Facturar))
            return Resultado<ComprobanteEmitido>.Error("No tenés permiso para facturar.");
        if (!_sesion.TieneAcceso(s.UnidadNegocioId))
            return Resultado<ComprobanteEmitido>.Error("No tenés acceso a esa unidad de negocio.");

        var config = await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct);
        if (config is null || string.IsNullOrEmpty(config.Cuit))
            return Resultado<ComprobanteEmitido>.Error("Falta cargar los datos fiscales de la empresa (Administración → Configuración fiscal).");
        if (!config.TieneCertificado)
            return Resultado<ComprobanteEmitido>.Error("Falta el certificado digital de ARCA (Administración → Configuración fiscal).");

        var pv = await _db.PuntosVenta.AsNoTracking().FirstOrDefaultAsync(p => p.Id == s.PuntoVentaId, ct);
        if (pv is null || !pv.Activo || pv.UnidadNegocioId != s.UnidadNegocioId)
            return Resultado<ComprobanteEmitido>.Error("El punto de venta no es válido para esta unidad de negocio.");

        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == s.ClienteId, ct);
        if (cliente is null || !cliente.Activo)
            return Resultado<ComprobanteEmitido>.Error("El cliente no existe o está desactivado.");

        var errores = ValidarItems(s.Items);
        if (errores.Count > 0) return Resultado<ComprobanteEmitido>.Error(errores.ToArray());

        var tipo = ReglasComprobante.TipoFactura(config.CondicionIva, cliente.CondicionIva);
        var lineas = s.Items.Select(i => new LineaCalculo(i.Cantidad, i.PrecioUnitario, i.AlicuotaIva, i.EsServicio)).ToList();
        var calculo = CalculadoraComprobante.Calcular(lineas, tipo);

        if (calculo.Total <= 0)
            errores.Add("El total del comprobante tiene que ser mayor a cero.");
        if (ReglasComprobante.Letra(tipo) == 'A' && cliente.TipoDocumento != TipoDocumento.Cuit)
            errores.Add("Para Factura A el cliente tiene que tener CUIT.");
        if (cliente.TipoDocumento == TipoDocumento.SinIdentificar && calculo.Total >= ReglasComprobante.MontoIdentificacionConsumidorFinal)
            errores.Add($"Desde {ReglasComprobante.MontoIdentificacionConsumidorFinal:C0} el consumidor final tiene que identificarse con DNI, CUIL o CUIT.");
        if (errores.Count > 0) return Resultado<ComprobanteEmitido>.Error(errores.ToArray());

        var hoy = _hoy().Date;
        var c = new Comprobante
        {
            UnidadNegocioId = s.UnidadNegocioId,
            PuntoVentaId = pv.Id,
            PuntoVentaNumero = pv.Numero,
            Tipo = tipo,
            Fecha = hoy,
            Concepto = calculo.Concepto,
            ClienteId = cliente.Id,
            ReceptorTipoDocumento = cliente.TipoDocumento,
            ReceptorNumeroDocumento = cliente.NumeroDocumento,
            ReceptorRazonSocial = cliente.RazonSocial,
            ReceptorCondicionIva = cliente.CondicionIva,
            ReceptorDomicilio = string.Join(", ", new[] { cliente.Domicilio, cliente.Localidad }.Where(x => !string.IsNullOrEmpty(x))),
            ImporteNeto = calculo.Neto,
            ImporteIva = calculo.Iva,
            ImporteTotal = calculo.Total,
            Estado = EstadoComprobante.Pendiente,
            Entorno = config.Entorno,
            UsuarioId = _sesion.Usuario!.Id
        };
        AjustarFechasServicio(c, hoy);

        for (var i = 0; i < s.Items.Count; i++)
        {
            var it = s.Items[i];
            var l = calculo.Lineas[i];
            c.Items.Add(new ComprobanteItem
            {
                ProductoId = it.ProductoId,
                Codigo = it.Codigo,
                Descripcion = it.Descripcion.Trim(),
                Cantidad = it.Cantidad,
                PrecioUnitario = it.PrecioUnitario,
                AlicuotaIva = it.AlicuotaIva,
                EsServicio = it.EsServicio,
                ImporteNeto = l.Neto,
                ImporteIva = l.Iva,
                ImporteTotal = l.Total
            });
        }
        foreach (var a in calculo.Alicuotas)
            c.Alicuotas.Add(new ComprobanteIva { AlicuotaIva = a.Alicuota, BaseImponible = a.BaseImponible, Importe = a.Importe });

        _db.Comprobantes.Add(c);
        await _db.SaveChangesAsync(ct);   // Queda guardado ANTES de hablar con ARCA.

        await EnviarAArcaAsync(c, ct);
        return Resultado<ComprobanteEmitido>.Ok(AEmitido(c));
    }

    public async Task<Resultado<ComprobanteEmitido>> ReintentarAsync(int comprobanteId, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.Facturar))
            return Resultado<ComprobanteEmitido>.Error("No tenés permiso para facturar.");

        var c = await _db.Comprobantes.Include(x => x.Alicuotas).FirstOrDefaultAsync(x => x.Id == comprobanteId, ct);
        if (c is null || !_sesion.TieneAcceso(c.UnidadNegocioId))
            return Resultado<ComprobanteEmitido>.Error("El comprobante no existe.");
        if (c.Estado != EstadoComprobante.Pendiente)
            return Resultado<ComprobanteEmitido>.Error("Solo se pueden reintentar comprobantes pendientes.");

        await EnviarAArcaAsync(c, ct);
        return Resultado<ComprobanteEmitido>.Ok(AEmitido(c));
    }

    public async Task<ResultadoReintentos> ReintentarPendientesAsync(CancellationToken ct = default)
    {
        var permitidas = _sesion.Usuario?.UnidadesNegocioIds.ToList() ?? new List<int>();
        var ids = await _db.Comprobantes
            .Where(c => c.Estado == EstadoComprobante.Pendiente && permitidas.Contains(c.UnidadNegocioId))
            .OrderBy(c => c.Id).Select(c => c.Id).ToListAsync(ct);

        int ok = 0, rechazados = 0, pendientes = 0;
        foreach (var id in ids)
        {
            var r = await ReintentarAsync(id, ct);
            switch (r.Valor?.Estado)
            {
                case EstadoComprobante.Autorizado: ok++; break;
                case EstadoComprobante.Rechazado: rechazados++; break;
                default: pendientes++; break;
            }
        }
        return new ResultadoReintentos(ok, rechazados, pendientes);
    }

    public async Task<List<ComprobanteResumen>> ListarAsync(DateTime desde, DateTime hasta, int? unidadNegocioId = null, CancellationToken ct = default)
    {
        var permitidas = _sesion.Usuario?.UnidadesNegocioIds.ToList() ?? new List<int>();
        var d = desde.Date;
        var h = hasta.Date.AddDays(1);
        var q = _db.Comprobantes.AsNoTracking().Include(c => c.UnidadNegocio)
            .Where(c => permitidas.Contains(c.UnidadNegocioId) && c.Fecha >= d && c.Fecha < h);
        if (unidadNegocioId is not null) q = q.Where(c => c.UnidadNegocioId == unidadNegocioId);

        var lista = await q.OrderByDescending(c => c.Id).Take(2000).ToListAsync(ct);
        return lista.Select(c => new ComprobanteResumen(
            c.Id, c.Fecha, c.Tipo.Texto(), c.NumeroCompleto, c.UnidadNegocio!.Nombre, c.ReceptorRazonSocial,
            c.ImporteTotal, c.Estado, c.Estado.Texto(), c.Cae, c.CaeVencimiento, c.MensajesArca,
            c.Entorno == EntornoArca.Produccion ? "Producción" : "Pruebas")).ToList();
    }

    // =====================================================================

    private async Task EnviarAArcaAsync(Comprobante c, CancellationToken ct)
    {
        c.Intentos++;
        try
        {
            // 1) Si ya se intentó con un número, ver si ARCA llegó a autorizarlo (corte a mitad de camino).
            if (c.NumeroIntentado is { } intentado)
            {
                var existente = await _arca.ConsultarAsync(c.PuntoVentaNumero, c.Tipo, intentado, ct);
                if (existente?.Cae is not null && Coincide(c, existente))
                {
                    MarcarAutorizado(c, intentado, existente.Cae, existente.CaeVencimiento,
                        "Recuperado: ARCA ya lo había autorizado en un intento anterior.");
                    await _db.SaveChangesAsync(ct);
                    return;
                }
            }

            // Si quedó pendiente varios días, ARCA no acepta la fecha original.
            var hoy = _hoy().Date;
            var diasPermitidos = c.Concepto == ConceptoComprobante.Productos ? 5 : 10;
            if ((hoy - c.Fecha.Date).TotalDays > diasPermitidos)
            {
                c.Fecha = hoy;
                AjustarFechasServicio(c, hoy);
            }

            // 2) Pedir número y CAE (con reintento si otra PC tomó el mismo número).
            for (var intento = 1; intento <= MaximoIntentosNumeracion; intento++)
            {
                var ultimo = await _arca.UltimoAutorizadoAsync(c.PuntoVentaNumero, c.Tipo, ct);
                var numero = ultimo + 1;
                c.NumeroIntentado = numero;
                await _db.SaveChangesAsync(ct);

                var respuesta = await _arca.SolicitarCaeAsync(ArmarSolicitud(c, numero), ct);

                if (respuesta.Aprobado && respuesta.Cae is not null)
                {
                    MarcarAutorizado(c, numero, respuesta.Cae, respuesta.CaeVencimiento,
                        Unir(respuesta.Observaciones));
                    await _db.SaveChangesAsync(ct);
                    return;
                }

                var noCorrelativo = respuesta.Errores.Any(e => e.Codigo == ErrorNumeroNoCorrelativo);
                if (noCorrelativo && intento < MaximoIntentosNumeracion)
                    continue;

                c.Estado = EstadoComprobante.Rechazado;
                c.NumeroIntentado = null;
                c.MensajesArca = Unir(respuesta.Errores.Concat(respuesta.Observaciones));
                await _db.SaveChangesAsync(ct);
                return;
            }
        }
        catch (ArcaComunicacionException ex)
        {
            c.Estado = EstadoComprobante.Pendiente;
            c.MensajesArca = "Sin conexión con ARCA: " + ex.Message;
            await _db.SaveChangesAsync(CancellationToken.None);
        }
        catch (ArcaException ex)
        {
            // Problema de configuración (certificado, permisos): queda pendiente hasta corregirlo.
            c.Estado = EstadoComprobante.Pendiente;
            c.MensajesArca = "Error de ARCA: " + ex.Message;
            await _db.SaveChangesAsync(CancellationToken.None);
        }
    }

    private static void MarcarAutorizado(Comprobante c, long numero, string cae, DateTime? vto, string? mensajes)
    {
        c.Numero = numero;
        c.NumeroIntentado = numero;
        c.Cae = cae;
        c.CaeVencimiento = vto;
        c.Estado = EstadoComprobante.Autorizado;
        c.MensajesArca = string.IsNullOrWhiteSpace(mensajes) ? null : mensajes;
        c.ModificadoEn = DateTime.UtcNow;
    }

    private static bool Coincide(Comprobante c, ComprobanteConsultado e) =>
        e.ImporteTotal == c.ImporteTotal && e.DocNro == DocNro(c);

    private static long DocNro(Comprobante c) =>
        c.ReceptorTipoDocumento == TipoDocumento.SinIdentificar ? 0 : long.Parse(c.ReceptorNumeroDocumento);

    private static void AjustarFechasServicio(Comprobante c, DateTime fecha)
    {
        if (c.Concepto == ConceptoComprobante.Productos)
        {
            c.FechaServicioDesde = c.FechaServicioHasta = c.FechaVencimientoPago = null;
        }
        else
        {
            // Servicios (ej. lavadero): se presta y se cobra en el día.
            c.FechaServicioDesde = c.FechaServicioHasta = c.FechaVencimientoPago = fecha;
        }
    }

    public static SolicitudCae ArmarSolicitud(Comprobante c, long numero) => new(
        c.PuntoVentaNumero,
        c.Tipo,
        numero,
        c.Concepto,
        c.ReceptorTipoDocumento,
        DocNro(c),
        c.ReceptorCondicionIva,
        c.Fecha,
        c.ImporteTotal,
        c.ImporteNoGravado,
        c.ImporteNeto,
        c.ImporteExento,
        c.ImporteTributos,
        c.ImporteIva,
        c.FechaServicioDesde,
        c.FechaServicioHasta,
        c.FechaVencimientoPago,
        c.Alicuotas.Select(a => new AlicuotaCae(a.AlicuotaIva, a.BaseImponible, a.Importe)).ToList());

    private static List<string> ValidarItems(List<ItemEmision> items)
    {
        var e = new List<string>();
        if (items.Count == 0) e.Add("Agregá al menos un ítem.");
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var n = i + 1;
            if (string.IsNullOrWhiteSpace(it.Descripcion)) e.Add($"Ítem {n}: falta la descripción.");
            if (it.Cantidad <= 0) e.Add($"Ítem {n}: la cantidad tiene que ser mayor a cero.");
            if (it.PrecioUnitario < 0) e.Add($"Ítem {n}: el precio no puede ser negativo.");
        }
        return e;
    }

    private static string? Unir(IEnumerable<MensajeArca> mensajes)
    {
        var texto = string.Join(Environment.NewLine, mensajes.Select(m => m.ToString()));
        return texto.Length == 0 ? null : texto;
    }

    private static ComprobanteEmitido AEmitido(Comprobante c) =>
        new(c.Id, c.Estado, c.Tipo, c.NumeroCompleto, c.ImporteTotal, c.Cae, c.CaeVencimiento, c.MensajesArca);
}
