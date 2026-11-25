-- ============================================================================
-- LogTech: script de la base de datos LogTechDB (estructura y datos iniciales)
--
-- Compatible con SQL Server 2017 o superior. El archivo está en UTF-8 (con BOM)
-- para que SSMS respete los acentos.
--
-- Cómo usarlo: abrirlo en SQL Server Management Studio y ejecutar todo (F5).
-- No hace falta crear la base antes: el script la crea. Si ya existe una base
-- LogTechDB, la BORRA y la vuelve a crear.
--
-- Datos iniciales (lo mínimo para que el sistema funcione): Roles (4),
-- Permisos (35), RolPermiso (58), Ciudades (15), PlanesSeguro (6) y el usuario
-- administrador. Usuario: admin / Contraseña: admin (cambiala al entrar).
-- ============================================================================

USE master;
GO

-- Necesario para las columnas computadas persistidas (DVH). Sin esto el script
-- falla si se corre desde sqlcmd, que trae QUOTED_IDENTIFIER en OFF.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Script de creación completa. Elimina la base anterior para mantener el esquema sincronizado.
IF DB_ID('LogTechDB') IS NOT NULL
BEGIN
    ALTER DATABASE LogTechDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LogTechDB;
END;
GO

CREATE DATABASE LogTechDB;
GO

ALTER DATABASE LogTechDB SET COMPATIBILITY_LEVEL = 140;
GO

USE LogTechDB;
GO

-- Dígito verificador: protege TODAS las tablas (menos DVV, que guarda los verticales).
--   DVH: una firma SHA-256 por fila, guardada en la propia tabla.
--   DVV: una firma que resume todos los DVH de la tabla, guardada en la tabla DVV.
-- El DVH no se recalcula solo: lo fija dbo.RecalcularDigitos después de cada cambio
-- autorizado. Si alguien edita la base por afuera, el DVH deja de coincidir y
-- dbo.VerificarIntegridad puede señalar la fila.
CREATE TABLE Roles (
    IdRol INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL UNIQUE,
    EsFamilia BIT NOT NULL CONSTRAINT DF_Roles_EsFamilia DEFAULT 0,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Roles_DVH DEFAULT ''
);

CREATE TABLE Permisos (
    IdPermiso INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(60) NOT NULL UNIQUE,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Permisos_DVH DEFAULT ''
);

-- Correo, teléfono, CUIT/CUIL y licencia van cifrados con AES (CifradoServicio, clave en Web.config).
CREATE TABLE Usuarios (
    IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
    NombreUsuario NVARCHAR(50) NOT NULL UNIQUE,
    CorreoElectronico NVARCHAR(200) NOT NULL UNIQUE,
    ClaveHash CHAR(64) NOT NULL,
    ClaveSalt NVARCHAR(44) NOT NULL,
    Nombre NVARCHAR(60) NOT NULL,
    Apellido NVARCHAR(60) NOT NULL,
    Telefono NVARCHAR(100) NOT NULL,
    CuitCuil NVARCHAR(100) NOT NULL,
    RazonSocial NVARCHAR(120) NULL,
    NumeroLicencia NVARCHAR(100) NULL,
    -- Para la factura (CU-008-039): A si es responsable inscripto, B si no.
    CondicionIva NVARCHAR(30) NOT NULL CONSTRAINT DF_Usuarios_CondicionIva DEFAULT N'Consumidor final'
        CONSTRAINT CK_Usuarios_CondicionIva CHECK (CondicionIva IN (N'Responsable inscripto', N'Monotributista',
            N'Exento', N'Consumidor final')),
    Idioma VARCHAR(10) NOT NULL CONSTRAINT DF_Usuarios_Idioma DEFAULT 'es-ES'
        CONSTRAINT CK_Usuarios_Idioma CHECK (Idioma IN ('es-ES', 'en-GB', 'pt-PT')),
    IntentosFallidos INT NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT 0,
    BloqueadoHasta DATETIME2(0) NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT 'Activo'
        CONSTRAINT CK_Usuarios_Estado CHECK (Estado IN ('Activo', 'Inactivo', 'Bloqueado')),
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_Usuarios_FechaAlta DEFAULT SYSDATETIME(),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Usuarios_DVH DEFAULT ''
);

CREATE TABLE UsuarioRol (
    IdUsuario INT NOT NULL,
    IdRol INT NOT NULL,
    CONSTRAINT PK_UsuarioRol PRIMARY KEY (IdUsuario, IdRol),
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY (IdRol) REFERENCES Roles(IdRol),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_UsuarioRol_DVH DEFAULT ''
);

CREATE TABLE RolPermiso (
    IdRol INT NOT NULL,
    IdPermiso INT NOT NULL,
    CONSTRAINT PK_RolPermiso PRIMARY KEY (IdRol, IdPermiso),
    CONSTRAINT FK_RolPermiso_Rol FOREIGN KEY (IdRol) REFERENCES Roles(IdRol),
    CONSTRAINT FK_RolPermiso_Permiso FOREIGN KEY (IdPermiso) REFERENCES Permisos(IdPermiso),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_RolPermiso_DVH DEFAULT ''
);

-- Permisos particulares: los que se le dan a un usuario además de los de su rol (protocolo, punto 14).
CREATE TABLE UsuarioPermiso (
    IdUsuario INT NOT NULL,
    IdPermiso INT NOT NULL,
    CONSTRAINT PK_UsuarioPermiso PRIMARY KEY (IdUsuario, IdPermiso),
    CONSTRAINT FK_UsuarioPermiso_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT FK_UsuarioPermiso_Permiso FOREIGN KEY (IdPermiso) REFERENCES Permisos(IdPermiso),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_UsuarioPermiso_DVH DEFAULT ''
);

-- Familias que se le dan a un usuario además de su rol (patrón Composite: el usuario las contiene).
CREATE TABLE UsuarioFamilia (
    IdUsuario INT NOT NULL,
    IdFamilia INT NOT NULL,
    CONSTRAINT PK_UsuarioFamilia PRIMARY KEY (IdUsuario, IdFamilia),
    CONSTRAINT FK_UsuarioFamilia_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT FK_UsuarioFamilia_Familia FOREIGN KEY (IdFamilia) REFERENCES Roles(IdRol),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_UsuarioFamilia_DVH DEFAULT ''
);

CREATE TABLE RolFamilia (
    IdPadre INT NOT NULL REFERENCES Roles(IdRol),
    IdFamilia INT NOT NULL REFERENCES Roles(IdRol),
    CONSTRAINT PK_RolFamilia PRIMARY KEY (IdPadre, IdFamilia),
    CONSTRAINT CK_RolFamilia_NoMisma CHECK (IdPadre <> IdFamilia),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_RolFamilia_DVH DEFAULT ''
);

-- Enlaces de recuperación de contraseña. El usuario recibe el token por correo y lo
-- usa una sola vez desde RestablecerClave.aspx.
CREATE TABLE RecuperacionesClave (
    IdRecuperacion INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Token CHAR(64) NOT NULL UNIQUE,
    FechaExpira DATETIME2(0) NOT NULL,
    Usado BIT NOT NULL CONSTRAINT DF_RecuperacionesClave_Usado DEFAULT 0,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_RecuperacionesClave_DVH DEFAULT ''
);

-- Claves de API de los dadores para consultar el seguimiento de sus viajes desde sus propios
-- sistemas (WS_TrackingViajes). Se guarda el SHA-256 de la clave, nunca la clave: el dador la ve
-- una sola vez, al generarla. Generar una nueva deja inactiva la anterior.
CREATE TABLE ClavesApi (
    IdClave INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Hash CHAR(64) NOT NULL UNIQUE,
    Ultimos4 NCHAR(4) NOT NULL,
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_ClavesApi_FechaAlta DEFAULT SYSDATETIME(),
    FechaUltimoUso DATETIME2(0) NULL,
    Activa BIT NOT NULL CONSTRAINT DF_ClavesApi_Activa DEFAULT 1,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_ClavesApi_DVH DEFAULT ''
);
CREATE INDEX IX_ClavesApi_Usuario ON ClavesApi (IdUsuario);

CREATE TABLE BitacoraEventos (
    IdEvento BIGINT IDENTITY(1,1) PRIMARY KEY,
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_BitacoraEventos_Fecha DEFAULT SYSDATETIME(),
    IdUsuario INT NULL,
    Usuario NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(250) NOT NULL,
    Criticidad NVARCHAR(20) NOT NULL CONSTRAINT DF_BitacoraEventos_Criticidad DEFAULT 'Informativa'
        CONSTRAINT CK_BitacoraEventos_Criticidad CHECK (Criticidad IN ('Informativa', 'Advertencia', 'Crítica')),
    CONSTRAINT FK_BitacoraEventos_Usuario FOREIGN KEY (IdUsuario)
        REFERENCES Usuarios(IdUsuario) ON DELETE SET NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_BitacoraEventos_DVH DEFAULT ''
);

-- Un DVV (dígito verificador vertical) por tabla protegida.
CREATE TABLE DVV (
    Tabla SYSNAME PRIMARY KEY,
    Digito CHAR(64) NOT NULL,
    FechaCalculo DATETIME2(0) NOT NULL CONSTRAINT DF_DVV_Fecha DEFAULT SYSDATETIME()
);

-- ====================================================================== NEGOCIO
-- Ciudades con sus coordenadas: origen y destino de los viajes y punto de referencia
-- para el motor de asignación (R4: distancia entre el usuario y el origen de la carga).
CREATE TABLE Ciudades (
    IdCiudad INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(60) NOT NULL UNIQUE,
    Provincia NVARCHAR(60) NOT NULL,
    Latitud DECIMAL(9,6) NOT NULL,
    Longitud DECIMAL(9,6) NOT NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Ciudades_DVH DEFAULT ''
);

-- Camiones de los transportistas y de las flotas (CU-002-007 a 010).
CREATE TABLE Vehiculos (
    IdVehiculo INT IDENTITY(1,1) PRIMARY KEY,
    IdPropietario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Patente NVARCHAR(10) NOT NULL,
    Marca NVARCHAR(40) NOT NULL,
    Modelo NVARCHAR(40) NOT NULL,
    CapacidadToneladas DECIMAL(6,2) NOT NULL
        CONSTRAINT CK_Vehiculos_Capacidad CHECK (CapacidadToneladas > 0 AND CapacidadToneladas <= 60),
    TipoAcoplado NVARCHAR(20) NOT NULL
        CONSTRAINT CK_Vehiculos_TipoAcoplado CHECK (TipoAcoplado IN (N'Sin acoplado', N'Acoplado', N'Semirremolque')),
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Vehiculos_Estado DEFAULT 'Activo'
        CONSTRAINT CK_Vehiculos_Estado CHECK (Estado IN ('Activo', 'Inactivo')),
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_Vehiculos_FechaAlta DEFAULT SYSDATETIME(),
    -- La flota puede dejar un conductor fijo para el camión (por ejemplo, el dueño del camión que trabaja en la flota).
    -- La clave foránea va después de crear Conductores.
    IdConductorDesignado INT NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Vehiculos_DVH DEFAULT ''
);
-- R5: la patente no puede repetirse entre los vehículos activos.
CREATE UNIQUE INDEX UX_Vehiculos_PatenteActiva ON Vehiculos (Patente) WHERE Estado = 'Activo';

-- Conductores propios de una flota (CU-002-011 a 014). No son usuarios del sistema:
-- los maneja el administrador de flota.
CREATE TABLE Conductores (
    IdConductor INT IDENTITY(1,1) PRIMARY KEY,
    IdFlota INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Nombre NVARCHAR(60) NOT NULL,
    Apellido NVARCHAR(60) NOT NULL,
    Dni NVARCHAR(100) NOT NULL,
    NumeroLicencia NVARCHAR(100) NOT NULL,
    Telefono NVARCHAR(100) NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Conductores_Estado DEFAULT 'Activo'
        CONSTRAINT CK_Conductores_Estado CHECK (Estado IN ('Activo', 'Inactivo')),
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_Conductores_FechaAlta DEFAULT SYSDATETIME(),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Conductores_DVH DEFAULT ''
);
-- R7: el DNI no puede repetirse entre los conductores activos.
CREATE UNIQUE INDEX UX_Conductores_DniActivo ON Conductores (Dni) WHERE Estado = 'Activo';
ALTER TABLE Vehiculos ADD CONSTRAINT FK_Vehiculos_ConductorDesignado FOREIGN KEY (IdConductorDesignado)
    REFERENCES Conductores(IdConductor);

