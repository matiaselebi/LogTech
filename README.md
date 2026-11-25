# LogTech

Plataforma web B2B de logística que conecta **dadores de carga** con **transportistas** y **flotas de camiones**.
El dador publica una carga, el transportista (o la flota) la toma desde la bolsa de cargas, y la plataforma
acompaña todo el viaje: documentación, seguimiento, remito digital, pago con retención (escrow), facturación,
seguros, factoring y suscripciones.

Trabajo final de la carrera de Ingeniería en Sistemas Informáticos (Universidad Abierta Interamericana).

> Los pagos, ARCA (facturación electrónica), las aseguradoras y la entidad de factoring están **simulados**:
> no se conecta con ningún servicio real ni se mueve dinero.

---

## Funcionalidades

**Operación logística**
- Bolsa de cargas con búsqueda por origen y destino, y orden por cercanía.
- Publicación de cargas con cotización automática del flete y tres medios de pago: efectivo, tarjeta o saldo de la billetera.
- Ciclo de vida del viaje: asignado, en camino a carga, en origen, en tránsito, en destino y entregado. Cada viaje tiene línea de tiempo, incidencias con foto y calificación del transportista.
- Remito digital inmutable, firmado con un token SHA-256 y exportable en XML (con XSLT).
- Flota SaaS: el administrador de flota da de alta vehículos y conductores y les asigna viajes, y tiene un reporte de la flota.

**Pagos y negocio (simulados)**
- Billetera con tarjetas, saldo, retiros a CBU y movimientos.
- Retención escrow: la plata del dador queda retenida hasta que se emite el remito.
- Disputas comerciales que resuelve el administrador.
- Factoring: el transportista adelanta el cobro de un viaje en efectivo.
- Facturas electrónicas con CAE simulado, imprimibles y exportables.
- Seguros de carga.
- Suscripciones: Flota SaaS, Premium para transportistas y protección de carga.

**Documentación legal**
- Cada cuenta, vehículo y conductor tiene su documentación obligatoria, con fecha de vencimiento (CUIT, licencia, CNRT, seguro, VTV, cédula verde, DNI, antecedentes, etc.).
- El administrador valida cada documento.
- Un semáforo indica si la cuenta puede operar.
- Se envían avisos antes de cada vencimiento.

**Administración y seguridad**
- Usuarios, roles, familias y permisos (patrón Composite). Además de su rol, cada usuario puede tener permisos y familias propios.
- Los menús y las opciones se muestran según los permisos.
- Cada intento de usar una opción sin permiso queda registrado.
- Bitácora de auditoría con filtros. Los eventos de más de 30 días pasan a un historial XML.
- Dígitos verificadores horizontales y verticales (DVH/DVV) en todas las tablas, con verificación de integridad y recálculo.
- Backup y restauración de la base desde la aplicación.
- Un usuario de contingencia puede entrar sin base de datos para restaurar un backup.
- Interfaz en tres idiomas: español, inglés y portugués.

---

## Tecnologías

| Capa | Tecnología |
|---|---|
| Presentación | ASP.NET Web Forms (.NET Framework 4.7.2), controles de usuario (.ascx), Bootstrap 5, jQuery |
| Negocio | C# |
| Datos | SQL Server (T-SQL, procedimientos almacenados), ADO.NET |
| Servicios web | SOAP (.asmx) |
| Otros | XML / XSLT, Newtonsoft.Json, System.Web.DataVisualization (gráficos del tablero) |

### Arquitectura

El proyecto está organizado en capas. Cada capa solo habla con la de abajo.

```
Páginas (.aspx) y controles (Controles/*.ascx)
        │
Fachada/        → patrón Facade: única puerta de la presentación a la lógica de negocio
        │
BLL/            → reglas de negocio
        │
DAL/            → acceso a datos (ADO.NET y procedimientos almacenados)
        │
SQL Server (LogTechDB)

BE/             → entidades que viajan entre capas
Servicios/      → servicios transversales (cifrado, seguridad, correo, archivos, traducción, sesión)
```

**Patrones de diseño:** Facade, Composite (roles, familias y permisos), Strategy (facturación), Observer
(cambios de estado del viaje) y Singleton (gestor de sesión).

### Seguridad

- Contraseñas guardadas como SHA-256 con salt por usuario y política de contraseñas (8 caracteres o más, con una mayúscula y un carácter especial).
- Bloqueo de la cuenta tras 3 intentos fallidos.
- Recuperación de contraseña por correo, con un token aleatorio de un solo uso.
- Datos personales (correo, teléfono, CUIT/CUIL, licencia, DNI y CBU) cifrados con AES-256 en la base.
- Integridad de cada tabla con dígitos verificadores (DVH/DVV).
- Bitácora de auditoría de todas las acciones relevantes y de los accesos denegados.
- HTTPS forzado (configurable).
- La clave de API del seguimiento de viajes se guarda como hash SHA-256 y se muestra una sola vez.

