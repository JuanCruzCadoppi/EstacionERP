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
        byte[] der;
        try
        {
            der = Decodificar(contenido);
        }
        catch (Exception ex)
        {
            throw new FormatException("no se encontró un certificado en el archivo (" + ex.Message + ")", ex);
        }

        try
        {
            return LeerDer(der);
        }
        catch (Exception ex)
        {
            throw new FormatException("el contenido no tiene formato de certificado X.509 (" + ex.Message + ")", ex);
        }
    }

    /// <summary>
    /// Lee el certificado X.509 (DER) con código propio, sin usar el almacén de certificados de Windows.
    /// Certificate ::= SEQUENCE { tbsCertificate, signatureAlgorithm, signature }
    /// tbsCertificate ::= SEQUENCE { [0] version, serialNumber, signature, issuer, validity, subject, subjectPublicKeyInfo, ... }
    /// </summary>
    private static DatosCertificado LeerDer(byte[] der)
    {
        var certificado = new AsnReader(der, AsnEncodingRules.DER).ReadSequence();
        var tbs = certificado.ReadSequence();

        if (tbs.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            tbs.ReadEncodedValue();                 // version
        tbs.ReadEncodedValue();                     // serialNumber
        tbs.ReadEncodedValue();                     // signature algorithm
        tbs.ReadEncodedValue();                     // issuer

        var validez = tbs.ReadSequence();
        LeerFecha(validez);                         // notBefore
        var vence = LeerFecha(validez);             // notAfter

        var sujeto = tbs.ReadSequence();
        var cuit = CuitDelSujeto(sujeto);

        var clavePublica = tbs.ReadEncodedValue().ToArray();   // SubjectPublicKeyInfo completo

        var pem = new string(PemEncoding.Write("CERTIFICATE", der)) + "\n";
        return new DatosCertificado(pem, vence.ToLocalTime().DateTime, cuit, clavePublica);
    }

    private static DateTimeOffset LeerFecha(AsnReader r) =>
        r.PeekTag().HasSameClassAndValue(Asn1Tag.UtcTime) ? r.ReadUtcTime() : r.ReadGeneralizedTime();

    /// <summary>Busca serialNumber=CUIT nnnnnnnnnnn dentro del sujeto (Name ::= SEQUENCE OF SET OF AttributeTypeAndValue).</summary>
    private static string? CuitDelSujeto(AsnReader sujeto)
    {
        while (sujeto.HasData)
        {
            var conjunto = sujeto.ReadSetOf(skipSortOrderValidation: true);
            while (conjunto.HasData)
            {
                var atributo = conjunto.ReadSequence();
                var oid = atributo.ReadObjectIdentifier();
                var tag = atributo.PeekTag();
                string valor;
                try
                {
                    valor = tag.TagClass == TagClass.Universal
                        ? atributo.ReadCharacterString((UniversalTagNumber)tag.TagValue)
                        : string.Empty;
                }
                catch (Exception)
                {
                    atributo.ReadEncodedValue();
                    valor = string.Empty;
                }

                if (oid == OidSerialNumber)
                {
                    var digitos = new string(valor.Where(char.IsDigit).ToArray());
                    if (digitos.Length == 11) return digitos;
                }
            }
        }
        return null;
    }

    private static byte[] Decodificar(string contenido)
    {
        contenido = contenido.Trim().TrimStart('\uFEFF');
        if (contenido.Contains("BEGIN CERTIFICATE"))
        {
            // Normaliza saltos de línea y espacios que pueda agregar el Bloc de notas o el navegador.
            var texto = contenido.Replace("\r", "");
            const string inicio = "-----BEGIN CERTIFICATE-----";
            const string fin = "-----END CERTIFICATE-----";
            var desde = texto.IndexOf(inicio, StringComparison.Ordinal) + inicio.Length;
            var hasta = texto.IndexOf(fin, desde, StringComparison.Ordinal);
            if (hasta < 0) throw new FormatException("falta la línea END CERTIFICATE");
            var base64 = texto[desde..hasta];
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
