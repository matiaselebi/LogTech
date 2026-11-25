using System.Collections.Generic;

namespace LogTech.BE
{
    // Patrón Composite - Hoja: un permiso suelto. Se devuelve a sí mismo.
    public sealed class PermisoBE : ComponentePermisoBE
    {
        public PermisoBE(string nombre) : base(nombre) { }

        public override List<string> ObtenerPermisos()
        {
            return new List<string> { Nombre };
        }
    }
}