-- Tarjetas de los usuarios (CU-005-025): el dador paga viajes con ellas y el transportista o la flota
-- carga saldo. Nunca se guarda el número completo ni el código de seguridad: la pasarela de pago
-- (simulada) valida la tarjeta (R21) y devuelve un token que la representa.
CREATE TABLE Tarjetas (
    IdTarjeta INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Titular NVARCHAR(80) NOT NULL,
    Marca NVARCHAR(20) NOT NULL,
    Ultimos4 NCHAR(4) NOT NULL,
    Vencimiento CHAR(5) NOT NULL,      -- MM/AA
    Token CHAR(32) NOT NULL UNIQUE,
    Activa BIT NOT NULL CONSTRAINT DF_Tarjetas_Activa DEFAULT 1,
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_Tarjetas_FechaAlta DEFAULT SYSDATETIME(),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Tarjetas_DVH DEFAULT ''
);
CREATE INDEX IX_Tarjetas_Usuario ON Tarjetas (IdUsuario);

-- CBU o alias donde se le deposita al transportista o a la flota lo que retira de la billetera (CU-005-026).
CREATE TABLE CuentasDeposito (
    IdUsuario INT PRIMARY KEY REFERENCES Usuarios(IdUsuario),
    CbuOAlias NVARCHAR(100) NOT NULL,
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_CuentasDeposito_FechaAlta DEFAULT SYSDATETIME(),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_CuentasDeposito_DVH DEFAULT ''
);

-- Planes de las aseguradoras (simuladas) que se contratan desde la aplicación (CU-006-031). El precio es por mes;
-- la aseguradora le paga a LogTech el 5 % de la prima como comisión.
CREATE TABLE PlanesSeguro (
    IdPlan INT IDENTITY(1,1) PRIMARY KEY,
    Aseguradora NVARCHAR(60) NOT NULL,
    Nombre NVARCHAR(40) NOT NULL,
    CoberturaPorViaje DECIMAL(14,2) NOT NULL,   -- valor máximo de la carga que cubre en cada viaje
    ViajesPorMes INT NULL,                      -- NULL = sin límite
    Precio DECIMAL(12,2) NOT NULL,              -- por mes
    Detalle NVARCHAR(300) NOT NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_PlanesSeguro_DVH DEFAULT ''
);

-- Suscripciones: las tres que define el modelo de negocio.
--   Flota SaaS: el administrador de flota paga un abono mensual por cada camión (patente).
--   Premium transportista: costo fijo mensual; el transportista cobra el 100 % del flete.
--   Protección de carga: seguro mensual de la carga, contratado a una aseguradora desde la aplicación
--   (CU-006-031); LogTech cobra el 5 % de la prima.
-- Las filas con IdUsuario NULL son suscripciones de ejemplo del tablero (clientes sin cuenta en el sistema).
CREATE TABLE Suscripciones (
    IdSuscripcion INT IDENTITY(1,1) PRIMARY KEY,
    Suscriptor NVARCHAR(120) NOT NULL,
    Tipo NVARCHAR(30) NOT NULL
        CONSTRAINT CK_Suscripciones_Tipo CHECK (Tipo IN (N'Flota SaaS', N'Premium transportista', N'Protección de carga')),
    CantidadPatentes INT NULL,    -- solo Flota SaaS
    Monto DECIMAL(12,2) NOT NULL, -- lo que paga por mes
    FechaAlta DATETIME2(0) NOT NULL CONSTRAINT DF_Suscripciones_FechaAlta DEFAULT SYSDATETIME(),
    FechaBaja DATETIME2(0) NULL,
    -- Seguro contratado desde la aplicación (CU-006-031 a CU-006-034).
    IdUsuario INT NULL REFERENCES Usuarios(IdUsuario),
    IdPlanSeguro INT NULL REFERENCES PlanesSeguro(IdPlan),
    IdTarjeta INT NULL REFERENCES Tarjetas(IdTarjeta),
    NumeroPoliza NVARCHAR(30) NULL,
    Vence DATETIME2(0) NULL,      -- fin del mes pagado: ahí se renueva, salvo que se haya cancelado
    CancelaAlVencer BIT NOT NULL CONSTRAINT DF_Suscripciones_CancelaAlVencer DEFAULT 0,
    -- R39: lo que se suma (o se descuenta, si es negativo) en la próxima renovación por un cambio de plan del seguro a
    -- mitad de mes. La Flota SaaS no lo usa: se paga por vehículo.
    AjustePendiente DECIMAL(12,2) NOT NULL CONSTRAINT DF_Suscripciones_AjustePendiente DEFAULT 0,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Suscripciones_DVH DEFAULT ''
);

-- Viajes: la tabla central (Oferta_Carga del análisis). Cada oferta publicada por un dador
-- es un viaje que pasa por estos estados:
--   Pendiente -> Asignado -> En camino a carga -> En origen -> En tránsito -> En destino -> Entregado
--   Cancelado: solo desde Pendiente (baja de la oferta).
-- IdDador NULL = viaje histórico de ejemplo (datos del tablero, no tiene dueño en el sistema).
-- TarifaAcordada = el precio que publica el dador, con todo incluido. LogTech cobra la comisión (Comision, 7 %)
-- y el remito digital (CargoRemito, $1.500); el transportista cobra
-- TarifaAcordada - Comision - DescuentoDador - CargoRemito. En los 3 primeros viajes de un dador nuevo la
-- comisión es 0 y DescuentoDador es el 7 %: el dador paga menos y el transportista cobra lo mismo.
-- Pago (CU-005-025): MedioPago Efectivo o Tarjeta. EstadoPago: Pendiente -> Retenido (tarjeta, al aceptar) ->
-- Liberado (con el remito) o En disputa -> Devuelto o Liberado. Efectivo: con el remito pasa a A cobrar (el dador tiene
-- 30 días para pagar en un punto de pago); si el transportista lo adelanta con factoring, Adelantado; al pagar, Liberado.
CREATE TABLE Viajes (
    IdViaje INT IDENTITY(1,1) PRIMARY KEY,
    FechaPedido DATETIME2(0) NOT NULL CONSTRAINT DF_Viajes_FechaPedido DEFAULT SYSDATETIME(),
    IdDador INT NULL REFERENCES Usuarios(IdUsuario),
    Origen NVARCHAR(60) NOT NULL,
    Destino NVARCHAR(60) NOT NULL,
    Mercaderia NVARCHAR(120) NOT NULL,
    PesoToneladas DECIMAL(6,2) NOT NULL
        CONSTRAINT CK_Viajes_Peso CHECK (PesoToneladas > 0 AND PesoToneladas <= 60),
    FechaCarga DATE NOT NULL,
    TarifaAcordada DECIMAL(12,2) NOT NULL CONSTRAINT CK_Viajes_Tarifa CHECK (TarifaAcordada > 0),
    Comision DECIMAL(12,2) NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Viajes_Estado DEFAULT N'Pendiente'
        CONSTRAINT CK_Viajes_Estado CHECK (Estado IN (N'Pendiente', N'Asignado', N'En camino a carga', N'En origen',
            N'En tránsito', N'En destino', N'Entregado', N'Cancelado')),
    IdTransportista INT NULL REFERENCES Usuarios(IdUsuario),
    IdVehiculo INT NULL REFERENCES Vehiculos(IdVehiculo),
    IdConductor INT NULL REFERENCES Conductores(IdConductor),
    MedioPago NVARCHAR(20) NOT NULL CONSTRAINT DF_Viajes_MedioPago DEFAULT N'Efectivo'
        CONSTRAINT CK_Viajes_MedioPago CHECK (MedioPago IN (N'Efectivo', N'Tarjeta', N'Saldo')),
    IdTarjeta INT NULL REFERENCES Tarjetas(IdTarjeta),
    DescuentoDador DECIMAL(12,2) NOT NULL CONSTRAINT DF_Viajes_DescuentoDador DEFAULT 0,
    CargoRemito DECIMAL(12,2) NOT NULL CONSTRAINT DF_Viajes_CargoRemito DEFAULT 0,
    EstadoPago NVARCHAR(20) NOT NULL CONSTRAINT DF_Viajes_EstadoPago DEFAULT N'Pendiente'
        CONSTRAINT CK_Viajes_EstadoPago CHECK (EstadoPago IN (N'Pendiente', N'Retenido', N'En disputa', N'Liberado',
            N'Devuelto', N'A cobrar', N'Adelantado')),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Viajes_DVH DEFAULT ''
);

-- Línea de tiempo de cada viaje (CU-007-036). La escribe el observador del cambio de estado.
CREATE TABLE HistorialViaje (
    IdHistorial BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdViaje INT NOT NULL REFERENCES Viajes(IdViaje),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_HistorialViaje_Fecha DEFAULT SYSDATETIME(),
    Estado NVARCHAR(20) NOT NULL,
    Detalle NVARCHAR(250) NOT NULL,
    Latitud DECIMAL(9,6) NULL,
    Longitud DECIMAL(9,6) NULL,
    Usuario NVARCHAR(50) NOT NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_HistorialViaje_DVH DEFAULT ''
);

-- Problemas reportados durante el viaje (CU-004-024).
CREATE TABLE Incidencias (
    IdIncidencia INT IDENTITY(1,1) PRIMARY KEY,
    IdViaje INT NOT NULL REFERENCES Viajes(IdViaje),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Incidencias_Fecha DEFAULT SYSDATETIME(),
    Tipo NVARCHAR(30) NOT NULL
        CONSTRAINT CK_Incidencias_Tipo CHECK (Tipo IN (N'Rotura', N'Demora', N'Accidente', N'Clima', N'Corte de ruta', N'Otro')),
    Descripcion NVARCHAR(500) NOT NULL,
    Foto NVARCHAR(260) NULL,
    Usuario NVARCHAR(50) NOT NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Incidencias_DVH DEFAULT ''
);

-- Comprobante de entrega (CU-004-022). Es inmutable: no se modifica ni se borra.
-- Token = SHA-256 de su contenido; DVH = dígito verificador como el resto de las tablas protegidas.
CREATE TABLE Remitos (
    IdRemito INT IDENTITY(1,1) PRIMARY KEY,
    IdViaje INT NOT NULL UNIQUE REFERENCES Viajes(IdViaje),
    Receptor NVARCHAR(100) NOT NULL,
    DniReceptor NVARCHAR(100) NOT NULL,
    Foto NVARCHAR(260) NOT NULL,
    FechaEmision DATETIME2(0) NOT NULL CONSTRAINT DF_Remitos_FechaEmision DEFAULT SYSDATETIME(),
    Token CHAR(64) NOT NULL UNIQUE,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Remitos_DVH DEFAULT ''
);

-- Calificación del dador al transportista (CU-007-037). R19: una sola por viaje.
CREATE TABLE Calificaciones (
    IdCalificacion INT IDENTITY(1,1) PRIMARY KEY,
    IdViaje INT NOT NULL UNIQUE REFERENCES Viajes(IdViaje),
    IdDador INT NOT NULL REFERENCES Usuarios(IdUsuario),
    IdTransportista INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Puntaje TINYINT NOT NULL CONSTRAINT CK_Calificaciones_Puntaje CHECK (Puntaje BETWEEN 1 AND 5),
    Resena NVARCHAR(500) NULL,
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Calificaciones_Fecha DEFAULT SYSDATETIME(),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Calificaciones_DVH DEFAULT ''
);

-- Documentación legal de cada usuario (CU-003-016 a 021): la suya, la de cada vehículo (IdVehiculo: seguro, VTV y
-- cédula verde) y la de cada conductor de la flota (IdConductor: DNI, licencia profesional y antecedentes penales).
-- Archivo = ruta dentro de App_Data/Archivos. AvisoEnviado: la rutina de vencimientos ya avisó que vence pronto.
CREATE TABLE Documentos (
    IdDocumento INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    IdVehiculo INT NULL REFERENCES Vehiculos(IdVehiculo),
    Tipo NVARCHAR(40) NOT NULL
        CONSTRAINT CK_Documentos_Tipo CHECK (Tipo IN (N'Licencia de conducir', N'Seguro del vehículo', N'VTV', N'Cédula verde',
            N'Habilitación CNRT', N'Constancia de CUIT', N'DNI del conductor', N'Licencia profesional', N'Antecedentes penales')),
    Archivo NVARCHAR(260) NOT NULL,
    FechaVencimiento DATE NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT DF_Documentos_Estado DEFAULT 'Pendiente'
        CONSTRAINT CK_Documentos_Estado CHECK (Estado IN ('Pendiente', 'Aprobado', 'Rechazado', 'Vencido', 'Inactivo')),
    Motivo NVARCHAR(250) NULL,
    FechaCarga DATETIME2(0) NOT NULL CONSTRAINT DF_Documentos_FechaCarga DEFAULT SYSDATETIME(),
    AvisoEnviado BIT NOT NULL CONSTRAINT DF_Documentos_AvisoEnviado DEFAULT 0,
    IdConductor INT NULL REFERENCES Conductores(IdConductor),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Documentos_DVH DEFAULT ''
);

