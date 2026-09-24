# Facturación electrónica ARCA — puesta en marcha en homologación (pruebas)

"Homologación" es el servidor de **pruebas** de ARCA. Las facturas que se emiten ahí tienen CAE
pero **no tienen validez fiscal**, así que podés probar todo lo que quieras sin riesgo.

Se hace una sola vez y lleva unos 15 minutos. Necesitás tu **CUIT y clave fiscal nivel 3**
(la tuya personal sirve para probar; más adelante se usa la de la estación).

---

## Paso 1 — Datos de la empresa (en el programa)

1. Entrá con un usuario **Administrador**.
2. Menú **ADMINISTRACIÓN → Configuración fiscal**.
3. En "1. Datos de la empresa" completá CUIT, razón social y condición de IVA.
   Entorno: **Homologación (pruebas)**. Tocá **Guardar datos**.

## Paso 2 — Generar la solicitud de certificado (en el programa)

1. En "2. Certificado digital" dejá el alias `estacionerp` (o el nombre que quieras, sin espacios).
2. Tocá **A. Generar solicitud (.csr)** y guardá el archivo (por ejemplo en el Escritorio).
   El texto también queda copiado en el portapapeles.

> La clave privada se genera y queda guardada en la base de datos del programa. No se la pasás a nadie.

## Paso 3 — Habilitar el servicio de certificados de prueba en ARCA (una sola vez)

1. Entrá a <https://auth.afip.gob.ar/contribuyente_/login.xhtml> con tu CUIT y clave fiscal.
2. Buscá **"Administrador de Relaciones de Clave Fiscal"**.
3. **Adherir servicio** → ARCA → Servicios interactivos →
   **"WSASS - Autogestión Certificados Homologación"** → Confirmar.
4. Cerrá sesión y volvé a entrar para que aparezca el servicio nuevo.

## Paso 4 — Obtener el certificado (en ARCA, servicio WSASS)

1. Entrá a **WSASS - Autogestión Certificados Homologación**.
2. Menú **Nuevo Certificado**:
   - Nombre simbólico del DN: el mismo alias (`estacionerp`).
   - CUIT del contribuyente: tu CUIT.
   - Solicitud de certificado en formato PKCS#10: abrí el `.csr` con el Bloc de notas y pegá **todo**
     el texto (incluidas las líneas `-----BEGIN CERTIFICATE REQUEST-----` y `-----END ...-----`).
   - **Crear DN y obtener certificado**.
3. ARCA muestra el certificado (texto que empieza con `-----BEGIN CERTIFICATE-----`).
   Copialo entero, pegalo en el Bloc de notas y guardalo como `estacionerp.crt`
   (en "Tipo" elegí "Todos los archivos" para que no quede `.crt.txt`).

## Paso 5 — Autorizar el certificado a facturar (en ARCA, WSASS)

1. Menú **Crear autorización a servicio**.
2. Nombre simbólico del DN: elegí `estacionerp`.
3. CUIT representado: tu CUIT (el mismo).
4. Servicio al que desea acceder: **wsfe - Facturación Electrónica**.
5. **Crear autorización de acceso**.

## Paso 6 — Importar y probar (en el programa)

1. Volvé a **Configuración fiscal** y tocá **B. Importar certificado (.crt)**; elegí el archivo.
2. Cargá al menos un punto de venta (paso 7.1) y tocá **C. Probar conexión**. Tenés que ver algo así:

```
OK - Servidores de ARCA funcionando.
OK - Certificado aceptado: autenticación correcta (WSAA).
OK - Facturación habilitada: último Factura B del punto de venta 0001 es el N° 0.
```

## Paso 7 — Puntos de venta y primera factura

1. En "3. Puntos de venta" cargá uno por unidad de negocio. En homologación sirve cualquier número:
   `1` Playa, `2` Repuestos, `3` Lavadero.
2. Menú **Facturación ARCA** → elegí unidad, punto de venta, cliente (por defecto Consumidor Final),
   agregá productos o una línea libre → **Emitir comprobante**.
3. Si sale bien, te muestra el número y el **CAE**.

---

## Cómo decide el programa el tipo de factura

| Emisor (la estación)    | Cliente                         | Comprobante |
|-------------------------|---------------------------------|-------------|
| Responsable Inscripto   | Responsable Inscripto / Monotributo | **A**   |
| Responsable Inscripto   | Consumidor Final / Exento / otros   | **B**   |
| Monotributo / Exento    | cualquiera                      | **C**       |

- Los precios se cargan **finales (con IVA)**. En A y B el programa separa neto e IVA por alícuota.
- A Consumidor Final **desde $10.000.000** hay que identificarlo (DNI/CUIT), si no ARCA lo rechaza.
- Se informa siempre la **condición de IVA del receptor** (obligatoria por RG 5616).
- Lavadero (servicios) informa además fechas de servicio.

## Estados de un comprobante

- **Autorizado**: tiene CAE. Listo.
- **Rechazado**: ARCA lo rechazó (el motivo aparece abajo de la lista). No consume número. Corregí y emití de nuevo.
- **Pendiente**: se cortó internet o ARCA no respondió. **No lo cargues de nuevo**: en
  "Comprobantes emitidos" tocá **Reintentar**. El programa primero consulta si ARCA llegó a
  autorizarlo, para no duplicar números.

## Problemas comunes

| Mensaje | Qué hacer |
|---|---|
| "ARCA no reconoce el certificado" | El certificado es de otro entorno o no es el que generó esta solicitud. Repetí pasos 2, 4 y 6. |
| "no tiene autorizado el servicio wsfe" | Falta el paso 5. |
| "la fecha y hora de esta PC no coinciden" | Windows → Configuración → Hora → **Sincronizar ahora**. |
| "ya entregó un ticket vigente" | Pasa si borraste la base o usás el mismo certificado en otra PC. Esperá unos minutos (como máximo 12 hs). |
| "El certificado no corresponde a la clave generada" | Importaste un certificado de otra solicitud. Usá el último `.csr` generado. |

## Para producción (más adelante, etapa 7)

Se repite lo mismo pero con el servicio **"Administración de Certificados Digitales"** (no WSASS),
se da de alta el punto de venta real como **"RECE para aplicativo y web services"** en
"Administración de puntos de venta y domicilios", y en el programa se cambia el entorno a Producción.
