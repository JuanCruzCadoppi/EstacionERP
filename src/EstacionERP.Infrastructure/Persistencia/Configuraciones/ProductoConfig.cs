using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class ProductoConfig : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("productos");
        b.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        b.Property(x => x.CodigoBarras).HasMaxLength(30);
        b.Property(x => x.CodigoFabricante).HasMaxLength(50);
        b.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
        b.Property(x => x.Marca).HasMaxLength(60);
        b.Property(x => x.Rubro).HasMaxLength(60);
        b.Property(x => x.Costo).HasPrecision(18, 4);
        b.Property(x => x.StockMinimo).HasPrecision(18, 3);
        b.Ignore(x => x.PrecioNeto);

        // El código es único dentro de cada unidad de negocio.
        b.HasIndex(x => new { x.UnidadNegocioId, x.Codigo }).IsUnique();
        b.HasIndex(x => x.CodigoBarras);
        b.HasIndex(x => x.Descripcion);

        b.HasOne(x => x.UnidadNegocio)
            .WithMany()
            .HasForeignKey(x => x.UnidadNegocioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