-- Avisos que ve cada usuario en su panel: cambios de estado de sus viajes, documentos
-- aprobados, rechazados o por vencer. Leida = el usuario ya los marcó como vistos.
CREATE TABLE Notificaciones (
    IdNotificacion BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Notificaciones_Fecha DEFAULT SYSDATETIME(),
    Texto NVARCHAR(250) NOT NULL,
    Leida BIT NOT NULL CONSTRAINT DF_Notificaciones_Leida DEFAULT 0,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Notificaciones_DVH DEFAULT ''
);

-- Movimientos de plata (CU-005-025 y CU-005-026). Monto con signo: positivo entra, negativo sale.
-- El saldo de la billetera es la suma de los movimientos con AfectaSaldo = 1. Los pagos con tarjeta del
-- dador y sus devoluciones quedan registrados con AfectaSaldo = 0: salen de su tarjeta, no de la billetera.
-- Tabla protegida con dígito verificador.
CREATE TABLE Transacciones (
    IdTransaccion INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Transacciones_Fecha DEFAULT SYSDATETIME(),
    Tipo NVARCHAR(40) NOT NULL
        CONSTRAINT CK_Transacciones_Tipo CHECK (Tipo IN (N'Carga de saldo', N'Retiro', N'Cobro de viaje',
            N'Pago retenido', N'Devolución', N'Pago en punto de pago', N'Adelanto de factoring', N'Pago de seguro',
            N'Pago de suscripción', N'Pago con saldo')),
    Monto DECIMAL(12,2) NOT NULL,
    AfectaSaldo BIT NOT NULL,
    IdViaje INT NULL REFERENCES Viajes(IdViaje),
    IdTarjeta INT NULL REFERENCES Tarjetas(IdTarjeta),
    Detalle NVARCHAR(200) NOT NULL,
    Comprobante NVARCHAR(40) NULL,     -- código de la pasarela: autorización del cobro o de la transferencia
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Transacciones_DVH DEFAULT ''
);
CREATE INDEX IX_Transacciones_Usuario ON Transacciones (IdUsuario, Fecha);

-- Reclamos del dador sobre un viaje pagado con tarjeta, con la plata retenida (CU-005-027, R13).
-- Uno por viaje. Lo resuelve el administrador: devolver al dador o pagar al transportista.
-- Pedidos de adelanto del cobro de un viaje en efectivo que el dador todavía no pagó (CU-006-029 y 030, R14 y R15).
-- El transportista cobra al momento el monto menos el 4 %; la entidad que adelanta (simulada) recupera la plata cuando
-- el dador paga. También se guardan los rechazados (puntaje bajo o fondo sin plata).
CREATE TABLE SolicitudesFactoring (
    IdSolicitud INT IDENTITY(1,1) PRIMARY KEY,
    IdViaje INT NOT NULL REFERENCES Viajes(IdViaje),
    IdTransportista INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_SolicitudesFactoring_Fecha DEFAULT SYSDATETIME(),
    MontoViaje DECIMAL(12,2) NOT NULL,       -- lo que cobraría el transportista
    Comision DECIMAL(12,2) NOT NULL,         -- 4 % para LogTech
    MontoAdelantado DECIMAL(12,2) NOT NULL,  -- lo que recibe al momento
    Puntaje INT NOT NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT CK_SolicitudesFactoring_Estado CHECK (Estado IN (N'Aprobada', N'Rechazada')),
    Motivo NVARCHAR(200) NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_SolicitudesFactoring_DVH DEFAULT ''
);
CREATE UNIQUE INDEX UX_SolicitudesFactoring_ViajeAprobado ON SolicitudesFactoring (IdViaje) WHERE Estado = N'Aprobada';

CREATE TABLE Disputas (
    IdDisputa INT IDENTITY(1,1) PRIMARY KEY,
    IdViaje INT NOT NULL UNIQUE REFERENCES Viajes(IdViaje),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Disputas_Fecha DEFAULT SYSDATETIME(),
    Motivo NVARCHAR(500) NOT NULL,
    Estado NVARCHAR(30) NOT NULL CONSTRAINT DF_Disputas_Estado DEFAULT N'Abierta'
        CONSTRAINT CK_Disputas_Estado CHECK (Estado IN (N'Abierta', N'Devuelta al dador', N'Pagada al transportista')),
    Resolucion NVARCHAR(500) NULL,
    FechaResolucion DATETIME2(0) NULL,
    ResueltaPor NVARCHAR(50) NULL,
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Disputas_DVH DEFAULT ''
);

-- Facturas electrónicas de LogTech (CU-008-039). Cada cobro de LogTech genera una: la comisión y el remito de un viaje
-- (al dador, cuando se entrega), la suscripción Premium del transportista y la Flota SaaS (cada mes). Los precios ya
-- incluyen el IVA (21 %). ARCA (simulado) autoriza cada factura con un CAE; si no responde, queda «Pendiente de CAE» y
-- la rutina diaria la vuelve a pedir (ERR022). El número se asigna cuando ARCA la autoriza. Los datos del cliente se
-- copian al emitir: la factura no cambia si después cambia su perfil. El CUIT va cifrado, como en Usuarios.
CREATE TABLE Facturas (
    IdFactura INT IDENTITY(1,1) PRIMARY KEY,
    IdUsuario INT NOT NULL REFERENCES Usuarios(IdUsuario),
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Facturas_Fecha DEFAULT SYSDATETIME(),
    Tipo CHAR(1) NOT NULL CONSTRAINT CK_Facturas_Tipo CHECK (Tipo IN ('A', 'B')),
    PuntoVenta INT NOT NULL,
    Numero INT NULL,
    Servicio NVARCHAR(30) NOT NULL
        CONSTRAINT CK_Facturas_Servicio CHECK (Servicio IN (N'Comisión por viaje', N'Flota SaaS', N'Premium transportista')),
    Detalle NVARCHAR(250) NOT NULL,
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(12,2) NOT NULL,
    Neto DECIMAL(12,2) NOT NULL,
    Iva DECIMAL(12,2) NOT NULL,
    Total DECIMAL(12,2) NOT NULL,
    Cliente NVARCHAR(120) NOT NULL,
    CuitCliente NVARCHAR(100) NOT NULL,
    CondicionIva NVARCHAR(30) NOT NULL,
    IdViaje INT NULL REFERENCES Viajes(IdViaje),
    IdSuscripcion INT NULL REFERENCES Suscripciones(IdSuscripcion),
    Cae CHAR(14) NULL,
    VencimientoCae DATE NULL,
    Estado NVARCHAR(20) NOT NULL CONSTRAINT CK_Facturas_Estado CHECK (Estado IN (N'Emitida', N'Pendiente de CAE')),
    DVH CHAR(64) NOT NULL CONSTRAINT DF_Facturas_DVH DEFAULT ''
);
CREATE UNIQUE INDEX UX_Facturas_Numero ON Facturas (Tipo, PuntoVenta, Numero) WHERE Numero IS NOT NULL;
-- Una sola factura por viaje.
CREATE UNIQUE INDEX UX_Facturas_Viaje ON Facturas (IdViaje) WHERE IdViaje IS NOT NULL;
CREATE INDEX IX_Facturas_Usuario ON Facturas (IdUsuario, Fecha);

CREATE INDEX IX_Suscripciones_FechaAlta ON Suscripciones (FechaAlta);
-- Un solo seguro activo por usuario (R16).
CREATE UNIQUE INDEX UX_Suscripciones_SeguroActivo ON Suscripciones (IdUsuario) WHERE IdPlanSeguro IS NOT NULL AND FechaBaja IS NULL;
-- Una sola suscripción Flota SaaS o Premium vigente por cuenta (CU-008-040).
CREATE UNIQUE INDEX UX_Suscripciones_PlanActivo ON Suscripciones (IdUsuario, Tipo) WHERE IdUsuario IS NOT NULL AND IdPlanSeguro IS NULL AND FechaBaja IS NULL;
CREATE INDEX IX_Viajes_FechaPedido ON Viajes (FechaPedido);
CREATE INDEX IX_Viajes_Estado ON Viajes (Estado);
CREATE INDEX IX_HistorialViaje_Viaje ON HistorialViaje (IdViaje, Fecha);
CREATE INDEX IX_Notificaciones_Usuario ON Notificaciones (IdUsuario, Leida);
GO

