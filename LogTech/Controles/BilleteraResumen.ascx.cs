using System.Web.UI;

namespace LogTech.Controles
{
    // Saldo disponible y fondos retenidos en escrow de la billetera (componente del análisis, 12.7.3).
    // Lo usa Billetera.ascx; los montos los pone la pantalla que lo contiene.
    public partial class BilleteraResumen : UserControl
    {
        // Las propiedades van en el ViewState: así el control se sigue viendo después de un postback que no lo vuelve a cargar.
        public decimal SaldoDisponible
        {
            get { return ViewState["SaldoDisponible"] == null ? 0 : (decimal)ViewState["SaldoDisponible"]; }
            set { ViewState["SaldoDisponible"] = value; }
        }

        public decimal SaldoRetenido
        {
            get { return ViewState["SaldoRetenido"] == null ? 0 : (decimal)ViewState["SaldoRetenido"]; }
            set { ViewState["SaldoRetenido"] = value; }
        }

        // Todos los clientes tienen saldo; la pantalla puede ocultarlo.
        public bool MostrarSaldo
        {
            get { return ViewState["MostrarSaldo"] != null && (bool)ViewState["MostrarSaldo"]; }
            set { ViewState["MostrarSaldo"] = value; }
        }

        public string DetalleRetenido
        {
            get { return (string)ViewState["DetalleRetenido"]; }
            set { ViewState["DetalleRetenido"] = value; }
        }
    }
}
