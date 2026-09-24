using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class VehiculoConfig : IEntityTypeConfiguration<Vehiculo>
{
    public void Configure(EntityTypeBuilder<Vehiculo> b)
    {
        b.ToTable("vehiculos");
        b.Property(x => x.Patente).HasMaxLength(10).IsRequired();
        b.Property(x => x.Marca).HasMaxLength(50);
        b.Property(x => x.Modelo).HasMaxLength(50);
        b.HasIndex(x => x.Patente).IsUnique();
        b.HasOne(x => x.Cliente)
            .WithMany(c => c.Vehiculos)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