-- Un solo lugar para la fórmula del DVH de cada tabla (todas menos DVV): una firma SHA-256 de todas las
-- columnas de la fila. Fila identifica el registro (su clave) y Guardado es el DVH que tiene guardado.
-- Generada con las columnas reales de cada tabla: al agregar una tabla o una columna, sumarla acá.
CREATE VIEW dbo.DVH_Calculado AS
    SELECT N'Transacciones' AS Tabla, CONCAT(N'IdTransaccion ', IdTransaccion) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdTransaccion, N'|', IdUsuario, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', Tipo, N'|', Monto, N'|',
            AfectaSaldo, N'|', IdViaje, N'|', IdTarjeta, N'|', Detalle, N'|', Comprobante)), 2) AS DVH,
        DVH AS Guardado
    FROM Transacciones
    UNION ALL
    SELECT N'Disputas' AS Tabla, CONCAT(N'IdDisputa ', IdDisputa) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdDisputa, N'|', IdViaje, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', Motivo, N'|', Estado, N'|',
            Resolucion, N'|', CONVERT(NVARCHAR(40), FechaResolucion, 126), N'|', ResueltaPor)), 2) AS DVH,
        DVH AS Guardado
    FROM Disputas
    UNION ALL
    SELECT N'SolicitudesFactoring' AS Tabla, CONCAT(N'IdSolicitud ', IdSolicitud) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdSolicitud, N'|', IdViaje, N'|', IdTransportista, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|',
            MontoViaje, N'|', Comision, N'|', MontoAdelantado, N'|', Puntaje, N'|', Estado, N'|', Motivo)), 2) AS DVH,
        DVH AS Guardado
    FROM SolicitudesFactoring
    UNION ALL
    SELECT N'PlanesSeguro' AS Tabla, CONCAT(N'IdPlan ', IdPlan) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdPlan, N'|', Aseguradora, N'|', Nombre, N'|', CoberturaPorViaje, N'|', ViajesPorMes, N'|', Precio, N'|',
            Detalle)), 2) AS DVH,
        DVH AS Guardado
    FROM PlanesSeguro
    UNION ALL
    SELECT N'Ciudades' AS Tabla, CONCAT(N'IdCiudad ', IdCiudad) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdCiudad, N'|', Nombre, N'|', Provincia, N'|', Latitud, N'|', Longitud)), 2) AS DVH,
        DVH AS Guardado
    FROM Ciudades
    UNION ALL
    SELECT N'CuentasDeposito' AS Tabla, CONCAT(N'IdUsuario ', IdUsuario) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdUsuario, N'|', CbuOAlias, N'|', CONVERT(NVARCHAR(40), FechaAlta, 126))), 2) AS DVH,
        DVH AS Guardado
    FROM CuentasDeposito
    UNION ALL
    SELECT N'Vehiculos' AS Tabla, CONCAT(N'IdVehiculo ', IdVehiculo) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdVehiculo, N'|', IdPropietario, N'|', Patente, N'|', Marca, N'|', Modelo, N'|', CapacidadToneladas, N'|',
            TipoAcoplado, N'|', Estado, N'|', CONVERT(NVARCHAR(40), FechaAlta, 126), N'|', IdConductorDesignado)), 2) AS DVH,
        DVH AS Guardado
    FROM Vehiculos
    UNION ALL
    SELECT N'Conductores' AS Tabla, CONCAT(N'IdConductor ', IdConductor) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdConductor, N'|', IdFlota, N'|', Nombre, N'|', Apellido, N'|', Dni, N'|', NumeroLicencia, N'|', Telefono,
            N'|', Estado, N'|', CONVERT(NVARCHAR(40), FechaAlta, 126))), 2) AS DVH,
        DVH AS Guardado
    FROM Conductores
    UNION ALL
    SELECT N'Suscripciones' AS Tabla, CONCAT(N'IdSuscripcion ', IdSuscripcion) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdSuscripcion, N'|', Suscriptor, N'|', Tipo, N'|', CantidadPatentes, N'|', Monto, N'|',
            CONVERT(NVARCHAR(40), FechaAlta, 126), N'|', CONVERT(NVARCHAR(40), FechaBaja, 126), N'|', IdUsuario, N'|',
            IdPlanSeguro, N'|', IdTarjeta, N'|', NumeroPoliza, N'|', CONVERT(NVARCHAR(40), Vence, 126), N'|',
            CancelaAlVencer, N'|', AjustePendiente)), 2) AS DVH,
        DVH AS Guardado
    FROM Suscripciones
    UNION ALL
    SELECT N'Viajes' AS Tabla, CONCAT(N'IdViaje ', IdViaje) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdViaje, N'|', CONVERT(NVARCHAR(40), FechaPedido, 126), N'|', IdDador, N'|', Origen, N'|', Destino, N'|',
            Mercaderia, N'|', PesoToneladas, N'|', CONVERT(NVARCHAR(40), FechaCarga, 126), N'|', TarifaAcordada, N'|',
            Comision, N'|', Estado, N'|', IdTransportista, N'|', IdVehiculo, N'|', IdConductor, N'|', MedioPago, N'|',
            IdTarjeta, N'|', DescuentoDador, N'|', CargoRemito, N'|', EstadoPago)), 2) AS DVH,
        DVH AS Guardado
    FROM Viajes
    UNION ALL
    SELECT N'HistorialViaje' AS Tabla, CONCAT(N'IdHistorial ', IdHistorial) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdHistorial, N'|', IdViaje, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', Estado, N'|', Detalle, N'|',
            Latitud, N'|', Longitud, N'|', Usuario)), 2) AS DVH,
        DVH AS Guardado
    FROM HistorialViaje
    UNION ALL
    SELECT N'Roles' AS Tabla, CONCAT(N'IdRol ', IdRol) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdRol, N'|', Nombre, N'|', EsFamilia)), 2) AS DVH,
        DVH AS Guardado
    FROM Roles
    UNION ALL
    SELECT N'Incidencias' AS Tabla, CONCAT(N'IdIncidencia ', IdIncidencia) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdIncidencia, N'|', IdViaje, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', Tipo, N'|', Descripcion, N'|',
            Foto, N'|', Usuario)), 2) AS DVH,
        DVH AS Guardado
    FROM Incidencias
    UNION ALL
    SELECT N'Permisos' AS Tabla, CONCAT(N'IdPermiso ', IdPermiso) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdPermiso, N'|', Nombre)), 2) AS DVH,
        DVH AS Guardado
    FROM Permisos
    UNION ALL
    SELECT N'Remitos' AS Tabla, CONCAT(N'IdRemito ', IdRemito) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdRemito, N'|', IdViaje, N'|', Receptor, N'|', DniReceptor, N'|', Foto, N'|',
            CONVERT(NVARCHAR(40), FechaEmision, 126), N'|', Token)), 2) AS DVH,
        DVH AS Guardado
    FROM Remitos
    UNION ALL
    SELECT N'Usuarios' AS Tabla, CONCAT(N'IdUsuario ', IdUsuario) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdUsuario, N'|', NombreUsuario, N'|', CorreoElectronico, N'|', ClaveHash, N'|', ClaveSalt, N'|', Nombre,
            N'|', Apellido, N'|', Telefono, N'|', CuitCuil, N'|', RazonSocial, N'|', NumeroLicencia, N'|', Idioma,
            N'|', IntentosFallidos, N'|', CONVERT(NVARCHAR(40), BloqueadoHasta, 126), N'|', Estado, N'|',
            CONVERT(NVARCHAR(40), FechaAlta, 126), N'|', CondicionIva)), 2) AS DVH,
        DVH AS Guardado
    FROM Usuarios
    UNION ALL
    SELECT N'Calificaciones' AS Tabla, CONCAT(N'IdCalificacion ', IdCalificacion) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdCalificacion, N'|', IdViaje, N'|', IdDador, N'|', IdTransportista, N'|', Puntaje, N'|', Resena, N'|',
            CONVERT(NVARCHAR(40), Fecha, 126))), 2) AS DVH,
        DVH AS Guardado
    FROM Calificaciones
    UNION ALL
    SELECT N'UsuarioRol' AS Tabla, CONCAT(N'IdUsuario ', IdUsuario, N', IdRol ', IdRol) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdUsuario, N'|', IdRol)), 2) AS DVH,
        DVH AS Guardado
    FROM UsuarioRol
    UNION ALL
    SELECT N'Documentos' AS Tabla, CONCAT(N'IdDocumento ', IdDocumento) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdDocumento, N'|', IdUsuario, N'|', IdVehiculo, N'|', Tipo, N'|', Archivo, N'|',
            CONVERT(NVARCHAR(40), FechaVencimiento, 126), N'|', Estado, N'|', Motivo, N'|',
            CONVERT(NVARCHAR(40), FechaCarga, 126), N'|', AvisoEnviado, N'|', IdConductor)), 2) AS DVH,
        DVH AS Guardado
    FROM Documentos
    UNION ALL
    SELECT N'RolPermiso' AS Tabla, CONCAT(N'IdRol ', IdRol, N', IdPermiso ', IdPermiso) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdRol, N'|', IdPermiso)), 2) AS DVH,
        DVH AS Guardado
    FROM RolPermiso
    UNION ALL
    SELECT N'UsuarioPermiso' AS Tabla, CONCAT(N'IdUsuario ', IdUsuario, N', IdPermiso ', IdPermiso) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdUsuario, N'|', IdPermiso)), 2) AS DVH,
        DVH AS Guardado
    FROM UsuarioPermiso
    UNION ALL
    SELECT N'UsuarioFamilia' AS Tabla, CONCAT(N'IdUsuario ', IdUsuario, N', IdFamilia ', IdFamilia) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdUsuario, N'|', IdFamilia)), 2) AS DVH,
        DVH AS Guardado
    FROM UsuarioFamilia
    UNION ALL
    SELECT N'RolFamilia' AS Tabla, CONCAT(N'IdPadre ', IdPadre, N', IdFamilia ', IdFamilia) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdPadre, N'|', IdFamilia)), 2) AS DVH,
        DVH AS Guardado
    FROM RolFamilia
    UNION ALL
    SELECT N'RecuperacionesClave' AS Tabla, CONCAT(N'IdRecuperacion ', IdRecuperacion) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdRecuperacion, N'|', IdUsuario, N'|', Token, N'|', CONVERT(NVARCHAR(40), FechaExpira, 126), N'|', Usado)), 2) AS DVH,
        DVH AS Guardado
    FROM RecuperacionesClave
    UNION ALL
    SELECT N'Notificaciones' AS Tabla, CONCAT(N'IdNotificacion ', IdNotificacion) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdNotificacion, N'|', IdUsuario, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', Texto, N'|', Leida)), 2) AS DVH,
        DVH AS Guardado
    FROM Notificaciones
    UNION ALL
    SELECT N'BitacoraEventos' AS Tabla, CONCAT(N'IdEvento ', IdEvento) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdEvento, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', IdUsuario, N'|', Usuario, N'|', Descripcion, N'|',
            Criticidad)), 2) AS DVH,
        DVH AS Guardado
    FROM BitacoraEventos
    UNION ALL
    SELECT N'ClavesApi' AS Tabla, CONCAT(N'IdClave ', IdClave) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdClave, N'|', IdUsuario, N'|', Hash, N'|', Ultimos4, N'|', CONVERT(NVARCHAR(40), FechaAlta, 126), N'|',
            CONVERT(NVARCHAR(40), FechaUltimoUso, 126), N'|', Activa)), 2) AS DVH,
        DVH AS Guardado
    FROM ClavesApi
    UNION ALL
    SELECT N'Tarjetas' AS Tabla, CONCAT(N'IdTarjeta ', IdTarjeta) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdTarjeta, N'|', IdUsuario, N'|', Titular, N'|', Marca, N'|', Ultimos4, N'|', Vencimiento, N'|', Token,
            N'|', Activa, N'|', CONVERT(NVARCHAR(40), FechaAlta, 126))), 2) AS DVH,
        DVH AS Guardado
    FROM Tarjetas
    UNION ALL
    SELECT N'Facturas' AS Tabla, CONCAT(N'IdFactura ', IdFactura) AS Fila,
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(
            IdFactura, N'|', IdUsuario, N'|', CONVERT(NVARCHAR(40), Fecha, 126), N'|', Tipo, N'|', PuntoVenta, N'|',
            Numero, N'|', Servicio, N'|', Detalle, N'|', Cantidad, N'|', PrecioUnitario, N'|', Neto, N'|', Iva, N'|',
            Total, N'|', Cliente, N'|', CuitCliente, N'|', CondicionIva, N'|', IdViaje, N'|', IdSuscripcion, N'|',
            Cae, N'|', CONVERT(NVARCHAR(40), VencimientoCae, 126), N'|', Estado)), 2) AS DVH,
        DVH AS Guardado
    FROM Facturas;
GO

-- DVV de cada tabla calculado con los DVH guardados (en orden de Fila). Una tabla vacía también tiene su DVV.
CREATE VIEW dbo.DVV_Calculado AS
    SELECT t.Tabla, CONVERT(CHAR(64), HASHBYTES('SHA2_256', ISNULL(a.Datos, N'')), 2) AS Digito
    FROM (VALUES
        (N'Transacciones'),
        (N'Disputas'),
        (N'SolicitudesFactoring'),
        (N'PlanesSeguro'),
        (N'Ciudades'),
        (N'CuentasDeposito'),
        (N'Vehiculos'),
        (N'Conductores'),
        (N'Suscripciones'),
        (N'Viajes'),
        (N'HistorialViaje'),
        (N'Roles'),
        (N'Incidencias'),
        (N'Permisos'),
        (N'Remitos'),
        (N'Usuarios'),
        (N'Calificaciones'),
        (N'UsuarioRol'),
        (N'Documentos'),
        (N'RolPermiso'),
        (N'UsuarioFamilia'),
        (N'UsuarioPermiso'),
        (N'RolFamilia'),
        (N'RecuperacionesClave'),
        (N'Notificaciones'),
        (N'BitacoraEventos'),
        (N'ClavesApi'),
        (N'Tarjetas'),
        (N'Facturas')) t (Tabla)
    LEFT JOIN (SELECT Tabla, STRING_AGG(CONVERT(NVARCHAR(MAX), Guardado), N'') WITHIN GROUP (ORDER BY Fila) AS Datos
               FROM dbo.DVH_Calculado GROUP BY Tabla) a ON a.Tabla = t.Tabla;
GO

-- 1 si la tabla está sana (cada DVH coincide con sus datos y el DVV con los DVH). La aplicación la consulta antes de
-- cambiar una tabla: si alguien la alteró por afuera, no se guarda nada (recalcular taparía el daño).
CREATE FUNCTION dbo.TablaIntegra (@Tabla SYSNAME) RETURNS BIT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.DVH_Calculado WHERE Tabla = @Tabla AND DVH <> Guardado) RETURN 0;
    IF EXISTS (SELECT 1 FROM dbo.DVV_Calculado v LEFT JOIN DVV d ON d.Tabla = v.Tabla
               WHERE v.Tabla = @Tabla AND (d.Digito IS NULL OR d.Digito <> v.Digito)) RETURN 0;
    RETURN 1;
END;
GO

