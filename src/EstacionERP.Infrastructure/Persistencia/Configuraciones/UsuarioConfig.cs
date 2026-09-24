using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    /// <summary>
    /// Hash de la contraseña inicial "admin". El sistema obliga a cambiarla en el primer ingreso.
    /// </summary>
    private const string HashAdminInicial =
        "PBKDF2-SHA256$100000$RXN0YWNpb25FUlAtaW5pIQ==$J7LlGFrH/7nIgI0D8uEdmGpPEaBTmMFwA8RH6gPGL9s=";

    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("usuarios");
        b.Property(x => x.NombreUsuario).HasMaxLength(30).IsRequired();
        b.Property(x => x.NombreCompleto).HasMaxLength(100).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.NombreUsuario).IsUnique();

        b.HasData(new Usuario
        {
            Id = Usuario.AdministradorInicialId,
            NombreUsuario = "admin",
            NombreCompleto = "Administrador",
            PasswordHash = HashAdminInicial,
            Rol = Rol.Administrador,
            DebeCambiarPassword = true,
            Activo = true,
            CreadoEn = DatosIniciales.FechaAlta
        });
    }
}

public class UsuarioUnidadNegocioConfig : IEntityTypeConfiguration<UsuarioUnidadNegocio>
{
    public void Configure(EntityTypeBuilder<UsuarioUnidadNegocio> b)
    {
        b.ToTable("usuarios_unidades_negocio");
        b.HasKey(x => new { x.UsuarioId, x.UnidadNegocioId });
        b.HasOne(x => x.Usuario).WithMany(u => u.Unidades).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.UnidadNegocio).WithMany().HasForeignKey(x => x.UnidadNegocioId).OnDelete(DeleteBehavior.Restrict);
    }
}
