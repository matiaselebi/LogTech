namespace LogTech.BE
{
    public enum EstadoAutenticacion
    {
        Exitoso,
        CredencialesInvalidas,
        Bloqueado,
        Inactivo,
        IntegridadComprometida   // la base está dañada: "el sistema no está disponible" (salvo el administrador)
    }

    public sealed class ResultadoAutenticacionBE
    {
        public EstadoAutenticacion Estado { get; set; }
        public UsuarioBE Usuario { get; set; }
        public int IntentosRestantes { get; set; }
        public int SegundosBloqueo { get; set; }
        // El administrador entró con la base dañada: lo que se rompió (vacío si la base está sana).
        public System.Collections.Generic.IList<ProblemaIntegridadBE> Problemas { get; set; }
    }
}