### Servicios web (SOAP)

| Servicio | Método | Uso |
|---|---|---|
| `WS_CotizadorFletes.asmx` | `CalcularTarifa(origen, destino, pesoToneladas)` | Cotiza un flete |
| `WS_TrackingViajes.asmx` | `ObtenerUbicacion(claveApi, idViaje)` | Seguimiento de un viaje. El dador genera la clave en *Mi perfil* |
| `WS_EmisionPolizas.asmx` | `CotizarPlanes(aseguradora)` | Planes de una aseguradora (simulada) |
| `WS_FactoringLogistico.asmx` | `CotizarAdelanto(monto)` | Cotiza un adelanto de cobro (simulado) |

---

## Estructura del repositorio

```
LogTech.slnx                    Solución de Visual Studio
LogTech/
├── LogTechDB.sql               Script de la base de datos (estructura, procedimientos y datos iniciales)
├── Web.config                  Configuración (cadena de conexión, clave de cifrado, HTTPS, etc.)
├── Web.Mail.config.ejemplo     Plantilla para configurar el correo
├── App_Data/Traducciones/      Textos de la interfaz en cada idioma (JSON)
├── BE/ BLL/ DAL/ Fachada/      Capas de la aplicación
├── Servicios/                  Servicios transversales
├── Controles/                  Controles de usuario (módulos del panel)
├── Xslt/                       Transformación XSLT del remito digital
├── WS_*.asmx                   Servicios web
└── *.aspx                      Páginas
```

---

## Requisitos

- Windows 10/11.
- **Visual Studio 2022 (17.13 o superior) o Visual Studio 2026**, con la carga de trabajo *Desarrollo de ASP.NET y web*. Incluye IIS Express y el paquete de destino de .NET Framework 4.7.2.
- **SQL Server 2017 o superior**. Sirve la edición Express o Developer.
- **SQL Server Management Studio (SSMS)**, para ejecutar el script de la base.

---

## Instalación y configuración

### 1. Crear la base de datos

1. Abrí `LogTech/LogTechDB.sql` en SSMS.
2. Ejecutalo completo (F5).

El script crea la base `LogTechDB`, con las tablas, los procedimientos almacenados y los datos iniciales.

> ⚠️ Si ya existe una base llamada `LogTechDB`, el script **la borra** y la vuelve a crear.

Los datos iniciales son lo mínimo para que el sistema funcione:
- los 4 roles, con sus permisos;
- el catálogo de permisos;
- las ciudades;
- los planes de seguro;
- el usuario administrador.

### 2. Cadena de conexión

En `LogTech/Web.config`, sección `connectionStrings`:

```xml
<add name="LogTechDB" connectionString="Data Source=localhost;Initial Catalog=LogTechDB;Integrated Security=True" providerName="System.Data.SqlClient" />
```

- Con **SQL Server Express** (instancia con nombre), usá `Data Source=.\SQLEXPRESS`.
- Con **usuario y contraseña de SQL Server**, reemplazá `Integrated Security=True` por `User ID=usuario;Password=contraseña`.

### 3. Clave de cifrado (recomendado)

Los datos personales se cifran con la clave `ClaveCifrado` de `Web.config`. El repositorio trae una clave de ejemplo.
Para usar una propia, generá una nueva en PowerShell:

```powershell
$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

Después reemplazá el valor de `ClaveCifrado` en `Web.config` por el que te dio el comando.

> Hacelo **antes de cargar datos**. Lo que se cifró con una clave no se puede leer con otra.
> El correo y el teléfono del admin del script están cifrados con la clave de ejemplo. Si cambiás la clave,
> actualizalos desde *Mi perfil*. El inicio de sesión con el nombre de usuario sigue funcionando igual.

### 4. Correo (opcional)

Sin configuración, los mails (recuperación de contraseña, avisos y facturas) no se envían. Se guardan como archivos
`.eml` en `LogTech/App_Data/Mails`, y así se pueden abrir para probar.

Para enviarlos de verdad con Gmail:

1. Copiá `LogTech/Web.Mail.config.ejemplo` con el nombre `LogTech/Web.Mail.config`.
2. Completá tu cuenta y una **contraseña de aplicación** de Google. Se crea en *Cuenta de Google → Seguridad → Verificación en dos pasos → Contraseñas de aplicaciones*.

`Web.Mail.config` está en `.gitignore`: nunca se sube al repositorio.

### 5. Ejecutar

1. Abrí `LogTech.slnx` en Visual Studio. Los paquetes NuGet se restauran solos al compilar.
2. Presioná **F5**. El sitio abre en `https://localhost:44303`.
   - La primera vez, Visual Studio pide confiar en el certificado de desarrollo de IIS Express. Aceptalo.