-- Fija el DVH de cada fila y el DVV de la tabla. La aplicación pasa @Tabla con la tabla que tocó, después de
-- comprobar que estaba sana (dbo.TablaIntegra). @Tabla NULL recalcula todo: lo usa el administrador (Recuperación de
-- DV, CU-010-066), la restauración de un backup y el script inicial.
CREATE PROCEDURE dbo.RecalcularDigitos @Tabla SYSNAME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Tabla IS NULL OR @Tabla = N'Transacciones'
        UPDATE x SET x.DVH = c.DVH FROM Transacciones x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Transacciones' AND c.Fila = CONCAT(N'IdTransaccion ', x.IdTransaccion);
    IF @Tabla IS NULL OR @Tabla = N'Disputas'
        UPDATE x SET x.DVH = c.DVH FROM Disputas x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Disputas' AND c.Fila = CONCAT(N'IdDisputa ', x.IdDisputa);
    IF @Tabla IS NULL OR @Tabla = N'SolicitudesFactoring'
        UPDATE x SET x.DVH = c.DVH FROM SolicitudesFactoring x JOIN dbo.DVH_Calculado c ON c.Tabla = N'SolicitudesFactoring' AND c.Fila = CONCAT(N'IdSolicitud ', x.IdSolicitud);
    IF @Tabla IS NULL OR @Tabla = N'PlanesSeguro'
        UPDATE x SET x.DVH = c.DVH FROM PlanesSeguro x JOIN dbo.DVH_Calculado c ON c.Tabla = N'PlanesSeguro' AND c.Fila = CONCAT(N'IdPlan ', x.IdPlan);
    IF @Tabla IS NULL OR @Tabla = N'Ciudades'
        UPDATE x SET x.DVH = c.DVH FROM Ciudades x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Ciudades' AND c.Fila = CONCAT(N'IdCiudad ', x.IdCiudad);
    IF @Tabla IS NULL OR @Tabla = N'CuentasDeposito'
        UPDATE x SET x.DVH = c.DVH FROM CuentasDeposito x JOIN dbo.DVH_Calculado c ON c.Tabla = N'CuentasDeposito' AND c.Fila = CONCAT(N'IdUsuario ', x.IdUsuario);
    IF @Tabla IS NULL OR @Tabla = N'Vehiculos'
        UPDATE x SET x.DVH = c.DVH FROM Vehiculos x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Vehiculos' AND c.Fila = CONCAT(N'IdVehiculo ', x.IdVehiculo);
    IF @Tabla IS NULL OR @Tabla = N'Conductores'
        UPDATE x SET x.DVH = c.DVH FROM Conductores x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Conductores' AND c.Fila = CONCAT(N'IdConductor ', x.IdConductor);
    IF @Tabla IS NULL OR @Tabla = N'Suscripciones'
        UPDATE x SET x.DVH = c.DVH FROM Suscripciones x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Suscripciones' AND c.Fila = CONCAT(N'IdSuscripcion ', x.IdSuscripcion);
    IF @Tabla IS NULL OR @Tabla = N'Viajes'
        UPDATE x SET x.DVH = c.DVH FROM Viajes x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Viajes' AND c.Fila = CONCAT(N'IdViaje ', x.IdViaje);
    IF @Tabla IS NULL OR @Tabla = N'HistorialViaje'
        UPDATE x SET x.DVH = c.DVH FROM HistorialViaje x JOIN dbo.DVH_Calculado c ON c.Tabla = N'HistorialViaje' AND c.Fila = CONCAT(N'IdHistorial ', x.IdHistorial);
    IF @Tabla IS NULL OR @Tabla = N'Roles'
        UPDATE x SET x.DVH = c.DVH FROM Roles x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Roles' AND c.Fila = CONCAT(N'IdRol ', x.IdRol);
    IF @Tabla IS NULL OR @Tabla = N'Incidencias'
        UPDATE x SET x.DVH = c.DVH FROM Incidencias x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Incidencias' AND c.Fila = CONCAT(N'IdIncidencia ', x.IdIncidencia);
    IF @Tabla IS NULL OR @Tabla = N'Permisos'
        UPDATE x SET x.DVH = c.DVH FROM Permisos x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Permisos' AND c.Fila = CONCAT(N'IdPermiso ', x.IdPermiso);
    IF @Tabla IS NULL OR @Tabla = N'Remitos'
        UPDATE x SET x.DVH = c.DVH FROM Remitos x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Remitos' AND c.Fila = CONCAT(N'IdRemito ', x.IdRemito);
    IF @Tabla IS NULL OR @Tabla = N'Usuarios'
        UPDATE x SET x.DVH = c.DVH FROM Usuarios x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Usuarios' AND c.Fila = CONCAT(N'IdUsuario ', x.IdUsuario);
    IF @Tabla IS NULL OR @Tabla = N'Calificaciones'
        UPDATE x SET x.DVH = c.DVH FROM Calificaciones x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Calificaciones' AND c.Fila = CONCAT(N'IdCalificacion ', x.IdCalificacion);
    IF @Tabla IS NULL OR @Tabla = N'UsuarioRol'
        UPDATE x SET x.DVH = c.DVH FROM UsuarioRol x JOIN dbo.DVH_Calculado c ON c.Tabla = N'UsuarioRol' AND c.Fila = CONCAT(N'IdUsuario ', x.IdUsuario, N', IdRol ', x.IdRol);
    IF @Tabla IS NULL OR @Tabla = N'Documentos'
        UPDATE x SET x.DVH = c.DVH FROM Documentos x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Documentos' AND c.Fila = CONCAT(N'IdDocumento ', x.IdDocumento);
    IF @Tabla IS NULL OR @Tabla = N'RolPermiso'
        UPDATE x SET x.DVH = c.DVH FROM RolPermiso x JOIN dbo.DVH_Calculado c ON c.Tabla = N'RolPermiso' AND c.Fila = CONCAT(N'IdRol ', x.IdRol, N', IdPermiso ', x.IdPermiso);
    IF @Tabla IS NULL OR @Tabla = N'UsuarioPermiso'
        UPDATE x SET x.DVH = c.DVH FROM UsuarioPermiso x JOIN dbo.DVH_Calculado c ON c.Tabla = N'UsuarioPermiso' AND c.Fila = CONCAT(N'IdUsuario ', x.IdUsuario, N', IdPermiso ', x.IdPermiso);
    IF @Tabla IS NULL OR @Tabla = N'UsuarioFamilia'
        UPDATE x SET x.DVH = c.DVH FROM UsuarioFamilia x JOIN dbo.DVH_Calculado c ON c.Tabla = N'UsuarioFamilia' AND c.Fila = CONCAT(N'IdUsuario ', x.IdUsuario, N', IdFamilia ', x.IdFamilia);
    IF @Tabla IS NULL OR @Tabla = N'RolFamilia'
        UPDATE x SET x.DVH = c.DVH FROM RolFamilia x JOIN dbo.DVH_Calculado c ON c.Tabla = N'RolFamilia' AND c.Fila = CONCAT(N'IdPadre ', x.IdPadre, N', IdFamilia ', x.IdFamilia);
    IF @Tabla IS NULL OR @Tabla = N'RecuperacionesClave'
        UPDATE x SET x.DVH = c.DVH FROM RecuperacionesClave x JOIN dbo.DVH_Calculado c ON c.Tabla = N'RecuperacionesClave' AND c.Fila = CONCAT(N'IdRecuperacion ', x.IdRecuperacion);
    IF @Tabla IS NULL OR @Tabla = N'Notificaciones'
        UPDATE x SET x.DVH = c.DVH FROM Notificaciones x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Notificaciones' AND c.Fila = CONCAT(N'IdNotificacion ', x.IdNotificacion);
    IF @Tabla IS NULL OR @Tabla = N'BitacoraEventos'
        UPDATE x SET x.DVH = c.DVH FROM BitacoraEventos x JOIN dbo.DVH_Calculado c ON c.Tabla = N'BitacoraEventos' AND c.Fila = CONCAT(N'IdEvento ', x.IdEvento);
    IF @Tabla IS NULL OR @Tabla = N'ClavesApi'
        UPDATE x SET x.DVH = c.DVH FROM ClavesApi x JOIN dbo.DVH_Calculado c ON c.Tabla = N'ClavesApi' AND c.Fila = CONCAT(N'IdClave ', x.IdClave);
    IF @Tabla IS NULL OR @Tabla = N'Tarjetas'
        UPDATE x SET x.DVH = c.DVH FROM Tarjetas x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Tarjetas' AND c.Fila = CONCAT(N'IdTarjeta ', x.IdTarjeta);
    IF @Tabla IS NULL OR @Tabla = N'Facturas'
        UPDATE x SET x.DVH = c.DVH FROM Facturas x JOIN dbo.DVH_Calculado c ON c.Tabla = N'Facturas' AND c.Fila = CONCAT(N'IdFactura ', x.IdFactura);
    DELETE FROM DVV WHERE @Tabla IS NULL OR Tabla = @Tabla;
    INSERT INTO DVV (Tabla, Digito)
    SELECT Tabla, Digito FROM dbo.DVV_Calculado WHERE @Tabla IS NULL OR Tabla = @Tabla OPTION (RECOMPILE);
END;
GO

-- Analiza la integridad de todas las tablas y devuelve qué se rompió (tabla, registro y detalle):
--   - registros alterados: su DVH guardado no coincide con sus datos
--   - tablas con registros agregados o borrados: el DVV no coincide con los DVH
-- Resultado vacío = base íntegra.
CREATE PROCEDURE dbo.VerificarIntegridad
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Tabla, Fila, N'Registro alterado: sus datos no coinciden con su dígito verificador (DVH)' AS Detalle
    FROM dbo.DVH_Calculado
    WHERE DVH <> Guardado
    UNION ALL
    SELECT v.Tabla, NULL, N'Se agregaron o borraron registros: el dígito verificador de la tabla (DVV) no coincide'
    FROM dbo.DVV_Calculado v
    LEFT JOIN DVV d ON d.Tabla = v.Tabla
    WHERE d.Digito IS NULL OR d.Digito <> v.Digito
    ORDER BY 1, 2;
END;
GO

-- ====================================================================== PROCEDIMIENTOS ALMACENADOS
-- Las operaciones principales de la DAL están en procedimientos almacenados (usp_Clase_Metodo): EjecutorDAL los
-- llama por su nombre. El control de los dígitos verificadores lo sigue haciendo EjecutorDAL: antes de un cambio
-- comprueba las tablas que nombra el procedimiento y después las recalcula.

-- ---------------------------------------------------------------------- BitacoraDAL
-- BitacoraDAL.Registrar
CREATE PROCEDURE dbo.usp_Bitacora_Registrar
    @IdUsuario INT,
    @Usuario NVARCHAR(50),
    @Descripcion NVARCHAR(250),
    @Criticidad NVARCHAR(20)
AS
BEGIN
    DECLARE @Sana BIT = dbo.TablaIntegra(N'BitacoraEventos');
    INSERT INTO BitacoraEventos (IdUsuario, Usuario, Descripcion, Criticidad)
    VALUES (@IdUsuario, @Usuario, @Descripcion, @Criticidad);
    DECLARE @Id BIGINT = SCOPE_IDENTITY();
    UPDATE b SET b.DVH = c.DVH FROM BitacoraEventos b
    JOIN dbo.DVH_Calculado c ON c.Tabla = N'BitacoraEventos' AND c.Fila = CONCAT(N'IdEvento ', b.IdEvento)
    WHERE b.IdEvento = @Id;
    IF @Sana = 1
    BEGIN
        DELETE FROM DVV WHERE Tabla = N'BitacoraEventos';
        INSERT INTO DVV (Tabla, Digito) SELECT Tabla, Digito FROM dbo.DVV_Calculado WHERE Tabla = N'BitacoraEventos';
    END
END;
GO

-- ---------------------------------------------------------------------- FactoringDAL
-- FactoringDAL.Aprobar
CREATE PROCEDURE dbo.usp_Factoring_Aprobar
    @Viaje INT,
    @Transportista INT,
    @MontoViaje DECIMAL(18,2),
    @Comision DECIMAL(18,2),
    @Adelantado DECIMAL(18,2),
    @Puntaje INT,
    @Estado NVARCHAR(20),
    @Motivo NVARCHAR(200),
    @TUsuario INT,
    @TTipo NVARCHAR(40),
    @TMonto DECIMAL(18,2),
    @TAfecta BIT,
    @TViaje INT,
    @TTarjeta INT,
    @TDetalle NVARCHAR(200),
    @TComprobante NVARCHAR(40)
AS
BEGIN
    UPDATE Viajes SET EstadoPago = N'Adelantado' WHERE IdViaje = @Viaje AND EstadoPago = N'A cobrar';
    IF @@ROWCOUNT = 1
    BEGIN
        INSERT INTO SolicitudesFactoring (IdViaje, IdTransportista, MontoViaje, Comision, MontoAdelantado, Puntaje, Estado, Motivo)
    VALUES (@Viaje, @Transportista, @MontoViaje, @Comision, @Adelantado, @Puntaje, @Estado, @Motivo);
        INSERT INTO Transacciones (IdUsuario, Tipo, Monto, AfectaSaldo, IdViaje, IdTarjeta, Detalle, Comprobante)
    VALUES (@TUsuario, @TTipo, @TMonto, @TAfecta, @TViaje, @TTarjeta, @TDetalle, @TComprobante);
    END
END;
GO

