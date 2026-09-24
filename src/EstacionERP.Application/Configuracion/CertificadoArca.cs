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

    /// <summary>Acepta el certificado en PEM (texto con BEGIN CERTIFICATE) o DER/base64.</summary>
    public static X509Certificate2 LeerCertificado(string contenido)
    {
        contenido = contenido.Trim();
        if (contenido.Contains("BEGIN CERTIFICATE"))
            return X509Certificate2.CreateFromPem(contenido);

        try
        {
            return new X509Certificate2(Convert.FromBase64String(contenido));
        }
        catch (FormatException)
        {
            return new X509Certificate2(Encoding.Latin1.GetBytes(contenido));
        }
    }

    public static string APem(X509Certificate2 cert) => cert.ExportCertificatePem();

    /// <summary>¿La clave privada corresponde al certificado?</summary>
    public static bool ClaveCoincide(X509Certificate2 cert, string clavePrivadaPem)
    {
        using var rsaCert = cert.GetRSAPublicKey();
        if (rsaCert is null) return false;
        using var rsa = RSA.Create();
        rsa.ImportFromPem(clavePrivadaPem);
        return rsaCert.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(rsa.ExportSubjectPublicKeyInfo());
    }

    /// <summary>CUIT que figura en el sujeto del certificado (serialNumber=CUIT nnnnnnnnnnn).</summary>
    public static string? CuitDelCertificado(X509Certificate2 cert)
    {
        foreach (var rdn in cert.SubjectName.EnumerateRelativeDistinguishedNames())
        {
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