---

## Usuarios iniciales

| Usuario | Contraseña | Uso |
|---|---|---|
| `admin` | `admin` | Administrador del sistema. **Cambiá la contraseña al entrar** (*Mi perfil*). |
| `contingencia` | `LogTech-Restore-2026` | Entra **sin base de datos**, para restaurar un backup o recalcular los dígitos verificadores. Se configura en `Web.config`. |

Para cambiar la contraseña de contingencia:
1. Calculá el SHA-256 de *salt + contraseña*, en hexadecimal y mayúsculas. El salt es el valor de `ContingenciaClaveSalt` en `Web.config`.
2. Poné el resultado en `ContingenciaClaveHash`.

En PowerShell:

```powershell
$salt = "contingencia-salt-2026"; $clave = "MiNuevaClave#2026"
-join ([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($salt + $clave)) | ForEach-Object { $_.ToString("X2") })
```

Las demás cuentas se crean desde **Crear cuenta**, en la pantalla de inicio. Hay tres perfiles:
- *Soy dador de carga*;
- *Soy transportista*;
- *Tengo una flota de camiones*.

---

## Recorrido de prueba

1. **Crear un dador de carga.** Desde *Crear cuenta*, con un CUIT válido (con dígito verificador).
2. **Subir su documentación.** En *Mi documentación*, el dador sube su Constancia de CUIT (PDF).
3. **Crear un transportista.** El transportista sube, desde *Mi perfil*:
   - su licencia y su constancia de CUIT;
   - su camión, con el seguro, la VTV y la cédula verde.
4. **Aprobar la documentación.** Entrá como `admin` y aprobá los documentos en *Validar documentación*. El semáforo de cada cuenta pasa a verde.
5. **Publicar y tomar un viaje.**
   - El dador publica una carga en *Mis cargas*.
   - El transportista la toma desde la *Bolsa de cargas*.
   - El transportista avanza el viaje en *Mis viajes* hasta entregarla. Ahí se emite el remito y se libera el pago.
6. **Contratar la Flota SaaS (opcional).** Un dador sin cargas publicadas la contrata desde *Suscripción*. Así su cuenta pasa a ser de flota y puede cargar vehículos y conductores.

En `LogTech/Web.config` se puede simular que ARCA o las aseguradoras no responden (`ArcaDisponible`, `AseguradoraDisponible`).

---

## Publicar en IIS

1. **Publicar.** En Visual Studio, publicá el proyecto (*Publicar → Carpeta*) en modo Release.
2. **Crear el sitio.** En IIS, creá un sitio que apunte a esa carpeta:
   - grupo de aplicaciones con **.NET CLR v4.0** y canalización **integrada**;
   - un binding **https** con un certificado.
3. **Configurar el puerto HTTPS.** En `Web.config`, poné en `PuertoHttps` el puerto https del sitio (por ejemplo `8443`). Si lo dejás vacío, no se fuerza HTTPS.
4. **Dar acceso a SQL Server.** Con `Integrated Security=True`, la identidad del grupo de aplicaciones (`IIS APPPOOL\<nombre del grupo>`) necesita un inicio de sesión en SQL Server. Asignale el rol `db_owner` sobre `LogTechDB`: el backup y la restauración desde la aplicación lo necesitan.
5. **Dar permiso de escritura en `App_Data`.** Esa misma identidad tiene que poder escribir en la carpeta `App_Data`. Ahí se guardan:
   - los archivos subidos;
   - los backups;
   - los mails;
   - el historial de la bitácora.
6. **Dar acceso a SQL Server en `App_Data\Backups`.** El backup lo escribe el servicio de SQL Server, así que su cuenta (por ejemplo `NT Service\MSSQLSERVER`) necesita permiso de *Modificar* en esa carpeta. La aplicación intenta darle ese permiso sola al crear la carpeta. Si no puede, dáselo a mano.

---

## Notas

- `App_Data/Archivos`, `App_Data/Backups`, `App_Data/Mails` y `App_Data/BitacoraHistorica.xml` se generan al usar la aplicación y están en `.gitignore`.
- Los textos que falten en una traducción se piden al servicio público MyMemory, que no usa clave.
