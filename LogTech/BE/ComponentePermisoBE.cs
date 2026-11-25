using System.Collections.Generic;

namespace LogTech.BE
{
    // Patrón Composite - Componente: lo que tienen en común un permiso suelto (hoja)
    // y un rol o familia (compuesto). Los dos saben devolver sus permisos.
    public abstract class ComponentePermisoBE
    {
        protected ComponentePermisoBE(string nombre) { Nombre = nombre; }

        public string Nombre { get; private set; }

        public abstract List<string> ObtenerPermisos();
    }
}
