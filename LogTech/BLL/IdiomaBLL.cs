using LogTech.DAL;
using LogTech.Servicios;

namespace LogTech.BLL
{
    public sealed class IdiomaBLL
    {
        private readonly IdiomaDAL idiomas = new IdiomaDAL();

        public void Actualizar(int idUsuario, string idioma)
        {
            idiomas.Actualizar(idUsuario, CatalogoIdiomas.Normalizar(idioma));
        }
    }
}
