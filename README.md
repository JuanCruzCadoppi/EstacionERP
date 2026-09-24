# Estación ERP

Sistema de gestión para la estación de servicio: **Playa / Combustibles**, **Repuestos** y **Lavadero**, con facturación electrónica ARCA, ventas, stock, caja y cuentas corrientes.

Diseño completo en [`docs/Diseno.md`](docs/Diseno.md).

## Tecnología

- C# / .NET 8 (LTS)
- WPF (aplicación de escritorio) con patrón MVVM (CommunityToolkit.Mvvm)
- PostgreSQL + Entity Framework Core 8
- xUnit para pruebas

## Estructura

```
EstacionERP.sln
├── src/
│   ├── EstacionERP.Domain/          Entidades, enums (con códigos ARCA) y validaciones (CUIT/DNI)
│   ├── EstacionERP.Application/     Casos de uso (ClienteService) y reglas de negocio
│   ├── EstacionERP.Infrastructure/  EF Core, PostgreSQL, migraciones
│   └── EstacionERP.Desktop/         WPF: ventanas, vistas y ViewModels
├── tests/
│   └── EstacionERP.Tests/           Pruebas automáticas (SQLite en memoria)
└── docs/
    └── Diseno.md                    Documento de diseño
```

Regla de dependencias: `Desktop → Infrastructure → Application → Domain`. El dominio no conoce la base ni las pantallas.

## Cómo ponerlo en marcha (Windows)

1. **Instalar** [Visual Studio 2026 o 2022](https://visualstudio.microsoft.com/es/downloads/) (Community) con la carga de trabajo **"Desarrollo de escritorio de .NET"**.
2. **Instalar** [PostgreSQL](https://www.postgresql.org/download/windows/) (16 o superior). Durante la instalación anotá la contraseña del usuario `postgres`.
3. **Configurar la conexión**: crear el archivo `src/EstacionERP.Desktop/appsettings.Local.json` (no se sube a Git) con:
   ```json
   {
     "ConnectionStrings": {
       "Estacion": "Host=localhost;Port=5432;Database=estacion_erp;Username=postgres;Password=TU_CONTRASEÑA"
     }
   }
   ```
4. Abrir `EstacionERP.sln`, marcar **EstacionERP.Desktop** como proyecto de inicio y apretar **F5**.

La primera vez la aplicación crea sola la base `estacion_erp`, las tablas y los datos iniciales (las 3 unidades de negocio y el cliente "Consumidor Final").

## Pruebas

En Visual Studio: **Prueba → Ejecutar todas las pruebas**, o por consola:

```
dotnet test
```

## Migraciones (cuando cambiemos el modelo)

```
dotnet tool restore
dotnet ef migrations add NombreDelCambio -p src/EstacionERP.Infrastructure -s src/EstacionERP.Infrastructure -o Persistencia/Migraciones
```

La app aplica las migraciones pendientes al iniciar.

## Avance

- [x] **Etapa 1a** — Estructura, base de datos, unidades de negocio, ABM de clientes
- [ ] Etapa 1b — Productos, usuarios y login
- [ ] Etapa 2 — Facturación ARCA en homologación (WSAA + WSFEv1)
- [ ] Etapa 3 — Repuestos (ventas de mostrador y stock)
- [ ] Etapa 4 — Caja y cuentas corrientes
- [ ] Etapa 5 — Lavadero
- [ ] Etapa 6 — Playa (tanques, mangueras, turnos, aforadores)
- [ ] Etapa 7 — Reportes, backups y pase a producción
