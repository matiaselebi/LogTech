using LogTech.Servicios;

namespace LogTech.DAL
{
    // CBU o alias (cifrado con AES) donde se le deposita al transportista o a la flota lo que retira de la billetera (uno por usuario).
    public sealed class CuentaDepositoDAL
    {
        // null si no guardó ninguno.
        public string Obtener(int idUsuario)
        {
            return CifradoServicio.Descifrar(EjecutorDAL.LeerValor("SELECT CbuOAlias FROM CuentasDeposito WHERE IdUsuario = @Id;",
                EjecutorDAL.Parametro("@Id", idUsuario)) as string);
        }

        // Lo guarda o reemplaza el que había.
        public void Guardar(int idUsuario, string cbuOAlias)
        {
            EjecutorDAL.EjecutarCambio(@"UPDATE CuentasDeposito SET CbuOAlias = @Cbu, FechaAlta = SYSDATETIME() WHERE IdUsuario = @Id;
                IF @@ROWCOUNT = 0 INSERT INTO CuentasDeposito (IdUsuario, CbuOAlias) VALUES (@Id, @Cbu);", null,
                EjecutorDAL.Parametro("@Id", idUsuario), EjecutorDAL.Parametro("@Cbu", CifradoServicio.Cifrar(cbuOAlias)));
        }

        public bool Eliminar(int idUsuario)
        {
            return EjecutorDAL.EjecutarCambio("DELETE FROM CuentasDeposito WHERE IdUsuario = @Id;", null,
                EjecutorDAL.Parametro("@Id", idUsuario)) == 1;
        }
    }
}