-- ---------------------------------------------------------------------- FacturaDAL
-- FacturaDAL.Alta
CREATE PROCEDURE dbo.usp_Factura_Alta
    @IdUsuario INT,
    @Fecha DATETIME2(0),
    @Tipo CHAR(1),
    @PuntoVenta INT,
    @Servicio NVARCHAR(30),
    @Detalle NVARCHAR(250),
    @Cantidad INT,
    @PrecioUnitario DECIMAL(18,2),
    @Neto DECIMAL(18,2),
    @Iva DECIMAL(18,2),
    @Total DECIMAL(18,2),
    @Cliente NVARCHAR(120),
    @Cuit NVARCHAR(100),
    @CondicionIva NVARCHAR(30),
    @IdViaje INT,
    @IdSuscripcion INT
AS
BEGIN
    INSERT INTO Facturas (IdUsuario, Fecha, Tipo, PuntoVenta, Servicio, Detalle, Cantidad, PrecioUnitario,
        Neto, Iva, Total, Cliente, CuitCliente, CondicionIva, IdViaje, IdSuscripcion, Estado)
    VALUES (@IdUsuario, @Fecha, @Tipo, @PuntoVenta, @Servicio, @Detalle, @Cantidad, @PrecioUnitario,
        @Neto, @Iva, @Total, @Cliente, @Cuit, @CondicionIva, @IdViaje, @IdSuscripcion, N'Pendiente de CAE');
    SELECT CONVERT(INT, SCOPE_IDENTITY());
END;
GO

-- ---------------------------------------------------------------------- RemitoDAL
-- RemitoDAL.Alta
CREATE PROCEDURE dbo.usp_Remito_Alta
    @Viaje INT,
    @Receptor NVARCHAR(100),
    @Dni NVARCHAR(100),
    @Foto NVARCHAR(260),
    @Fecha DATETIME2(0),
    @Token CHAR(64)
AS
BEGIN
    INSERT INTO Remitos (IdViaje, Receptor, DniReceptor, Foto, FechaEmision, Token)
    SELECT @Viaje, @Receptor, @Dni, @Foto, @Fecha, @Token
    WHERE EXISTS (SELECT 1 FROM Viajes WHERE IdViaje = @Viaje AND Estado = N'En destino')
      AND NOT EXISTS (SELECT 1 FROM Remitos WHERE IdViaje = @Viaje);
    IF @@ROWCOUNT = 1
    BEGIN
        UPDATE Viajes SET Estado = N'Entregado' WHERE IdViaje = @Viaje;
        EXEC dbo.RecalcularDigitos 'Viajes';
    END
END;
GO

-- ---------------------------------------------------------------------- RolPermisoDAL
-- RolPermisoDAL.ObtenerTodos
CREATE PROCEDURE dbo.usp_RolPermiso_ObtenerTodos
AS
BEGIN
    SELECT r.IdRol, r.Nombre, p.Nombre, r.EsFamilia FROM Roles r
                    LEFT JOIN RolPermiso rp ON rp.IdRol = r.IdRol
                    LEFT JOIN Permisos p ON p.IdPermiso = rp.IdPermiso
                    ORDER BY r.Nombre, p.Nombre;
    -- segundo resultado: qué familias tiene cada rol (patrón Composite)
    SELECT IdPadre, IdFamilia FROM RolFamilia ORDER BY IdPadre, IdFamilia;
END;
GO

-- ---------------------------------------------------------------------- SeguroDAL
-- SeguroDAL.Contratar
CREATE PROCEDURE dbo.usp_Seguro_Contratar
    @Usuario INT,
    @Suscriptor NVARCHAR(120),
    @Monto DECIMAL(18,2),
    @Plan INT,
    @Tarjeta INT,
    @Poliza NVARCHAR(30),
    @Vence DATETIME2(0),
    @TUsuario INT,
    @TTipo NVARCHAR(40),
    @TMonto DECIMAL(18,2),
    @TAfecta BIT,
    @TViaje INT,
    @TTarjeta INT,
    @TDetalle NVARCHAR(200),
    @TComprobante NVARCHAR(40)
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Suscripciones WITH (UPDLOCK, HOLDLOCK)
        WHERE IdUsuario = @Usuario AND IdPlanSeguro IS NOT NULL AND FechaBaja IS NULL)
    BEGIN
        INSERT INTO Suscripciones (Suscriptor, Tipo, Monto, IdUsuario, IdPlanSeguro, IdTarjeta, NumeroPoliza, Vence)
        VALUES (@Suscriptor, N'Protección de carga', @Monto, @Usuario, @Plan, @Tarjeta, @Poliza, @Vence);
        INSERT INTO Transacciones (IdUsuario, Tipo, Monto, AfectaSaldo, IdViaje, IdTarjeta, Detalle, Comprobante)
    VALUES (@TUsuario, @TTipo, @TMonto, @TAfecta, @TViaje, @TTarjeta, @TDetalle, @TComprobante);
    END
END;
GO

-- ---------------------------------------------------------------------- SuscripcionDAL
-- SuscripcionDAL.Contratar
CREATE PROCEDURE dbo.usp_Suscripcion_Contratar
    @Usuario INT,
    @Tipo NVARCHAR(30),
    @Suscriptor NVARCHAR(120),
    @Patentes INT,
    @Monto DECIMAL(18,2),
    @Tarjeta INT,
    @Vence DATETIME2(0),
    @TUsuario INT,
    @TTipo NVARCHAR(40),
    @TMonto DECIMAL(18,2),
    @TAfecta BIT,
    @TViaje INT,
    @TTarjeta INT,
    @TDetalle NVARCHAR(200),
    @TComprobante NVARCHAR(40),
    @PasaAFlota INT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Suscripciones WITH (UPDLOCK, HOLDLOCK) WHERE IdUsuario = @Usuario AND Tipo = @Tipo
        AND IdPlanSeguro IS NULL AND FechaBaja IS NULL)
        SELECT 0;
    ELSE
    BEGIN
        INSERT INTO Suscripciones (Suscriptor, Tipo, CantidadPatentes, Monto, IdUsuario, IdTarjeta, Vence)
        VALUES (@Suscriptor, @Tipo, @Patentes, @Monto, @Usuario, @Tarjeta, @Vence);
        DECLARE @IdSuscripcion INT = SCOPE_IDENTITY();
        INSERT INTO Transacciones (IdUsuario, Tipo, Monto, AfectaSaldo, IdViaje, IdTarjeta, Detalle, Comprobante)
    VALUES (@TUsuario, @TTipo, @TMonto, @TAfecta, @TViaje, @TTarjeta, @TDetalle, @TComprobante);
        IF @PasaAFlota = 1
            UPDATE UsuarioRol SET IdRol = (SELECT IdRol FROM Roles WHERE Nombre = N'Administrador de Flota')
            WHERE IdUsuario = @Usuario;
        SELECT @IdSuscripcion;
    END
END;
GO

-- ---------------------------------------------------------------------- UsuarioDAL
-- UsuarioDAL.ActualizarIntentos
CREATE PROCEDURE dbo.usp_Usuario_ActualizarIntentos
    @Intentos INT,
    @BloqueadoHasta DATETIME2(0),
    @IdUsuario INT
AS
BEGIN
    UPDATE Usuarios SET IntentosFallidos = @Intentos,
    BloqueadoHasta = @BloqueadoHasta,
    Estado = CASE WHEN @BloqueadoHasta IS NULL THEN 'Activo' ELSE 'Bloqueado' END
    WHERE IdUsuario = @IdUsuario;
END;
GO

-- UsuarioDAL.ObtenerParaLogin
CREATE PROCEDURE dbo.usp_Usuario_ObtenerParaLogin
    @Identificador NVARCHAR(50),
    @Correo NVARCHAR(200)
AS
BEGIN
    SELECT TOP 1 u.IdUsuario, u.NombreUsuario, u.CorreoElectronico,
    u.ClaveHash, u.ClaveSalt, u.Idioma, u.IntentosFallidos, u.BloqueadoHasta,
    u.Estado, r.Nombre, u.Nombre, u.Apellido
    FROM Usuarios u
    INNER JOIN UsuarioRol ur ON ur.IdUsuario = u.IdUsuario
    INNER JOIN Roles r ON r.IdRol = ur.IdRol
    WHERE u.NombreUsuario = @Identificador OR u.CorreoElectronico = @Correo;
END;
GO

-- UsuarioDAL.RestablecerClave
CREATE PROCEDURE dbo.usp_Usuario_RestablecerClave
    @Hash CHAR(64),
    @Salt NVARCHAR(44),
    @IdUsuario INT
AS
BEGIN
    UPDATE Usuarios SET ClaveHash = @Hash, ClaveSalt = @Salt,
    IntentosFallidos = 0, BloqueadoHasta = NULL, Estado = 'Activo'
    WHERE IdUsuario = @IdUsuario;
END;
GO

-- ---------------------------------------------------------------------- ViajeDAL
-- ViajeDAL.Aceptar
CREATE PROCEDURE dbo.usp_Viaje_Aceptar
    @Transportista INT,
    @Vehiculo INT,
    @SinComision INT,
    @Retiene INT,
    @Id INT,
    @TUsuario INT,
    @TTipo NVARCHAR(40),
    @TMonto DECIMAL(18,2),
    @TAfecta BIT,
    @TViaje INT,
    @TTarjeta INT,
    @TDetalle NVARCHAR(200),
    @TComprobante NVARCHAR(40)
AS
BEGIN
    UPDATE Viajes SET Estado = N'Asignado', IdTransportista = @Transportista, IdVehiculo = @Vehiculo,
        Comision = CASE WHEN @SinComision = 1 THEN 0 ELSE Comision END,
        EstadoPago = CASE WHEN @Retiene = 1 THEN N'Retenido' ELSE EstadoPago END
    WHERE IdViaje = @Id AND Estado = N'Pendiente';
    IF @@ROWCOUNT = 1 AND @Retiene = 1 INSERT INTO Transacciones (IdUsuario, Tipo, Monto, AfectaSaldo, IdViaje, IdTarjeta, Detalle, Comprobante)
    VALUES (@TUsuario, @TTipo, @TMonto, @TAfecta, @TViaje, @TTarjeta, @TDetalle, @TComprobante);
END;
GO

-- ViajeDAL.CambiarEstado
CREATE PROCEDURE dbo.usp_Viaje_CambiarEstado
    @Nuevo NVARCHAR(20),
    @Id INT,
    @Actual NVARCHAR(20)
AS
BEGIN
    UPDATE Viajes SET Estado = @Nuevo WHERE IdViaje = @Id AND Estado = @Actual;
END;
GO

-- ViajeDAL.Publicar
CREATE PROCEDURE dbo.usp_Viaje_Publicar
    @Dador INT,
    @Origen NVARCHAR(60),
    @Destino NVARCHAR(60),
    @Mercaderia NVARCHAR(120),
    @Peso DECIMAL(18,2),
    @FechaCarga DATE,
    @Tarifa DECIMAL(18,2),
    @Comision DECIMAL(18,2),
    @MedioPago NVARCHAR(20),
    @Tarjeta INT,
    @Descuento DECIMAL(18,2),
    @Remito DECIMAL(18,2)
AS
BEGIN
    INSERT INTO Viajes (IdDador, Origen, Destino, Mercaderia, PesoToneladas, FechaCarga, TarifaAcordada, Comision,
        MedioPago, IdTarjeta, DescuentoDador, CargoRemito)
    VALUES (@Dador, @Origen, @Destino, @Mercaderia, @Peso, @FechaCarga, @Tarifa, @Comision,
        @MedioPago, @Tarjeta, @Descuento, @Remito);
END;
GO

CREATE INDEX IX_BitacoraEventos_Fecha ON BitacoraEventos (Fecha DESC);
CREATE INDEX IX_Usuarios_Correo ON Usuarios (CorreoElectronico);
GO

-- ====================================================================== DATOS INICIALES

