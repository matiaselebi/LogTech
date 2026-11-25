using System;
using System.Configuration;

namespace LogTech.Servicios
{
    // Aseguradoras SIMULADAS (decisión de la entrega final): emiten la póliza de un plan y responden como lo haría la API
    // de una aseguradora, pero no hay una aseguradora real atrás. Para conectar una real se reemplaza solo esta clase.
    public static class AseguradoraServicio
    {
        // Comisión que la aseguradora le paga a LogTech por cada prima cobrada desde la aplicación.
        public const decimal ComisionLogTech = 0.05m;

        // Emite la póliza del plan y devuelve su número, o null si la aseguradora no responde (ERR042). Con
        // AseguradoraDisponible = false en Web.config se simula que no responde.
        public static string EmitirPoliza(string aseguradora, string plan)
        {
            if (ConfigurationManager.AppSettings["AseguradoraDisponible"] == "false") return null;
            return "POL-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
        }
    }
}
