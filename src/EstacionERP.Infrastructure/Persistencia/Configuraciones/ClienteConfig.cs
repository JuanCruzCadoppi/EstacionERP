using EstacionERP.Domain.Entidades;
using EstacionERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class ClienteConfig : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("clientes");
        b.Property(x => x.NumeroDocumento).HasMaxLength(11).IsRequired();
        b.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
        b.Property(x => x.NombreFantasia).HasMaxLength(150);
        b.Property(x => x.Domicilio).HasMaxLength(200);
        b.Property(x => x.Localidad).HasMaxLength(100);
        b.Property(x => x.Telefono).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Observaciones).HasMaxLength(1000);

        b.HasIndex(x => new { x.TipoDocumento, x.NumeroDocumento });
        b.HasIndex(x => x.RazonSocial);

        b.HasData(new Cliente
        {
            Id = Cliente.ConsumidorFinalId,
            TipoDocumento = TipoDocumento.SinIdentificar,
            NumeroDocumento = "0",
            RazonSocial = "CONSUMIDOR FINAL",
            CondicionIva = CondicionIva.ConsumidorFinal,
            Activo = true,
            CreadoEn = DatosIniciales.FechaAlta
        });
    }
}
