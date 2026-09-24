using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class UnidadNegocioConfig : IEntityTypeConfiguration<UnidadNegocio>
{
    public void Configure(EntityTypeBuilder<UnidadNegocio> b)
    {
        b.ToTable("unidades_negocio");
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();

        var alta = DatosIniciales.FechaAlta;
        b.HasData(
            new UnidadNegocio { Id = UnidadNegocio.PlayaId, Codigo = "PLAYA", Nombre = "Playa / Combustibles", CreadoEn = alta },
            new UnidadNegocio { Id = UnidadNegocio.RepuestosId, Codigo = "REPUESTOS", Nombre = "Repuestos", CreadoEn = alta },
            new UnidadNegocio { Id = UnidadNegocio.LavaderoId, Codigo = "LAVADERO", Nombre = "Lavadero", CreadoEn = alta });
    }
}
