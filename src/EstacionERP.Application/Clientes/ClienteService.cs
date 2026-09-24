using EstacionERP.Application.Common;
using EstacionERP.Application.Seguridad;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using EstacionERP.Domain.Validaciones;
using Microsoft.EntityFrameworkCore;

namespace EstacionERP.Application.Clientes;

/// <summary>
/// Casos de uso de clientes: buscar, dar de alta, modificar, activar/desactivar.
/// Todas las reglas de negocio de clientes viven acá, no en las pantallas.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IEstacionDbContext _db;
    private readonly ISesionActual _sesion;

    public ClienteService(IEstacionDbContext db, ISesionActual sesion)
    {
        _db = db;
        _sesion = sesion;
    }

    public async Task<List<ClienteResumen>> BuscarAsync(string? texto, bool incluirInactivos = false, CancellationToken ct = default)
    {
        var q = _db.Clientes.AsNoTracking().AsQueryable();

        if (!incluirInactivos)
            q = q.Where(c => c.Activo);

        if (!string.IsNullOrWhiteSpace(texto))
        {
            var t = texto.Trim().ToUpper();
            var digitos = DocumentoValidador.SoloDigitos(texto);
            q = q.Where(c =>
                c.RazonSocial.ToUpper().Contains(t) ||
                (c.NombreFantasia != null && c.NombreFantasia.ToUpper().Contains(t)) ||
                (digitos.Length > 0 && c.NumeroDocumento.Contains(digitos)));
        }

        var lista = await q.OrderBy(c => c.RazonSocial).Take(500).ToListAsync(ct);

        return lista.Select(c => new ClienteResumen(
            c.Id,
            FormatearDocumento(c.TipoDocumento, c.NumeroDocumento),
            c.RazonSocial,
            c.CondicionIva.Texto(),
            c.Localidad,
            c.Telefono,
            c.TieneCuentaCorriente,
            c.LimiteCredito,
            c.Activo)).ToList();
    }

    public async Task<ClienteDatos?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return null;

        return new ClienteDatos
        {
            Id = c.Id,
            TipoDocumento = c.TipoDocumento,
            NumeroDocumento = c.NumeroDocumento,
            RazonSocial = c.RazonSocial,
            NombreFantasia = c.NombreFantasia,
            CondicionIva = c.CondicionIva,
            Domicilio = c.Domicilio,
            Localidad = c.Localidad,
            Telefono = c.Telefono,
            Email = c.Email,
            TieneCuentaCorriente = c.TieneCuentaCorriente,
            LimiteCredito = c.LimiteCredito,
            Observaciones = c.Observaciones,
            Activo = c.Activo
        };
    }

    public async Task<Resultado<int>> GuardarAsync(ClienteDatos datos, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.EditarClientes))
            return Resultado<int>.Error("No tenés permiso para modificar clientes.");

        if (datos.Id == Cliente.ConsumidorFinalId)
            return Resultado<int>.Error("El cliente \"Consumidor Final\" es del sistema y no se puede modificar.");

        Normalizar(datos);

        var errores = Validar(datos);
        if (errores.Count > 0)
            return Resultado<int>.Error(errores.ToArray());

        if (datos.TipoDocumento != TipoDocumento.SinIdentificar)
        {
            var duplicado = await _db.Clientes.AnyAsync(c =>
                c.Id != datos.Id &&
                c.TipoDocumento == datos.TipoDocumento &&
                c.NumeroDocumento == datos.NumeroDocumento, ct);

            if (duplicado)
                return Resultado<int>.Error($"Ya existe un cliente con {datos.TipoDocumento.Texto()} {datos.NumeroDocumento}.");
        }

        Cliente cliente;
        if (datos.Id == 0)
        {
            cliente = new Cliente();
            _db.Clientes.Add(cliente);
        }
        else
        {
            cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == datos.Id, ct)
                      ?? throw new InvalidOperationException($"No existe el cliente {datos.Id}.");
            cliente.ModificadoEn = DateTime.UtcNow;
        }

        cliente.TipoDocumento = datos.TipoDocumento;
        cliente.NumeroDocumento = datos.NumeroDocumento;
        cliente.RazonSocial = datos.RazonSocial;
        cliente.NombreFantasia = datos.NombreFantasia;
        cliente.CondicionIva = datos.CondicionIva;
        cliente.Domicilio = datos.Domicilio;
        cliente.Localidad = datos.Localidad;
        cliente.Telefono = datos.Telefono;
        cliente.Email = datos.Email;
        cliente.TieneCuentaCorriente = datos.TieneCuentaCorriente;
        cliente.LimiteCredito = datos.TieneCuentaCorriente ? datos.LimiteCredito : 0;
        cliente.Observaciones = datos.Observaciones;
        cliente.Activo = datos.Activo;

        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(cliente.Id);
    }

    public async Task<Resultado> CambiarEstadoAsync(int id, bool activo, CancellationToken ct = default)
    {
        if (!_sesion.Puede(Permiso.DesactivarClientes))
            return Resultado.Error("No tenés permiso para activar o desactivar clientes.");

        if (id == Cliente.ConsumidorFinalId)
            return Resultado.Error("El cliente \"Consumidor Final\" no se puede desactivar.");

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cliente is null)
            return Resultado.Error("El cliente no existe.");

        // Nunca se borran clientes: se desactivan para conservar el historial.
        cliente.Activo = activo;
        cliente.ModificadoEn = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    // ---------------------------------------------------------------------

    private static void Normalizar(ClienteDatos d)
    {
        d.NumeroDocumento = d.TipoDocumento == TipoDocumento.SinIdentificar
            ? "0"
            : DocumentoValidador.SoloDigitos(d.NumeroDocumento);
        d.RazonSocial = (d.RazonSocial ?? string.Empty).Trim().ToUpper();
        d.NombreFantasia = Limpiar(d.NombreFantasia);
        d.Domicilio = Limpiar(d.Domicilio);
        d.Localidad = Limpiar(d.Localidad);
        d.Telefono = Limpiar(d.Telefono);
        d.Email = Limpiar(d.Email)?.ToLower();
        d.Observaciones = Limpiar(d.Observaciones);
    }

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Reglas de validación de un cliente. Pública para poder testearla.</summary>
    public static List<string> Validar(ClienteDatos d)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(d.RazonSocial))
            errores.Add("La razón social / nombre es obligatoria.");
        else if (d.RazonSocial.Length > 150)
            errores.Add("La razón social no puede superar los 150 caracteres.");

        switch (d.TipoDocumento)
        {
            case TipoDocumento.Cuit:
            case TipoDocumento.Cuil:
                if (!DocumentoValidador.CuitEsValido(d.NumeroDocumento))
                    errores.Add($"El {d.TipoDocumento.Texto()} ingresado no es válido (revisá el dígito verificador).");
                break;
            case TipoDocumento.Dni:
                if (!DocumentoValidador.DniEsValido(d.NumeroDocumento))
                    errores.Add("El DNI debe tener 7 u 8 dígitos.");
                break;
        }

        // Para Factura A (y para exentos/monotributistas) ARCA exige identificar al receptor con CUIT.
        var requiereCuit = d.CondicionIva is CondicionIva.ResponsableInscripto or CondicionIva.Monotributo
            or CondicionIva.Exento or CondicionIva.MonotributistaSocial;
        if (requiereCuit && d.TipoDocumento != TipoDocumento.Cuit)
            errores.Add($"Un cliente \"{d.CondicionIva.Texto()}\" debe tener CUIT.");

        if (d.TipoDocumento == TipoDocumento.SinIdentificar && d.CondicionIva != CondicionIva.ConsumidorFinal)
            errores.Add("Solo un Consumidor Final puede quedar sin identificar.");

        if (d.TieneCuentaCorriente && d.TipoDocumento == TipoDocumento.SinIdentificar)
            errores.Add("Para abrir cuenta corriente el cliente tiene que estar identificado.");

        if (d.LimiteCredito < 0)
            errores.Add("El límite de crédito no puede ser negativo.");

        if (d.Email is not null && !d.Email.Contains('@'))
            errores.Add("El email no tiene un formato válido.");

        return errores;
    }

    private static string FormatearDocumento(TipoDocumento tipo, string numero) => tipo switch
    {
        TipoDocumento.Cuit or TipoDocumento.Cuil => $"{tipo.Texto()} {DocumentoValidador.FormatearCuit(numero)}",
        TipoDocumento.SinIdentificar => "-",
        _ => $"{tipo.Texto()} {numero}"
    };
}
