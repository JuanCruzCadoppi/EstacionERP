using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EstacionERP.Application.Configuracion;

/// <summary>
/// Genera la clave privada y la solicitud de certificado (CSR) que pide ARCA, e importa
/// el certificado que devuelve ARCA. Todo con .NET, sin necesidad de instalar OpenSSL.
/// </summary>
public static class CertificadoArca
{
    private const string OidSerialNumber = "2.5.4.5";

    public record Solicitud(string CsrPem, string ClavePrivadaPem);

    /// <summary>
    /// Sujeto que exige ARCA: C=AR, O=empresa, CN=alias, serialNumber=CUIT nnnnnnnnnnn
    /// </summary>
    public static Solicitud GenerarSolicitud(string cuit, string razonSocial, string alias)
    {
        using var rsa = RSA.Create(2048);

        var nombre = new X500DistinguishedNameBuilder();
        nombre.AddCountryOrRegion("AR");
        nombre.AddOrganizationName(razonSocial);
        nombre.AddCommonName(alias);
        nombre.Add(OidSerialNumber, $"CUIT {cuit}", UniversalTagNumber.PrintableString);

        var request = new CertificateRequest(nombre.Build(), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var csr = request.CreateSigningRequestPem();
        var clave = rsa.ExportPkcs8PrivateKeyPem();
        return new Solicitud(csr, clave);
    }

    /// <summary>Datos del certificado ya extraídos (sin objetos de Windows que haya que liberar).</summary>
    public record DatosCertificado(string Pem, DateTime Vence, string? Cuit, byte[] ClavePublica);

    /// <summary>
    /// Lee el certificado de ARCA (PEM con BEGIN CERTIFICATE, o DER/base64) y extrae lo que necesitamos.
    /// Se decodifica el PEM a mano y se lee todo de una vez: en Windows, usar el objeto
    /// X509Certificate2 fuera de su ciclo de vida da "m_safeCertContext is an invalid handle".
    /// </summary>
    public static DatosCertificado LeerCertificado(string contenido)
    {
        var der = Decodificar(contenido);
        var cert = new X509Certificate2(der);
        try
        {
            var raw = cert.RawData;
            var pem = new string(PemEncoding.Write("CERTIFICATE", raw)) + "\n";
            return new DatosCertificado(
                pem,
                cert.NotAfter,
                CuitDelSujeto(cert.SubjectName),
                cert.PublicKey.ExportSubjectPublicKeyInfo());
        }
        finally
        {
            cert.Dispose();
        }
    }

    private static byte[] Decodificar(string contenido)
    {
        contenido = contenido.Trim().TrimStart('\uFEFF');
        if (contenido.Contains("BEGIN CERTIFICATE"))
        {
            // Normaliza saltos de línea y espacios que pueda agregar el Bloc de notas o el navegador.
            var texto = contenido.Replace("\r", "");
            if (!PemEncoding.TryFind(texto, out var campos))
                throw new FormatException("PEM inválido");
            var base64 = texto[campos.Base64Data];
            return Convert.FromBase64String(new string(base64.Where(ch => !char.IsWhiteSpace(ch)).ToArray()));
        }

        try
        {
            return Convert.FromBase64String(new string(contenido.Where(ch => !char.IsWhiteSpace(ch)).ToArray()));
        }
        catch (FormatException)
        {
            return Encoding.Latin1.GetBytes(contenido);
        }
    }

    /// <summary>¿La clave privada corresponde al certificado? (compara las claves públicas)</summary>
    public static bool ClaveCoincide(DatosCertificado cert, string clavePrivadaPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(clavePrivadaPem);
        return cert.ClavePublica.AsSpan().SequenceEqual(rsa.ExportSubjectPublicKeyInfo());
    }

    /// <summary>CUIT que figura en el sujeto del certificado (serialNumber=CUIT nnnnnnnnnnn).</summary>
    private static string? CuitDelSujeto(X500DistinguishedName sujeto)
    {
        foreach (var rdn in sujeto.EnumerateRelativeDistinguishedNames())
        {
            if (rdn.HasMultipleElements) continue;
            if (rdn.GetSingleElementType().Value == OidSerialNumber)
            {
                var valor = rdn.GetSingleElementValue() ?? string.Empty;
                var digitos = new string(valor.Where(char.IsDigit).ToArray());
                return digitos.Length == 11 ? digitos : null;
            }
        }
        return null;
    }

    /// <summary>
    /// Arma el certificado con su clave privada listo para firmar.
    /// Se re-importa como PKCS#12 porque en Windows las claves "efímeras" de PEM no sirven para firmar CMS.
    /// </summary>
    public static X509Certificate2 ParaFirmar(string certificadoPem, string clavePrivadaPem)
    {
        using var temporal = X509Certificate2.CreateFromPem(certificadoPem, clavePrivadaPem);
        var pfx = temporal.Export(X509ContentType.Pkcs12);
        return new X509Certificate2(pfx, (string?)null, X509KeyStorageFlags.Exportable);
    }
}
