using System.Collections.Generic;
using LogTech.BE;

namespace LogTech.DAL
{
    public sealed class CiudadDAL
    {
        public List<CiudadBE> Listar()
        {
            return EjecutorDAL.Leer("SELECT Nombre, Provincia, Latitud, Longitud FROM Ciudades ORDER BY Nombre;",
                lector => new CiudadBE
                {
                    Nombre = lector.GetString(0),
                    Provincia = lector.GetString(1),
                    Latitud = lector.GetDecimal(2),
                    Longitud = lector.GetDecimal(3)
                });
        }
    }
}
