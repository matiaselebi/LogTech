using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class IntegridadDAL
    {
        // Ejecuta dbo.VerificarIntegridad y devuelve los problemas encontrados:
        // filas alteradas y tablas con filas agregadas o borradas. Lista vacía = base íntegra.
        public IList<ProblemaIntegridadBE> Verificar()
        {
            var problemas = new List<ProblemaIntegridadBE>();
            using (var conexion = new SqlConnection(Conexion.cadena))
            using (var comando = new SqlCommand("EXEC dbo.VerificarIntegridad;", conexion))
            {
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        problemas.Add(new ProblemaIntegridadBE
                        {
                            Tabla = lector.GetString(0),
                            Fila = lector.IsDBNull(1) ? string.Empty : lector.GetString(1),
                            Detalle = lector.GetString(2)
                        });
                    }
                }
            }
            return problemas;
        }
    }
}
