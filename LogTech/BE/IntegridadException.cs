using System;

namespace LogTech.BE
{
    // Una tabla fue alterada por afuera (sus dígitos verificadores no coinciden con los datos): no se guardan cambios en
    // ella hasta que el administrador recalcule los dígitos o restaure un backup (ERR049). Hereda de
    // InvalidOperationException para que las pantallas muestren el mensaje como cualquier otro error de la operación.
    public sealed class IntegridadException : InvalidOperationException
    {
        // Lo que ve cualquier usuario que no sea el administrador o el de contingencia mientras la base está dañada.
        public const string SistemaNoDisponible = "El sistema no está disponible en este momento. Intentá más tarde.";

        public IntegridadException(string tabla)
            : base("No se guardaron los cambios: la tabla " + tabla + " tiene una falla de integridad. Avisale al administrador.")
        {
            Tabla = tabla;
        }

        public string Tabla { get; private set; }
    }
}