-- Ciudades: 15 fila(s)
SET IDENTITY_INSERT Ciudades ON;
INSERT INTO Ciudades (IdCiudad, Nombre, Provincia, Latitud, Longitud, DVH) VALUES
    (1, N'Buenos Aires', N'CABA', -34.603700, -58.381600, N'1C516C966303DAE41AC11E600CCF8820344C8C0C3E1E530811F6CE822AAA8AFD'),
    (2, N'La Plata', N'Buenos Aires', -34.920500, -57.953600, N'9E357A25D2E750ECDA3A6E0C4D00266C631A068B028587A2F1F76B69E6D43E1C'),
    (3, N'Mar del Plata', N'Buenos Aires', -38.005500, -57.542600, N'22B41C6805777CC117EC77E2EB513B465D58169238EC69619D58C4B10CB42331'),
    (4, N'Bahía Blanca', N'Buenos Aires', -38.718300, -62.266300, N'ABC45CBFC79E557C7F69CF3A57449893362DEA6622B65231B99C8A6AF7EEC4B4'),
    (5, N'Rosario', N'Santa Fe', -32.944200, -60.650500, N'2EFB5A467CC6FE985F4E0601875FBC962C81EF5DEBE02B2CB1F077248A06B8F2'),
    (6, N'Santa Fe', N'Santa Fe', -31.610700, -60.697300, N'9EB0D564F1996D9E65531375FE24113E7E5CB3FCC64130C84060C9D2389CDB42'),
    (7, N'Córdoba', N'Córdoba', -31.420100, -64.188800, N'80C22692E1DD5F14925E09C8D2EE5527408814793D10D77156E8AFB79733163A'),
    (8, N'Mendoza', N'Mendoza', -32.889500, -68.845800, N'6E6F1A450B8944861B2DE44E75C8F1BF01B82919BA4B4BBF2A25D281BBDE3536'),
    (9, N'San Juan', N'San Juan', -31.537500, -68.536400, N'D2690DDD8D08C544420E66486152105DD747246AE85533841829F0034103D9F9'),
    (10, N'Tucumán', N'Tucumán', -26.808300, -65.217600, N'F79984390E03E5E79432FE1295243DCF14DF715589CB72B220DCE43E709AD085'),
    (11, N'Salta', N'Salta', -24.782100, -65.423200, N'63C9934B31347C6F011F99A6B00685F8F0F1B82789740DB2C002D6F673C90442'),
    (12, N'Paraná', N'Entre Ríos', -31.731900, -60.523800, N'8FE3E21F018C9484C39978F685573C35966EB42AC6027B045BFAF26734EE5E52'),
    (13, N'Corrientes', N'Corrientes', -27.469200, -58.830600, N'DD375E4E90E6645908718E8114E7E3070A76D42DF4676F72329F0B14A024CE60'),
    (14, N'Neuquén', N'Neuquén', -38.951600, -68.059100, N'301C9CEB68F4A58FEA8EE0283018FB8B9C99CC661F9622FD0BBD9897E23112E6'),
    (15, N'Comodoro Rivadavia', N'Chubut', -45.864100, -67.496600, N'EED8A740917A96F816047578FE5BED4FA1A76CE4C9BAEB02BF824108B4EDACFC');
SET IDENTITY_INSERT Ciudades OFF;
GO

-- DVV: 27 fila(s)
INSERT INTO DVV (Tabla, Digito, FechaCalculo) VALUES
    (N'BitacoraEventos', N'53BD823DB1449BEB95971D6670DB9974B398D79CD8B939C5ECC025E86C6ADFE3', '2026-11-04T22:41:15'),
    (N'Calificaciones', N'39225F1C1918F2DF6DFFDB0B4281BC677724C00AFC0A67FAB30ADCBD3544E892', '2026-10-26T16:27:54'),
    (N'Ciudades', N'067D9849B047114796031C7B27CB860E9359A306F7CDBD2D2B16965FB76A3935', '2026-10-26T16:27:54'),
    (N'ClavesApi', N'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', '2026-10-26T16:27:54'),
    (N'Conductores', N'35C291B476CF9870F22FBA2C4C675431835288CFB772ECE62EA3FB2792A1ACF5', '2026-10-26T16:27:54'),
    (N'CuentasDeposito', N'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', '2026-10-26T16:27:54'),
    (N'Disputas', N'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', '2026-10-26T16:27:54'),
    (N'Documentos', N'6ED281AE0F7F49251866A1E7808419CFAD03726891BA67C6AFF900A0717B047C', '2026-11-10T14:17:22'),
    (N'Facturas', N'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', '2026-11-04T21:44:50'),
    (N'HistorialViaje', N'EE925CE097A5F54C14D8BC60D089CE698947199DD4F7778CD22FD23B42BBA773', '2026-10-26T16:27:54'),
    (N'Incidencias', N'FD17D1F4827EE93F7EC8B2213ED76BD03AD115A8D13CD447AE500835604C92CF', '2026-10-26T16:27:54'),
    (N'Notificaciones', N'5C40128563D27D51D365B78BF8CCD44D7E3ED6F54BAEE97766A16EF6ADB36A2D', '2026-11-04T21:13:46'),
    (N'Permisos', N'88195AFC796D4F02A573975B649D0274145984AE8687367DD5C77C65CA1C2F24', '2026-11-04T22:14:17'),
    (N'PlanesSeguro', N'8B416F791DF2D46936E0AACC092215289D55E06F74EE38217258CBAC2A73A771', '2026-10-26T16:27:54'),
    (N'RecuperacionesClave', N'54325C02FD7F0EE0EAB010F7BE4530801FE3DEC737055265BBDC4B0E082DF383', '2026-10-26T16:27:54'),
    (N'Remitos', N'8B5D4DCB706AA11824E10D2358D3C13AE6139BF63AD7823626FC2F4B56BF1270', '2026-10-26T16:27:54'),
    (N'Roles', N'BA9793A5826EBB6EA107650F2A0214D2B9DACF38149F167559ED38AF36BEA401', '2026-10-26T16:27:54'),
    (N'RolFamilia', N'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', '2026-10-26T16:27:54'),
    (N'RolPermiso', N'1EB8E6A0AA81DFE779BD8ABB31A742CC2C12447D324F8212BE1AAA4047F12FF3', '2026-11-04T22:14:17'),
    (N'SolicitudesFactoring', N'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855', '2026-10-26T16:27:54'),
    (N'Suscripciones', N'413D2F3E96C4F5C1B7A181B327F609E053121436598C2B7B719C149130442DFD', '2026-11-10T14:22:01'),
    (N'Tarjetas', N'308B83954BABE24E27454A92FD471BD5E3FFA257295603F69F33F99AA581998A', '2026-10-26T16:27:54'),
    (N'Transacciones', N'220019A85C1B795E5A37651EDEB37A6E1A83A131ED917FE20F908B68798DA8BD', '2026-10-26T16:27:54'),
    (N'UsuarioRol', N'497EF20043D2F26166D0091ADA0FA802C691A484AB8B41258391DCB533332964', '2026-10-26T16:27:54'),
    (N'Usuarios', N'B4958A9F3C91D27FCA51555D4494999216D7510633941FE787DA212F915FB942', '2026-11-10T14:07:44'),
    (N'Vehiculos', N'5B585AFCC65FCD2A23ED42B34EF8B9D85804DF80D1CD2C11926A24F832958DDB', '2026-10-26T16:27:54'),
    (N'Viajes', N'B9D5861D556CF4D353F8F0D6DF261624BC32524AE24BC7CCCBCF6293FB80A221', '2026-11-10T14:17:23');
GO

-- Permisos: 35 fila(s)
SET IDENTITY_INSERT Permisos ON;
INSERT INTO Permisos (IdPermiso, Nombre, DVH) VALUES
    (1, N'Administrar usuarios', N'FB3203AC028E4959C5D5917C6B9C3AB616AB6A2ABFACD307CD8E62526C9F84E5'),
    (2, N'Administrar roles', N'7EC0253B2B734C53090B3A121FFB2EDDBD27900FF6975A5F1B7C592565C6B3BD'),
    (3, N'Administrar permisos', N'040BCE59BBC01B375C07227C417253A7E26E5A7B197A517932DE67A7153D4605'),
    (4, N'Administrar familias', N'A66A3C56CF30F9632BA7D732B5CAF492669154B9939295B059E7F66FEA9790A2'),
    (5, N'Consultar bitácora', N'6D7F2981ED6F51915D0D83B8AF7E7EF3A74B707E9A30E05AAFB3A19A15579502'),
    (6, N'Realizar backup', N'DC134C5B9F603A4C5DE326D5C10A8B3C2E43DC8E3EE0F52BAE6C50FA45D184D5'),
    (7, N'Restaurar base de datos', N'052CDAF2B3832424CC316DA3FC5186655180A3DE944D207B62F1366C87A0E7FD'),
    (8, N'Recuperar dígitos verificadores', N'3A5694336F464D43F8A011F585EB7D8A3273D946AF73A9EF1D0CB242F7565C00'),
    (9, N'Publicar ofertas de carga', N'F35975995081A7C04D68C69389E047FC11A29EF1A9BA4940330B78CE8D724A29'),
    (10, N'Gestionar ofertas de carga', N'1CB09A56B3E536C9AC75AC583FECC269F92A3D87FF66EE01D27B3CF56272853D'),
    (11, N'Consultar match de cargas', N'55D914C38912D66C1BFBF6A261C7811CB4469C09608ADE451CBEB5F86A0EB0D5'),
    (12, N'Aceptar ofertas de carga', N'175F061832DE202CFB0E30B445AB7DDE56E0B42967AAC75F32442874CDA6AF62'),
    (13, N'Gestionar vehículos', N'D1B4557CE405518E27FB4164A9C7460AA2048E1CC988F85C6BF2F9D60927E7A0'),
    (14, N'Gestionar transportistas', N'E8F3B95110C7737556FB0B655632F439B3654F723427E3010D877702EF136C0A'),
    (15, N'Asignar viajes internos', N'E4A424D6477EB9E61AFE7C15A1CF996FA95474A27C66FC9E05785AFA3F167A61'),
    (16, N'Gestionar documentación', N'34539E329885947133F2519D6C3D920E17959ABB5EA8CB4A4A552BCADA1B0824'),
    (17, N'Validar documentación', N'7EE3CE3C0C2445963F0D5BCC0B46A6FF4F954DB1D621B1D5A3B4E88E06F1D096'),
    (18, N'Gestionar remitos digitales', N'C155F46F2D8D32EB46846D88AC750AB699E4BD544B8C77C361EF4C382CA245D4'),
    (19, N'Reportar incidencias de viaje', N'B13B66F99FFAC355B005F1729A9527F810898D0BFB0D1A7E673F4835C5EF3F48'),
    (20, N'Gestionar fondos escrow', N'308A8409962C8EDEA2AC67E4E644C582B8E629A651F8D36D08C9115B412F9988'),
    (21, N'Consultar estado de pago', N'F6DCBDE5C2FFFA073911D9A546392328C2DA964A0A4EC846A4DE1CAEE3FE3F76'),
    (22, N'Gestionar disputas comerciales', N'82F30738886503E4821F9AB44DEAA3A35EA10F20E2EEBB4EC47009E293401670'),
    (23, N'Gestionar factoring', N'D8A47A626992CE41B3CCAD6DD5E4DAC5FEEE21B64417697B2ACFB79040D6F760'),
    (24, N'Gestionar pólizas de seguro', N'E2335C164E0E319F33AF8BA18C25CD4B2F0DA7073940A7BC47A10D8CFB7442AE'),
    (25, N'Actualizar estado de viaje', N'BB3F75AEA8B24EE2FF3A4243F2922A2A227EF5FC1A7FC9968E40704EB4D43066'),
    (26, N'Consultar monitoreo de viaje', N'55B5455501860AD1693B16C37601452230BD1FF6D85DF27AF66EB5B265E59946'),
    (27, N'Calificar servicio de transporte', N'B687FEBCE069AC435E1661F1652058D36E1F9F78CB1EAC1E0BABA3F5E016F0E0'),
    (28, N'Consultar reputación', N'9AC70FB3F32846F05785DF9FA906F47510F5010A79D415DF3AACB34B0E59556E'),
    (29, N'Gestionar suscripción SaaS', N'7049005DC401808398F0C6F4ED1AE26E30F199500520BA8DECDECF95A658321B'),
    (30, N'Generar facturas corporativas', N'54FD797FB87033A77B7818A730F483E6DEAE3621F1383C9B48A88A91D2CEB804'),
    (31, N'Generar reportes gerenciales', N'EE281E28F2E22D9E40905464A6C52E3DA1408F635675DB87A1133530E1FDD0A9'),
    (32, N'Resolver disputas comerciales', N'F0EDFF5286BC93BFB96C4020F7066CCEFCBACC9FCCE46102A0E59CDD079B9065'),
    (33, N'Supervisar viajes', N'92E5AFDB180A5D512FF7EBAC9D4C11E81D97D456B846ED9005439745BC90ED16'),
    (34, N'Consultar facturas', N'CB57E74BFB961E538E8990E495A941E1E96C7003C0C1B25495B15BCA3687759B'),
    (35, N'Gestionar suscripción premium', N'524E8400B90B252D7EF9905D76414C40E4766727DD51C736C8F77C58B0573CB7');
SET IDENTITY_INSERT Permisos OFF;
GO

