using System;
using System.Collections.Generic;

namespace LogTech.BE
{
    // Patrón Composite - Compuesto: un rol o una familia. Puede contener permisos
    // sueltos y otras familias, y sus permisos son la suma de los de sus hijos.
    public sealed class RolBE : ComponentePermisoBE
    {
        private readonly List<ComponentePermisoBE> elementos = new List<ComponentePermisoBE>();

        public RolBE(int idRol, string nombre) : base(nombre) { IdRol = idRol; }

        public int IdRol { get; private set; }
        public bool EsFamilia { get; set; }
        public IList<ComponentePermisoBE> Hijos { get { return elementos.AsReadOnly(); } }

        public void Agregar(ComponentePermisoBE elemento)
        {
            if (elemento == null) throw new ArgumentNullException("elemento");

            // Una familia no puede contener a quien la contiene: sería un ciclo.
            var familia = elemento as RolBE;
            if (familia != null && familia.Contiene(IdRol))
                throw new InvalidOperationException("No se puede crear un ciclo entre familias.");

            foreach (var hijo in elementos)
            {
                if (EsElMismo(hijo, elemento))
                    throw new InvalidOperationException("El elemento ya está agregado.");
            }
            elementos.Add(elemento);
        }

        // true si este rol es el buscado o lo tiene adentro, en cualquier nivel.
        public bool Contiene(int idRol)
        {
            if (IdRol == idRol) return true;
            foreach (var hijo in elementos)
            {
                var familia = hijo as RolBE;
                if (familia != null && familia.Contiene(idRol)) return true;
            }
            return false;
        }

        // Junta los permisos de todos los hijos, sin repetir.
        public override List<string> ObtenerPermisos()
        {
            var permisos = new List<string>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hijo in elementos)
            {
                foreach (var permiso in hijo.ObtenerPermisos())
                {
                    if (vistos.Add(permiso)) permisos.Add(permiso);
                }
            }
            return permisos;
        }

        // Dos familias son la misma si tienen el mismo Id; dos permisos, si tienen el
        // mismo nombre. Un permiso y una familia nunca son lo mismo.
        private static bool EsElMismo(ComponentePermisoBE a, ComponentePermisoBE b)
        {
            var rolA = a as RolBE;
            var rolB = b as RolBE;
            if (rolA != null && rolB != null) return rolA.IdRol == rolB.IdRol;
            if (rolA == null && rolB == null) return string.Equals(a.Nombre, b.Nombre, StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }
}
