using LogTech.BE;
using LogTech.DAL;

namespace LogTech.BLL
{
    public sealed class FamiliaBLL
    {
        private readonly FamiliaDAL familias = new FamiliaDAL();

        public bool Agregar(int idPadre, int idFamilia)
        {
            RolBE padre = null;
            RolBE familia = null;
            foreach (var rol in new RolPermisoDAL().ObtenerTodos())
            {
                if (rol.IdRol == idPadre) padre = rol;
                if (rol.IdRol == idFamilia && rol.EsFamilia) familia = rol;
            }
            if (padre == null || familia == null) return false;

            // El Composite valida duplicados y ciclos antes de guardar.
            padre.Agregar(familia);
            return familias.Agregar(idPadre, idFamilia);
        }

        public bool Quitar(int idPadre, int idFamilia)
        {
            return familias.Quitar(idPadre, idFamilia);
        }
    }
}
