using EstacionERP.Application.Facturacion;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Tests;

/// <summary>ARCA simulado para probar el flujo de facturación sin internet.</summary>
public class ArcaFalso : IArcaClient
{
    public Dictionary<(int, TipoComprobante), long> Ultimos { get; } = new();
    public Dictionary<(int, TipoComprobante, long), ComprobanteConsultado> Autorizados { get; } = new();
    public List<SolicitudCae> Solicitudes { get; } = new();

    /// <summary>Si es true, la próxima llamada simula corte de conexión.</summary>
    public bool SinConexion { get; set; }
    /// <summary>Simula que ARCA autoriza pero la respuesta nunca llega (corte a mitad de camino).</summary>
    public bool CortarDespuesDeAutorizar { get; set; }
    /// <summary>Errores a devolver en la próxima solicitud de CAE.</summary>
    public Queue<MensajeArca[]> ErroresProximos { get; } = new();
    /// <summary>Simula que otra PC emite un comprobante justo antes.</summary>
    public int OtrasPcEmitenAntes { get; set; }

    private void Red()
    {
        if (SinConexion) throw new ArcaComunicacionException("simulado: sin internet");
    }

    public Task<EstadoServidoresArca> VerificarServidoresAsync(CancellationToken ct = default)
    {
        Red();
        return Task.FromResult(new EstadoServidoresArca("OK", "OK", "OK"));
    }

    public Task ProbarAutenticacionAsync(CancellationToken ct = default)
    {
        Red();
        return Task.CompletedTask;
    }

    public Task<long> UltimoAutorizadoAsync(int puntoVenta, TipoComprobante tipo, CancellationToken ct = default)
    {
        Red();
        return Task.FromResult(Ultimos.GetValueOrDefault((puntoVenta, tipo)));
    }

    public Task<RespuestaCae> SolicitarCaeAsync(SolicitudCae s, CancellationToken ct = default)
    {
        Red();
        Solicitudes.Add(s);
        var clave = (s.PuntoVenta, s.Tipo);

        if (OtrasPcEmitenAntes > 0)
        {
            OtrasPcEmitenAntes--;
            Ultimos[clave] = Ultimos.GetValueOrDefault(clave) + 1;
        }

        if (ErroresProximos.TryDequeue(out var errores))
            return Task.FromResult(new RespuestaCae(false, null, null, Array.Empty<MensajeArca>(), errores));

        if (s.Numero != Ultimos.GetValueOrDefault(clave) + 1)
            return Task.FromResult(new RespuestaCae(false, null, null, Array.Empty<MensajeArca>(),
                new[] { new MensajeArca(10016, "El numero o fecha del comprobante no se corresponde con el proximo a autorizar.") }));

        Ultimos[clave] = s.Numero;
        var cae = (70000000000000 + s.Numero).ToString();
        var vto = s.Fecha.AddDays(10);
        Autorizados[(s.PuntoVenta, s.Tipo, s.Numero)] = new ComprobanteConsultado(s.Numero, cae, vto, s.ImporteTotal, s.DocNro, s.Fecha);

        if (CortarDespuesDeAutorizar)
        {
            CortarDespuesDeAutorizar = false;
            throw new ArcaComunicacionException("simulado: se cortó la conexión esperando la respuesta");
        }

        return Task.FromResult(new RespuestaCae(true, cae, vto, Array.Empty<MensajeArca>(), Array.Empty<MensajeArca>()));
    }

    public Task<ComprobanteConsultado?> ConsultarAsync(int puntoVenta, TipoComprobante tipo, long numero, CancellationToken ct = default)
    {
        Red();
        return Task.FromResult(Autorizados.TryGetValue((puntoVenta, tipo, numero), out var c) ? c : null);
    }
}