-- PlanesSeguro: 6 fila(s)
SET IDENTITY_INSERT PlanesSeguro ON;
INSERT INTO PlanesSeguro (IdPlan, Aseguradora, Nombre, CoberturaPorViaje, ViajesPorMes, Precio, Detalle, DVH) VALUES
    (1, N'Aseguradora Pampa', N'Básico', 5000000.00, 8, 15000.00, N'Accidente y vuelco del camión con la carga.', N'2E0C58115523E59C7ABCF2C6257333B3953A3A47AD63C94C87B9371A73484908'),
    (2, N'Aseguradora Pampa', N'Plus', 15000000.00, 20, 35000.00, N'Accidente, vuelco, incendio y robo total de la carga.', N'FC7BC9C2F9B4F21F28356103C5FDB97AE47231CFA7024003C2B9835527912968'),
    (3, N'Aseguradora Pampa', N'Total', 30000000.00, NULL, 55000.00, N'Todo riesgo: accidente, vuelco, incendio, robo total o parcial y daños en la carga y descarga.', N'E84D1A6A6133ADBF4BC9319CEDC820EC85F96D11FBAEDA712AEB512BF0D3CDAA'),
    (4, N'Aseguradora Andina', N'Esencial', 4000000.00, 10, 13500.00, N'Accidente, vuelco e incendio.', N'DAC9A392E5B69EDE2AE2FEDAA94395760C7805FCF54C08A4343B5F555FAD010C'),
    (5, N'Aseguradora Andina', N'Integral', 12000000.00, 25, 32000.00, N'Accidente, vuelco, incendio y robo total o parcial de la carga.', N'F544AC1228452AFB16E381BBB6C8A932E30A8C75A1FF3F9ED794E563D0810C71'),
    (6, N'Aseguradora Andina', N'Premium', 25000000.00, NULL, 49000.00, N'Todo riesgo y asistencia en ruta las 24 horas.', N'EEF78E382505ED33816D54EEB2EF29487E79D0DD6D8671C537AA79A36EBCD8E8');
SET IDENTITY_INSERT PlanesSeguro OFF;
GO

-- Roles: 4 fila(s)
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (IdRol, Nombre, EsFamilia, DVH) VALUES
    (1, N'Administrador', 0, N'821FD0851A4DFC0EF511517DA3D983F46634D4E15C336A9A2218BC99C98D7A5B'),
    (2, N'Administrador de Flota', 0, N'6C7BA98DDB61088DC7828D36FEB24787AD29A846F55932A39C26E853DC72A618'),
    (3, N'Transportista', 0, N'04D18C8A6AC723F75A7EA8C91AD1A89D4D7539889563C99CB912BA272E0EF5A8'),
    (4, N'Dador de carga', 0, N'CD550F09E7BB304112DD4A4DC70EDB60460F082BC035F50D99290DE9ED029EAE');
SET IDENTITY_INSERT Roles OFF;
GO

-- Usuarios: 1 fila(s)
SET IDENTITY_INSERT Usuarios ON;
INSERT INTO Usuarios (IdUsuario, NombreUsuario, CorreoElectronico, ClaveHash, ClaveSalt, Nombre, Apellido, Telefono, CuitCuil, RazonSocial, NumeroLicencia, Idioma, IntentosFallidos, BloqueadoHasta, Estado, FechaAlta, DVH, CondicionIva) VALUES
    (1, N'admin', N'Qsjr2xmGqu9DeKQXLSt5Tdyf1XsnFlNGjePDricitkPmkrSs9UbDtzrsRv0J+RPd', N'AD1A65E6E764DEEF54833FA93AA61EFE6C1C3229EADF26ADE8F9E9010CD5B9A5', N'admin-salt-2026', N'Administrador', N'LogTech', N'9PB0lUpkoOUP3GzGHRnqVeB9nYPIuaLa9qNowvJp8ME=', N'IihkAbitYEHHZQV23gwjlhd7/Om9gMbsSXInmekQ7rg=', N'LogTech', NULL, N'es-ES', 0, NULL, N'Activo', '2026-09-15T13:38:04', N'455602DED6A5710F901DCA40387FD02559DAD956FADD20E988FAB6BE1A86A7AF', N'Consumidor final');
SET IDENTITY_INSERT Usuarios OFF;
GO

-- RolPermiso: 58 fila(s)
INSERT INTO RolPermiso (IdRol, IdPermiso, DVH) VALUES
    (1, 1, N'BBB26435324F96716367F8ECBD3A178B162AB4E30CEBBA857AFD13958A7087E8'),
    (1, 2, N'3125D1504B055F35E7E599F5479E88356FB7963F88201E2AA18F1E111296D253'),
    (1, 3, N'8BE6E3F80BF2D04E2957A73CF3A814588F7D9549D065C442C40CE9053605EBC9'),
    (1, 4, N'CAFBC103D8AF0318BB71E6333D5CB1AA21E81B699D0B970F3A253134792A7BAC'),
    (1, 5, N'D2A3544F903CA03BC67C779C5B99D0C0A20DAED9A067B8E70F9D5F9B42AB112B'),
    (1, 6, N'F19CB159C600ABFEB85601EB4F39BC46C6FE9245A2003B9124DD80A62C2E8508'),
    (1, 7, N'9F107DA8A5CADD6DA0FCE35D4B9F666CAAAECE892A8EE894825E46545B351904'),
    (1, 8, N'B05C3705F69DBB098A16F487FFC2F5F74424E0F1896E3FD504BBE0CB45A99207'),
    (1, 17, N'F96A1CD68A88BAB4F1A4B575CB298F61326EE2666C381AA51A0B97E1FDDEE6A3'),
    (1, 30, N'05783DFA97745C03458C0FB5ABA54CD9C20D82BF9B2E3180AB03F97D06A94EC3'),
    (1, 31, N'82AAA998670E47985197BD337086B03352E404343247AAE9B751BC0E51921868'),
    (1, 32, N'8471409FDA19652F1084ED1DE35032958A252F1CC882713C853457B65C97D466'),
    (1, 33, N'599E814A113C8C1F0C1B4A0B3600503D6DEDECACF42C2D77AC99B74EBC875BF5'),
    (2, 11, N'F9D4BC4C1794637E851C59DC32F0B94BA7B870D0F1C92813B42B071D8FBA3DD0'),
    (2, 12, N'7C451D3C5F8F1C4D9C0B46CA6F71C1412C9C19E00AD087DCC16CB4E76403EECA'),
    (2, 13, N'D85CC2BFEAC4D9321CA7C32707B80C6D9529FEBB93F50FD7F858CC23017E3189'),
    (2, 14, N'B2E464734EEE0B753DA9274E3DF71659A05B40BAC1C0B601A0EEDD9C350845D9'),
    (2, 15, N'16901EB90DF5593487E627B3D50F3885181A402588D9F3D30D4E4CAF0C5C27A3'),
    (2, 16, N'26CBF465ABBEF6836D8503C0182270D95A1F0A3B871EE12A090BEDCFB6E425B9'),
    (2, 18, N'AC0F21A8D78AEA4CA090830EE01330EB3DE40ED44908E10AD39E8234B5A1C880'),
    (2, 19, N'C371D8C19BA0AEA26CCC5480C7593F529410777B2E3F4DC0529EA6BFF3679F1C'),
    (2, 21, N'3214CB1B8596807EEB75E9E9E19A0948B670FA56DFC31C87875CB5FB8AAEF40A'),
    (2, 24, N'50FD086C25A9477C580030AE794B1980132C49B69630D3147F1F5C72731F2783'),
    (2, 25, N'5163896DBDDA7BDA7A1A125383C232324282E8B2E2E572B15175311DF01245C7'),
    (2, 26, N'5B5095F1B9610D32A533B9D53F9FA1E46A7A4785573C054CB8B0D599E57E9F53'),
    (2, 28, N'C78800249B2B37CB95CF2EF6D67EC01695438F06C2939547825725741C142C2A'),
    (2, 29, N'978E73023D499119D525359CC593D79CB6AEE4CCCEE248F387A2862A6DDE0CAE'),
    (2, 31, N'69E1CF146D5D9D94B81B301B3F0E78525BAE5EF61A2B335D17E83BC632EF2AF8'),
    (2, 34, N'4D29860CFAA35B29CAE2155AD611CEEE063109766935EE0B086D2F32283B447C'),
    (3, 11, N'143E1C63033DEA2F750E459BB0CFD4B0256797D3027EFDBE12F466405EF4FD75'),
    (3, 12, N'ED9645AB29195A8E9F80F8DB24B6B8F17A5F325ECE7D5A5061E1571904E3E6F4'),
    (3, 13, N'C75A23472007A746C8431AE75313269F4CB1356C59FFB58370EECE35731D2ADD'),
    (3, 16, N'2550B812916A986E390A50AF27C1213F19670253E6BD87CCD277DEC5B02CA28F'),
    (3, 18, N'9077078AE51B59F880EE041B49355EB0322AA9BF3B9BD0B3B50F9511354A7749'),
    (3, 19, N'C9831AD69AA8A8221799AE3A7C74F58FA200AF42C2D71C671A0A86B5158D9F1A'),
    (3, 21, N'F7A002865627DEA51EC961FEE42A19CE25D4B5497CBBA763F5F82FB70210292A'),
    (3, 23, N'54932D76F873E8BA9F02E39B67774790002C399F595F7CCE152002ECC795668F'),
    (3, 24, N'E8463512C12408C92C06620F1D8CD7BAD42BAB75F5B81D2DCBF8BA5CC7F7CEF7'),
    (3, 25, N'1A865CBA1D874B772F681D832A5FFD54E5D5E37C1F0DEF9F7C4A4C7E8F978039'),
    (3, 26, N'57DC31B6567F84497F745C3A6DF2A3212511C0C8F3FC9A60284DF10558B63273'),
    (3, 27, N'70231E6D6EC42D1C0806722E5555B3ACEE54470793AEE03C800555F544E0F322'),
    (3, 28, N'9B653A65E51DB09A4FB13965175F8D8EB84A6ECDBB1C8ABFA84A86C33CFF5BB9'),
    (3, 34, N'B95E6FBFDB5A13B0B623AF8A2A9ECDFF070487125C79E10EE50360E53D06BBBD'),
    (3, 35, N'1860DCEB056B4522214E3A6C474CB628174BE7EC9720C049011854D5B4DA190F'),
    (4, 9, N'4EFB6FB768D1CB04AFF3B7C0F8963EE2789D7C52797A0106252ED4E043381BEC'),
    (4, 10, N'54947220581E2DE734F09FB592E1BB459B4025186CEA3DB7480CE50812369F77'),
    (4, 11, N'0711399DEFE7C83614270068299546C8CBDDCCB12BB3A7AAEA2D0CDD76E0B0D0'),
    (4, 16, N'267574AB7E2E1C8E87F62621C832786FDA89869B04EC3EEBD18FFF76D69138DC'),
    (4, 18, N'AF9FEC1F835539AE9868E47D833D30B12D292335E1CE91FABDA5E54AD421214B'),
    (4, 20, N'262A4324391C5DA912151DC141A7418290D1430A8F8DFA78E190773EC7B67B7B'),
    (4, 21, N'B5D89F02C1AF719CBD63E507F42CAE987EFC63965820BE3F27A97299DC6DDDB9'),
    (4, 22, N'D5BB4544CB31EEF0900B15F11040AE75BE0B4846FD5CE4C7467F92AA77C28CC5'),
    (4, 24, N'47192DA4F9706E8BE424902499C8855108C7CD42501B7D3276D402C10364AEA7'),
    (4, 26, N'80024981AD44C10B6276B4D66A4065283D9730E3401094FD338B9F8E59DBEFA7'),
    (4, 27, N'B03595EAB54FB64B3B3E547488B0A49CABB9AD1AFD207C5C6E80B620FE98F56E'),
    (4, 28, N'EEF442BCA9CD381BED941600B0017D02417846B5819EC5D7FD034E2C055C2AD6'),
    (4, 29, N''),
    (4, 34, N'14739AF2733B6E1768F4D9F979022F6A025C801069C3FC5757A3B65882F3C787');
GO

-- UsuarioRol: 1 fila(s)
INSERT INTO UsuarioRol (IdUsuario, IdRol, DVH) VALUES
    (1, 1, N'BBB26435324F96716367F8ECBD3A178B162AB4E30CEBBA857AFD13958A7087E8');
GO

-- Los dígitos verificadores (DVH y DVV) se recalculan en el servidor donde se
-- carga el script, así la verificación de integridad da bien.
EXEC dbo.RecalcularDigitos;
GO
