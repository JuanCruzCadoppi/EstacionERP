using EstacionERP.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EstacionERP.Infrastructure.Persistencia.Configuraciones;

public class ConfiguracionFiscalConfig : IEntityTypeConfiguration<ConfiguracionFiscal>
{
    public void Configure(EntityTypeBuilder<ConfiguracionFiscal> b)
    {
        b.ToTable("configuracion_fiscal");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Cuit).HasMaxLength(11).IsRequired();
        b.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
        b.Property(x => x.NombreFantasia).HasMaxLength(150);
        b.Property(x => x.Domicilio).HasMaxLength(200);
        b.Property(x => x.Localidad).HasMaxLength(100);
        b.Property(x => x.IngresosBrutos).HasMaxLength(30);
        b.Property(x => x.AliasCertificado).HasMaxLength(30);
        b.Property(x => x.InicioActividades).HasColumnType("date");
    }
}

public class TicketAccesoConfig : IEntityTypeConfiguration<TicketAcceso>
{
    public void Configure(EntityTypeBuilder<TicketAcceso> b)
    {
        b.ToTable("tickets_acceso_arca");
        b.Property(x => x.Servicio).HasMaxLength(30).IsRequired();
        b.Property(x => x.Cuit).HasMaxLength(11).IsRequired();
        b.Property(x => x.Token).IsRequired();
        b.Property(x => x.Sign).IsRequired();
        b.HasIndex(x => new { x.Servicio, x.Entorno, x.Cuit }).IsUnique();
    }
}

public class ComprobanteConfig : IEntityTypeConfiguration<Comprobante>
{
    public void Configure(EntityTypeBuilder<Comprobante> b)
    {
        b.ToTable("comprobantes");
        b.Ignore(x => x.NumeroCompleto);
        b.Property(x => x.ReceptorNumeroDocumento).HasMaxLength(11).IsRequired();
        b.Property(x => x.ReceptorRazonSocial).HasMaxLength(150).IsRequired();
        b.Property(x => x.ReceptorDomicilio).HasMaxLength(300);
        b.Property(x => x.Cae).HasMaxLength(14);
        b.Property(x => x.MensajesArca).HasMaxLength(4000);

        // Fechas de calendario (sin hora): columna "date" para no depender de la zona horaria.
        b.Property(x => x.Fecha).HasColumnType("date");
        b.Property(x => x.FechaServicioDesde).HasColumnType("date");
        b.Property(x => x.FechaServicioHasta).HasColumnType("date");
        b.Property(x => x.FechaVencimientoPago).HasColumnType("date");
        b.Property(x => x.CaeVencimiento).HasColumnType("date");

        // Un número por (entorno, punto de venta, tipo). Los NULL (pendientes) no chocan.
        b.HasIndex(x => new { x.Entorno, x.PuntoVentaNumero, x.Tipo, x.Numero }).IsUnique();
        b.HasIndex(x => x.Fecha);
        b.HasIndex(x => x.Estado);
        b.HasIndex(x => x.ClienteId);

        b.HasOne(x => x.UnidadNegocio).WithMany().HasForeignKey(x => x.UnidadNegocioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PuntoVenta).WithMany().HasForeignKey(x => x.PuntoVentaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ComprobanteAsociado).WithMany().HasForeignKey(x => x.ComprobanteAsociadoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ComprobanteId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Alicuotas).WithOne().HasForeignKey(x => x.ComprobanteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ComprobanteItemConfig : IEntityTypeConfiguration<ComprobanteItem>
{
    public void Configure(EntityTypeBuilder<ComprobanteItem> b)
    {
        b.ToTable("comprobantes_items");
        b.Property(x => x.Codigo).HasMaxLength(30);
        b.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
        b.Property(x => x.Cantidad).HasPrecision(18, 3);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ComprobanteIvaConfig : IEntityTypeConfiguration<ComprobanteIva>
{
    public void Configure(EntityTypeBuilder<ComprobanteIva> b) => b.ToTable("comprobantes_iva");
}
