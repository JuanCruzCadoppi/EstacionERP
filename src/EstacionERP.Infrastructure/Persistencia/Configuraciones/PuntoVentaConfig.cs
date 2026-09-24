using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class PuntoVentaConfig : IEntityTypeConfiguration<PuntoVenta>
{
    public void Configure(EntityTypeBuilder<PuntoVenta> b)
    {
        b.ToTable("puntos_venta");
        b.Property(x => x.Descripcion).HasMaxLength(60).IsRequired();
        b.Property(x => x.Impresora).HasMaxLength(200);
        b.HasIndex(x => x.Numero).IsUnique();
        b.HasOne(x => x.UnidadNegocio)
            .WithMany(u => u.PuntosVenta)
            .HasForeignKey(x => x.UnidadNegocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
