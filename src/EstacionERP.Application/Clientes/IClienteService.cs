using EstacionERP.Application.Common;

namespace EstacionERP.Application.Clientes;

public interface IClienteService
{
    Task<List<ClienteResumen>> BuscarAsync(string? texto, bool incluirInactivos = false, CancellationToken ct = default);
    Task<ClienteDatos?> ObtenerAsync(int id, CancellationToken ct = default);
    Task<Resultado<int>> GuardarAsync(ClienteDatos datos, CancellationToken ct = default);
    Task<Resultado> CambiarEstadoAsync(int id, bool activo, CancellationToken ct = default);
}
