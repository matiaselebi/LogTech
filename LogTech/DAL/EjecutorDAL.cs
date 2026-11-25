using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using LogTech.BE;

namespace LogTech.DAL
{
    // Ejecuta los SQL contra la base: los cambios dentro de una transacción y las
    // lecturas armando una lista de objetos. Las operaciones principales están en procedimientos almacenados
    // (usp_Clase_Metodo, en LogTechDB): en vez del SQL se pasa el nombre del procedimiento.
    //
    // Dígitos verificadores: todas las tablas los tienen (menos DVV). Cada cambio, antes de escribir,
    // comprueba que las tablas que nombra su SQL (o su procedimiento) estén sanas (dbo.TablaIntegra): si alguien
    // las alteró por afuera no se guarda nada, porque recalcular los dígitos taparía el daño (IntegridadException).
    // Después de escribir recalcula los dígitos de esas tablas (dbo.RecalcularDigitos).
    internal static class EjecutorDAL
    {
        // Texto de cada procedimiento, leído de la base una sola vez (para saber qué tablas nombra).
        private static readonly Dictionary<string, string> Definiciones = new Dictionary<string, string>();
        private static readonly object Candado = new object();

        // Todas las tablas de LogTechDB con dígito verificador (menos BitacoraEventos, que firma su propia fila: ver
        // BitacoraDAL, y DVV, que guarda los verticales).
        private static readonly string[] Tablas =
        {
            "Roles", "Permisos", "Usuarios", "UsuarioRol", "RolPermiso", "UsuarioPermiso", "UsuarioFamilia", "RolFamilia", "RecuperacionesClave",
            "Suscripciones", "Ciudades", "Vehiculos", "Conductores", "Viajes", "HistorialViaje", "Incidencias",
            "Remitos", "Calificaciones", "Documentos", "Notificaciones", "ClavesApi", "Tarjetas", "CuentasDeposito",
            "Transacciones", "Disputas", "SolicitudesFactoring", "PlanesSeguro", "Facturas"
        };

        // Los procedimientos almacenados se llaman por su nombre, que empieza con «usp_».
        private static bool EsProcedimiento(string sql)
        {
            return sql.StartsWith("usp_");
        }

        // Comando para un SQL o para un procedimiento almacenado.
        private static SqlCommand Comando(string sql, SqlConnection conexion, SqlTransaction transaccion = null)
        {
            var comando = new SqlCommand(sql, conexion, transaccion);
            if (EsProcedimiento(sql)) comando.CommandType = CommandType.StoredProcedure;
            return comando;
        }

        // Para las clases de la DAL que arman su propio comando y llaman a un procedimiento almacenado.
        internal static SqlCommand Procedimiento(string nombre, SqlConnection conexion)
        {
            return new SqlCommand(nombre, conexion) { CommandType = CommandType.StoredProcedure };
        }

        // Texto del procedimiento (OBJECT_DEFINITION). Se lee la primera vez y queda guardado.
        private static string Definicion(string procedimiento)
        {
            lock (Candado)
            {
                string texto;
                if (Definiciones.TryGetValue(procedimiento, out texto)) return texto;
                using (var conexion = new SqlConnection(Conexion.cadena))
                using (var comando = new SqlCommand("SELECT OBJECT_DEFINITION(OBJECT_ID(@Nombre));", conexion))
                {
                    comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 128).Value = procedimiento;
                    conexion.Open();
                    texto = Convert.ToString(comando.ExecuteScalar());
                }
                Definiciones[procedimiento] = texto;
                return texto;
            }
        }

        // Tablas que nombra el SQL (o el procedimiento) de un cambio: las que escribe y las que lee para escribir, más
        // las que se pasen.
        private static List<string> TablasDe(string sql, string extra)
        {
            var texto = EsProcedimiento(sql) ? Definicion(sql) : sql;
            var lista = new List<string>();
            foreach (var tabla in Tablas)
                if (Regex.IsMatch(texto, @"\b" + tabla + @"\b")) lista.Add(tabla);
            if (extra != null)
                foreach (var tabla in extra.Split(','))
                    if (!lista.Contains(tabla.Trim())) lista.Add(tabla.Trim());
            return lista;
        }

        // Para los SQL que terminan con SELECT 1 (se hizo) o SELECT 0 (no se hizo).
        internal static bool EjecutarBooleano(string sql, params SqlParameter[] parametros)
        {
            return Convert.ToInt32(CambiarYLeer(sql, parametros)) == 1;
        }

