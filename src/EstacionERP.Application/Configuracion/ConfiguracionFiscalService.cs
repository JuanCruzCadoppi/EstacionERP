using EstacionERP.Application.Common;
using EstacionERP.Application.Facturacion;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Domain.Validaciones;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Configuracion;

public class ConfiguracionFiscalDatos
{
    public string Cuit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreFantasia { get; set; }
    public CondicionIva CondicionIva { get; set; } = CondicionIva.ResponsableInscripto;
    public string? Domicilio { get; set; }
    public string? Localidad { get; set; }
    public string? IngresosBrutos { get; set; }
    public DateTime? InicioActividades { get; set; }
    public EntornoArca Entorno { get; set; } = EntornoArca.Homologacion;
}

public record EstadoCertificado(
    bool TieneCertificado,
    string? Alias,
    DateTime? Vence,
    bool TieneSolicitudPendiente);

public record PuntoVentaFila(int Id, int Numero, string Descripcion, int UnidadNegocioId, string UnidadNegocio, bool Activo);

public interface IConfiguracionFiscalService
{
    Task<ConfiguracionFiscalDatos> ObtenerAsync(CancellationToken ct = default);
    Task<EstadoCertificado> EstadoCertificadoAsync(CancellationToken ct = default);
    Task<Resultado> GuardarAsync(ConfiguracionFiscalDatos datos, CancellationToken ct = default);
    Task<Resultado<string>> GenerarSolicitudCertificadoAsync(string alias, CancellationToken ct = default);
    Task<Resultado<DateTime>> ImportarCertificadoAsync(string contenidoCertificado, CancellationToken ct = default);
    Task<List<string>> ProbarConexionAsync(CancellationToken ct = default);
    Task<List<PuntoVentaFila>> ListarPuntosVentaAsync(bool soloActivos = false, CancellationToken ct = default);
    Task<Resultado<int>> GuardarPuntoVentaAsync(PuntoVentaFila datos, CancellationToken ct = default);
}

public class ConfiguracionFiscalService : IConfiguracionFiscalService
{
    private readonly IEstacionDbContext _db;
    private readonly ISesionActual _sesion;
    private readonly IArcaClient _arca;

    public ConfiguracionFiscalService(IEstacionDbContext db, ISesionActual sesion, IArcaClient arca)
    {
        _db = db;
        _sesion = sesion;
        _arca = arca;
    }

    private bool PuedeConfigurar => _sesion.Puede(Permiso.ConfigurarFacturacion);

