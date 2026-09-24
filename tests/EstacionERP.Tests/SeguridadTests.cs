using EstacionERP.Application.Seguridad;
using EstacionERP.Application.Usuarios;
using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;

namespace EstacionERP.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hashea_y_verifica()
    {
        var hash = PasswordHasher.Hashear("clave123");
        Assert.True(PasswordHasher.Verificar("clave123", hash));
        Assert.False(PasswordHasher.Verificar("clave124", hash));
    }

    [Fact]
    public void Dos_hashes_de_la_misma_clave_son_distintos_por_el_salt() =>
        Assert.NotEqual(PasswordHasher.Hashear("abc123"), PasswordHasher.Hashear("abc123"));

    [Fact]
    public void Hash_invalido_no_verifica() => Assert.False(PasswordHasher.Verificar("x", "basura"));
}

public class AuthServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    public void Dispose() => _bd.Dispose();

    [Fact]
    public async Task Admin_inicial_ingresa_con_admin_admin_y_debe_cambiar_la_clave()
    {
        using var db = _bd.CrearContexto();
        var r = await new AuthService(db).IngresarAsync("ADMIN", "admin");

        Assert.True(r.Exito, string.Join(", ", r.Errores));
        Assert.Equal(Rol.Administrador, r.Valor!.Rol);
        Assert.True(r.Valor.DebeCambiarPassword);
        Assert.Equal(3, r.Valor.UnidadesNegocioIds.Count);
    }

    [Fact]
    public async Task Clave_incorrecta_es_rechazada()
    {
        using var db = _bd.CrearContexto();
        var r = await new AuthService(db).IngresarAsync("admin", "otra");
        Assert.False(r.Exito);
    }

    [Fact]
    public async Task Cambiar_password_valida_y_permite_ingresar_con_la_nueva()
    {
        using var db = _bd.CrearContexto();
        var auth = new AuthService(db);

        Assert.False((await auth.CambiarPasswordAsync(1, "admin", "123", "123")).Exito);            // corta
        Assert.False((await auth.CambiarPasswordAsync(1, "admin", "nueva123", "otra123")).Exito);   // no coincide
        Assert.False((await auth.CambiarPasswordAsync(1, "mala", "nueva123", "nueva123")).Exito);   // actual mal
        Assert.True((await auth.CambiarPasswordAsync(1, "admin", "nueva123", "nueva123")).Exito);

        var r = await auth.IngresarAsync("admin", "nueva123");
        Assert.True(r.Exito);
        Assert.False(r.Valor!.DebeCambiarPassword);
    }

    [Fact]
    public async Task Usuario_desactivado_no_puede_ingresar()
    {
        using var db = _bd.CrearContexto();
        var usuarios = new UsuarioService(db, Sesiones.Admin());
        var id = (await usuarios.GuardarAsync(new UsuarioDatos
        {
            NombreUsuario = "playero1", NombreCompleto = "Playero Uno", Rol = Rol.Operador,
            UnidadesNegocioIds = { UnidadNegocio.PlayaId }, NuevaPassword = "temp123"
        })).Valor;

        var datos = await usuarios.ObtenerAsync(id);
        datos!.Activo = false;
        Assert.True((await usuarios.GuardarAsync(datos)).Exito);

        var r = await new AuthService(db).IngresarAsync("playero1", "temp123");
        Assert.False(r.Exito);
        Assert.Contains("desactivado", r.Errores[0]);
    }
}

public class UsuarioServiceTests : IDisposable
{
    private readonly BaseDeDatosDePrueba _bd = new();
    public void Dispose() => _bd.Dispose();

    private static UsuarioDatos Playero() => new()
    {
        NombreUsuario = "Playero1", NombreCompleto = "Carlos Gómez", Rol = Rol.Operador,
        UnidadesNegocioIds = { UnidadNegocio.PlayaId }, NuevaPassword = "temp123"
    };

    [Fact]
    public async Task Crea_operador_con_su_unidad_y_login_devuelve_esa_unidad()
    {
        using var db = _bd.CrearContexto();
        var r = await new UsuarioService(db, Sesiones.Admin()).GuardarAsync(Playero());
        Assert.True(r.Exito, string.Join(", ", r.Errores));

        var login = await new AuthService(db).IngresarAsync("playero1", "temp123");
        Assert.True(login.Exito);
        Assert.Equal(new[] { UnidadNegocio.PlayaId }, login.Valor!.UnidadesNegocioIds);
        Assert.True(login.Valor.DebeCambiarPassword);
    }

    [Fact]
    public async Task Validaciones_de_usuario()
    {
        using var db = _bd.CrearContexto();
        var servicio = new UsuarioService(db, Sesiones.Admin());

        var sinUnidad = Playero(); sinUnidad.UnidadesNegocioIds.Clear();
        Assert.False((await servicio.GuardarAsync(sinUnidad)).Exito);

        var sinClave = Playero(); sinClave.NuevaPassword = null;
        Assert.False((await servicio.GuardarAsync(sinClave)).Exito);

        var nombreMalo = Playero(); nombreMalo.NombreUsuario = "a b";
        Assert.False((await servicio.GuardarAsync(nombreMalo)).Exito);

        Assert.True((await servicio.GuardarAsync(Playero())).Exito);
        Assert.False((await servicio.GuardarAsync(Playero())).Exito); // duplicado
    }

    [Fact]
    public async Task No_se_puede_quitar_el_ultimo_administrador()
    {
        using var db = _bd.CrearContexto();
        var servicio = new UsuarioService(db, Sesiones.Encargado(1)); // sesión sin permiso
        Assert.False((await servicio.GuardarAsync(Playero())).Exito);

        var comoAdmin = new UsuarioService(db, Sesiones.Admin());
        var admin = await comoAdmin.ObtenerAsync(Usuario.AdministradorInicialId);
        admin!.Rol = Rol.Encargado;
        admin.UnidadesNegocioIds.Add(1);
        var r = await comoAdmin.GuardarAsync(admin);
        Assert.False(r.Exito);
        Assert.Contains(r.Errores, e => e.Contains("al menos un administrador"));
    }

    [Fact]
    public async Task Cambiar_unidades_asignadas()
    {
        using var db = _bd.CrearContexto();
        var servicio = new UsuarioService(db, Sesiones.Admin());
        var id = (await servicio.GuardarAsync(Playero())).Valor;

        var d = await servicio.ObtenerAsync(id);
        d!.UnidadesNegocioIds = new() { UnidadNegocio.RepuestosId, UnidadNegocio.LavaderoId };
        Assert.True((await servicio.GuardarAsync(d)).Exito);

        var otra = await servicio.ObtenerAsync(id);
        Assert.Equal(new[] { 2, 3 }, otra!.UnidadesNegocioIds.OrderBy(x => x));
    }
}