        // Un cambio que devuelve un valor (por ejemplo, con OUTPUT o un SELECT al final).
        internal static object CambiarYLeer(string sql, params SqlParameter[] parametros)
        {
            var tablas = TablasDe(sql, null);
            using (var conexion = new SqlConnection(Conexion.cadena))
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                using (var comando = Comando(sql, conexion, transaccion))
                {
                    ComprobarIntegridad(conexion, transaccion, tablas);
                    comando.Parameters.AddRange(parametros);
                    var resultado = comando.ExecuteScalar();
                    foreach (var tabla in tablas) RecalcularDigitos(conexion, transaccion, tabla);
                    transaccion.Commit();
                    return resultado;
                }
            }
        }

        // Un cambio que devuelve varias filas (UPDATE ... OUTPUT): comprueba y recalcula los dígitos como los demás.
        internal static List<T> CambiarYLeerFilas<T>(string sql, Func<SqlDataReader, T> leerFila, params SqlParameter[] parametros)
        {
            var tablas = TablasDe(sql, null);
            var lista = new List<T>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                using (var comando = Comando(sql, conexion, transaccion))
                {
                    ComprobarIntegridad(conexion, transaccion, tablas);
                    comando.Parameters.AddRange(parametros);
                    using (var lector = comando.ExecuteReader())
                    {
                        while (lector.Read()) lista.Add(leerFila(lector));
                    }
                    foreach (var tabla in tablas) RecalcularDigitos(conexion, transaccion, tabla);
                    transaccion.Commit();
                }
            }
            return lista;
        }

        // tablaProtegida: tablas extra separadas por coma (las que nombra el SQL ya se detectan solas); puede ser null.
        // Devuelve la cantidad de filas afectadas: 0 avisa que la condición del WHERE ya
        // no se cumplía (por ejemplo, otro usuario tomó la oferta un instante antes).
        internal static int EjecutarCambio(string sql, string tablaProtegida, params SqlParameter[] parametros)
        {
            var tablas = TablasDe(sql, tablaProtegida);
            using (var conexion = new SqlConnection(Conexion.cadena))
            {
                conexion.Open();
                using (var transaccion = conexion.BeginTransaction())
                using (var comando = Comando(sql, conexion, transaccion))
                {
                    ComprobarIntegridad(conexion, transaccion, tablas);
                    comando.Parameters.AddRange(parametros);
                    var filas = comando.ExecuteNonQuery();
                    foreach (var tabla in tablas) RecalcularDigitos(conexion, transaccion, tabla);
                    transaccion.Commit();
                    return filas;
                }
            }
        }

        // Un cambio que firma sus propias filas (la bitácora): no comprueba ni recalcula nada.
        internal static void EjecutarSinRecalcular(string sql, params SqlParameter[] parametros)
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = Comando(sql, conexion))
            {
                comando.Parameters.AddRange(parametros);
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        // Antes de cambiar: si una tabla fue alterada por afuera, se cancela todo (ERR049).
        internal static void ComprobarIntegridad(SqlConnection conexion, SqlTransaction transaccion, IEnumerable<string> tablas)
        {
            foreach (var tabla in tablas)
            {
                using (var comando = new SqlCommand("SELECT dbo.TablaIntegra(@Tabla);", conexion, transaccion))
                {
                    comando.Parameters.Add("@Tabla", SqlDbType.NVarChar, 128).Value = tabla;
                    if (!Convert.ToBoolean(comando.ExecuteScalar())) throw new IntegridadException(tabla);
                }
            }
        }

        internal static void RecalcularDigitos(SqlConnection conexion, SqlTransaction transaccion, string tabla)
        {
            using (var comando = new SqlCommand("EXEC dbo.RecalcularDigitos @Tabla;", conexion, transaccion))
            {
                comando.Parameters.Add("@Tabla", SqlDbType.NVarChar, 128).Value = tabla;
                comando.ExecuteNonQuery();
            }
        }

        // Lee todas las filas del SELECT; "leerFila" arma un objeto con cada fila.
        internal static List<T> Leer<T>(string sql, Func<SqlDataReader, T> leerFila, params SqlParameter[] parametros)
        {
            var lista = new List<T>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = Comando(sql, conexion))
            {
                comando.Parameters.AddRange(parametros);
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read()) lista.Add(leerFila(lector));
                }
            }
            return lista;
        }

        // Primer valor del SELECT (un COUNT, un EXISTS, un promedio...).
        internal static object LeerValor(string sql, params SqlParameter[] parametros)
        {
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = Comando(sql, conexion))
            {
                comando.Parameters.AddRange(parametros);
                conexion.Open();
                return comando.ExecuteScalar();
            }
        }

        // Parámetro con el tipo deducido del valor; null se guarda como NULL.
        internal static SqlParameter Parametro(string nombre, object valor)
        {
            return new SqlParameter(nombre, valor ?? DBNull.Value);
        }

        // Lectura de columnas que pueden venir en NULL.
        internal static string Texto(SqlDataReader lector, int columna)
        {
            return lector.IsDBNull(columna) ? null : lector.GetString(columna);
        }

        internal static int? Entero(SqlDataReader lector, int columna)
        {
            return lector.IsDBNull(columna) ? (int?)null : lector.GetInt32(columna);
        }

        internal static decimal? Decimal(SqlDataReader lector, int columna)
        {
            return lector.IsDBNull(columna) ? (decimal?)null : lector.GetDecimal(columna);
        }
    }
}