    public async Task<ConfiguracionFiscalDatos> ObtenerAsync(CancellationToken ct = default)
    {
        var c = await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct);
        return c is null ? new ConfiguracionFiscalDatos() : new ConfiguracionFiscalDatos
        {
            Cuit = c.Cuit, RazonSocial = c.RazonSocial, NombreFantasia = c.NombreFantasia, CondicionIva = c.CondicionIva,
            Domicilio = c.Domicilio, Localidad = c.Localidad, IngresosBrutos = c.IngresosBrutos,
            InicioActividades = c.InicioActividades, Entorno = c.Entorno
        };
    }

    public async Task<EstadoCertificado> EstadoCertificadoAsync(CancellationToken ct = default)
    {
        var c = await _db.ConfiguracionesFiscales.AsNoTracking().FirstOrDefaultAsync(ct);
        return c is null
            ? new EstadoCertificado(false, null, null, false)
            : new EstadoCertificado(c.TieneCertificado, c.AliasCertificado, c.CertificadoVence, c.ClavePendientePem is not null);
    }

    public async Task<Resultado> GuardarAsync(ConfiguracionFiscalDatos d, CancellationToken ct = default)
    {
        if (!PuedeConfigurar) return Resultado.Error("Solo el administrador puede modificar la configuración fiscal.");

        d.Cuit = DocumentoValidador.SoloDigitos(d.Cuit);
        d.RazonSocial = (d.RazonSocial ?? string.Empty).Trim().ToUpper();

        var errores = new List<string>();
        if (!DocumentoValidador.CuitEsValido(d.Cuit)) errores.Add("El CUIT de la empresa no es válido.");
        if (d.RazonSocial.Length == 0) errores.Add("La razón social es obligatoria.");
        if (d.CondicionIva is not (CondicionIva.ResponsableInscripto or CondicionIva.Monotributo or CondicionIva.Exento))
            errores.Add("La condición de IVA del emisor debe ser Responsable Inscripto, Monotributo o Exento.");
        if (errores.Count > 0) return Resultado.Error(errores.ToArray());

        var c = await _db.ConfiguracionesFiscales.FirstOrDefaultAsync(ct);
        if (c is null)
        {
            c = new ConfiguracionFiscal { Id = ConfiguracionFiscal.IdUnico };
            _db.ConfiguracionesFiscales.Add(c);
        }
        else if (c.Cuit != d.Cuit && c.TieneCertificado)
        {
            // Otro CUIT: el certificado anterior ya no sirve.
            c.CertificadoPem = null;
            c.ClavePrivadaPem = null;
            c.CertificadoVence = null;
            c.AliasCertificado = null;
            _db.TicketsAcceso.RemoveRange(_db.TicketsAcceso);
        }

        c.Cuit = d.Cuit;
        c.RazonSocial = d.RazonSocial;
        c.NombreFantasia = string.IsNullOrWhiteSpace(d.NombreFantasia) ? null : d.NombreFantasia.Trim();
        c.CondicionIva = d.CondicionIva;
        c.Domicilio = string.IsNullOrWhiteSpace(d.Domicilio) ? null : d.Domicilio.Trim();
        c.Localidad = string.IsNullOrWhiteSpace(d.Localidad) ? null : d.Localidad.Trim();
        c.IngresosBrutos = string.IsNullOrWhiteSpace(d.IngresosBrutos) ? null : d.IngresosBrutos.Trim();
        c.InicioActividades = d.InicioActividades?.Date;
        c.Entorno = d.Entorno;
        c.ModificadoEn = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<string>> GenerarSolicitudCertificadoAsync(string alias, CancellationToken ct = default)
    {
        if (!PuedeConfigurar) return Resultado<string>.Error("Solo el administrador puede gestionar el certificado.");

        var c = await _db.ConfiguracionesFiscales.FirstOrDefaultAsync(ct);
        if (c is null || string.IsNullOrEmpty(c.Cuit))
            return Resultado<string>.Error("Primero guardá los datos fiscales (CUIT y razón social).");

        alias = (alias ?? string.Empty).Trim().ToLower();
        if (alias.Length is < 3 or > 30 || !alias.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
            return Resultado<string>.Error("El alias debe tener entre 3 y 30 caracteres: letras, números, guion o guion bajo (ej. estacionerp).");

        var solicitud = CertificadoArca.GenerarSolicitud(c.Cuit, c.RazonSocial, alias);
        c.ClavePendientePem = solicitud.ClavePrivadaPem;
        c.SolicitudCertificadoPem = solicitud.CsrPem;
        c.AliasCertificado ??= alias;
        c.ModificadoEn = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Resultado<string>.Ok(solicitud.CsrPem);
    }

    public async Task<Resultado<DateTime>> ImportarCertificadoAsync(string contenido, CancellationToken ct = default)
    {
        if (!PuedeConfigurar) return Resultado<DateTime>.Error("Solo el administrador puede gestionar el certificado.");

        var c = await _db.ConfiguracionesFiscales.FirstOrDefaultAsync(ct);
        if (c is null) return Resultado<DateTime>.Error("Primero guardá los datos fiscales.");

        CertificadoArca.DatosCertificado cert;
        try
        {
            cert = CertificadoArca.LeerCertificado(contenido);
        }
        catch (Exception)
        {
            return Resultado<DateTime>.Error("El archivo no es un certificado válido. Tiene que ser el que descargaste de ARCA (.crt o .pem).");
        }

        if (cert.Cuit is not null && cert.Cuit != c.Cuit)
            return Resultado<DateTime>.Error($"El certificado es del CUIT {DocumentoValidador.FormatearCuit(cert.Cuit)}, pero la empresa tiene el CUIT {DocumentoValidador.FormatearCuit(c.Cuit)}.");

        string? clave = null;
        try
        {
            if (c.ClavePendientePem is not null && CertificadoArca.ClaveCoincide(cert, c.ClavePendientePem))
                clave = c.ClavePendientePem;
            else if (c.ClavePrivadaPem is not null && CertificadoArca.ClaveCoincide(cert, c.ClavePrivadaPem))
                clave = c.ClavePrivadaPem;
        }
        catch (Exception ex)
        {
            return Resultado<DateTime>.Error("No se pudo verificar la clave del certificado: " + ex.Message);
        }

        if (clave is null)
            return Resultado<DateTime>.Error("El certificado no corresponde a la última solicitud generada. Generá una solicitud nueva y volvé a pedir el certificado en ARCA.");

        if (cert.Vence < DateTime.Now)
            return Resultado<DateTime>.Error($"El certificado está vencido (venció el {cert.Vence:dd/MM/yyyy}).");

        // Verifica que Windows pueda firmar con el certificado antes de guardarlo.
        try
        {
            using var prueba = CertificadoArca.ParaFirmar(cert.Pem, clave);
        }
        catch (Exception ex)
        {
            return Resultado<DateTime>.Error("El certificado es correcto pero Windows no pudo prepararlo para firmar: " + ex.Message);
        }

        c.CertificadoPem = cert.Pem;
        c.ClavePrivadaPem = clave;
        if (clave == c.ClavePendientePem)
        {
            c.ClavePendientePem = null;
            c.SolicitudCertificadoPem = null;
        }
        c.CertificadoVence = cert.Vence.ToUniversalTime();
        c.ModificadoEn = DateTime.UtcNow;

        // Certificado nuevo → tickets de acceso nuevos.
        _db.TicketsAcceso.RemoveRange(_db.TicketsAcceso);
        await _db.SaveChangesAsync(ct);
        return Resultado<DateTime>.Ok(cert.Vence);
    }

    public async Task<List<string>> ProbarConexionAsync(CancellationToken ct = default)
    {
        var r = new List<string>();
        try
        {
            var estado = await _arca.VerificarServidoresAsync(ct);
            r.Add(estado.TodoOk
                ? "OK - Servidores de ARCA funcionando."
                : $"ATENCIÓN - Servidores de ARCA: App {estado.AppServer}, Base {estado.DbServer}, Autenticación {estado.AuthServer}.");
        }
        catch (Exception ex)
        {
            r.Add("ERROR - No se pudo conectar con ARCA: " + ex.Message);
            return r;
        }

        try
        {
            await _arca.ProbarAutenticacionAsync(ct);
            r.Add("OK - Certificado aceptado: autenticación correcta (WSAA).");
        }
        catch (Exception ex)
        {
            r.Add("ERROR - Autenticación: " + ex.Message);
            return r;
        }

        var pv = await _db.PuntosVenta.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Numero).FirstOrDefaultAsync(ct);
        if (pv is null)
        {
            r.Add("ATENCIÓN - No hay puntos de venta cargados.");
            return r;
        }

        try
        {
            var config = await _db.ConfiguracionesFiscales.AsNoTracking().FirstAsync(ct);
            var tipo = ReglasComprobante.TipoFactura(config.CondicionIva, CondicionIva.ConsumidorFinal);
            var ultimo = await _arca.UltimoAutorizadoAsync(pv.Numero, tipo, ct);
            r.Add($"OK - Facturación habilitada: último {tipo.Texto()} del punto de venta {pv.Numero:D4} es el N° {ultimo}.");
        }
        catch (Exception ex)
        {
            r.Add("ERROR - Facturación (WSFEv1): " + ex.Message);
        }
        return r;
    }

    public async Task<List<PuntoVentaFila>> ListarPuntosVentaAsync(bool soloActivos = false, CancellationToken ct = default)
    {
        var q = _db.PuntosVenta.AsNoTracking().Include(p => p.UnidadNegocio).AsQueryable();
        if (soloActivos) q = q.Where(p => p.Activo);
        var lista = await q.OrderBy(p => p.Numero).ToListAsync(ct);
        return lista.Select(p => new PuntoVentaFila(p.Id, p.Numero, p.Descripcion, p.UnidadNegocioId, p.UnidadNegocio!.Nombre, p.Activo)).ToList();
    }

    public async Task<Resultado<int>> GuardarPuntoVentaAsync(PuntoVentaFila d, CancellationToken ct = default)
    {
        if (!PuedeConfigurar) return Resultado<int>.Error("Solo el administrador puede modificar puntos de venta.");

        var errores = new List<string>();
        if (d.Numero is < 1 or > 99998) errores.Add("El número de punto de venta debe estar entre 1 y 99998.");
        if (string.IsNullOrWhiteSpace(d.Descripcion)) errores.Add("La descripción es obligatoria.");
        if (!await _db.UnidadesNegocio.AnyAsync(u => u.Id == d.UnidadNegocioId, ct)) errores.Add("Elegí la unidad de negocio.");
        if (await _db.PuntosVenta.AnyAsync(p => p.Id != d.Id && p.Numero == d.Numero, ct))
            errores.Add($"El punto de venta {d.Numero} ya está cargado.");
        if (errores.Count > 0) return Resultado<int>.Error(errores.ToArray());

        PuntoVenta pv;
        if (d.Id == 0)
        {
            pv = new PuntoVenta();
            _db.PuntosVenta.Add(pv);
        }
        else
        {
            pv = await _db.PuntosVenta.FirstAsync(p => p.Id == d.Id, ct);
            if (pv.Numero != d.Numero && await _db.Comprobantes.AnyAsync(c => c.PuntoVentaId == pv.Id, ct))
                return Resultado<int>.Error("No se puede cambiar el número: ya tiene comprobantes emitidos.");
            pv.ModificadoEn = DateTime.UtcNow;
        }

        pv.Numero = d.Numero;
        pv.Descripcion = d.Descripcion.Trim();
        pv.UnidadNegocioId = d.UnidadNegocioId;
        pv.Activo = d.Activo;
        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(pv.Id);
    }
}
