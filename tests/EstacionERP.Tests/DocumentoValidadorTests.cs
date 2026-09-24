using EstacionERP.Domain.Validaciones;

namespace EstacionERP.Tests;

public class DocumentoValidadorTests
{
    [Theory]
    [InlineData("20-12345678-6")]
    [InlineData("20123456786")]
    [InlineData("30-50001091-2")]   // CUIT de ejemplo con prefijo de empresa
    [InlineData("27-11111111-7")]
    public void Cuit_valido(string cuit) => Assert.True(DocumentoValidador.CuitEsValido(cuit));

    [Theory]
    [InlineData("20-12345678-0")]   // dígito verificador incorrecto
    [InlineData("2012345678")]      // 10 dígitos
    [InlineData("99-12345678-6")]   // prefijo inexistente
    [InlineData("")]
    [InlineData(null)]
    public void Cuit_invalido(string? cuit) => Assert.False(DocumentoValidador.CuitEsValido(cuit));

    [Theory]
    [InlineData("12.345.678", true)]
    [InlineData("1234567", true)]
    [InlineData("123456", false)]
    [InlineData("00000000", false)]
    public void Dni(string dni, bool esperado) => Assert.Equal(esperado, DocumentoValidador.DniEsValido(dni));

    [Fact]
    public void Formatea_cuit() =>
        Assert.Equal("20-12345678-6", DocumentoValidador.FormatearCuit("20123456786"));
}
