namespace LogTech.DAL
{
    public sealed class CalificacionDAL
    {
        // R19: una sola calificación por viaje, y solo de un viaje entregado del propio dador.
        // Devuelve false si el viaje ya estaba calificado.
        public bool Alta(int idViaje, int idDador, int puntaje, string resena)
        {
            const string sql = @"INSERT INTO Calificaciones (IdViaje, IdDador, IdTransportista, Puntaje, Resena)
                SELECT v.IdViaje, v.IdDador, v.IdTransportista, @Puntaje, @Resena FROM Viajes v
                WHERE v.IdViaje = @Viaje AND v.IdDador = @Dador AND v.Estado = N'Entregado'
                  AND NOT EXISTS (SELECT 1 FROM Calificaciones k WHERE k.IdViaje = v.IdViaje);";
            return EjecutorDAL.EjecutarCambio(sql, null, EjecutorDAL.Parametro("@Viaje", idViaje),
                EjecutorDAL.Parametro("@Dador", idDador), EjecutorDAL.Parametro("@Puntaje", puntaje),
                EjecutorDAL.Parametro("@Resena", resena)) == 1;
        }
    }
}
