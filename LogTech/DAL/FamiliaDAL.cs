using System.Data;
using System.Data.SqlClient;

namespace LogTech.DAL
{
    public sealed class FamiliaDAL
    {
        public bool Agregar(int idPadre, int idFamilia)
        {
            // El bloqueo dura hasta finalizar la transacción de EjecutorDAL.
            const string sql = @"DECLARE @Total INT;
                SELECT @Total = COUNT(*) FROM RolFamilia WITH (TABLOCKX, HOLDLOCK);
                IF NOT EXISTS (SELECT 1 FROM Roles WHERE IdRol = @Padre)
                    OR NOT EXISTS (SELECT 1 FROM Roles WHERE IdRol = @Familia AND EsFamilia = 1)
                    OR EXISTS (SELECT 1 FROM RolFamilia WHERE IdPadre = @Padre AND IdFamilia = @Familia)
                BEGIN SELECT 0; RETURN; END;
                DECLARE @Visitados TABLE (Id INT PRIMARY KEY);
                INSERT INTO @Visitados VALUES (@Familia);
                WHILE 1 = 1
                BEGIN
                    INSERT INTO @Visitados
                    SELECT DISTINCT rf.IdFamilia FROM RolFamilia rf
                    JOIN @Visitados v ON v.Id = rf.IdPadre
                    WHERE NOT EXISTS (SELECT 1 FROM @Visitados WHERE Id = rf.IdFamilia);
                    IF @@ROWCOUNT = 0 BREAK;
                END;
                IF EXISTS (SELECT 1 FROM @Visitados WHERE Id = @Padre)
                BEGIN SELECT 0; RETURN; END;
                INSERT INTO RolFamilia (IdPadre, IdFamilia) VALUES (@Padre, @Familia);
                SELECT 1;";
            return EjecutorDAL.EjecutarBooleano(sql,
                new SqlParameter("@Padre", SqlDbType.Int) { Value = idPadre },
                new SqlParameter("@Familia", SqlDbType.Int) { Value = idFamilia });
        }

        public bool Quitar(int idPadre, int idFamilia)
        {
            return EjecutorDAL.EjecutarBooleano(
                "DELETE FROM RolFamilia WHERE IdPadre = @Padre AND IdFamilia = @Familia; SELECT CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END;",
                new SqlParameter("@Padre", SqlDbType.Int) { Value = idPadre },
                new SqlParameter("@Familia", SqlDbType.Int) { Value = idFamilia });
        }
    }
}
