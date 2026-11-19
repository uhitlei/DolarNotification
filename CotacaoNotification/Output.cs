using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CotacaoNotification
{
  
    public partial class Output : Form
    {

        private readonly MonitorCotacao monitor = new MonitorCotacao();
        protected System.Timers.Timer i_Timer = null;
        private List<Grafico> GraficoGeral;

        public Output()
        {
            InitializeComponent();
            GraficoGeral = new List<Grafico>();
            this.Hide();
        }

        private void Output_Load(object sender, EventArgs e)
        {
            // notifyIcon1.ShowBalloonTip(100, "Notification", "Inicio da Verificação", ToolTipIcon.Info);
            //richTextBox1.Text = "Início: " + DateTime.Now.ToString("dd/MM/yy HH:mm");

            #region Configura e Inicia o Timer

            i_Timer = new System.Timers.Timer();
            i_Timer.Enabled = true;
            i_Timer.Interval = 30000;//Global.Instance.IntervaloExecucao;//Global.Instance.IntervaloExecucao;//60000; //300000;//teste de 1 min 60.000 // Padrao 300.000 sem '.'

            i_Timer.Elapsed += new System.Timers.ElapsedEventHandler(CotacaoSchedulada);
            i_Timer.Start();
            #endregion

            // loadPage();
            this.Hide();
        }

        protected void CotacaoSchedulada(object sender, System.Timers.ElapsedEventArgs e)
        {
            CotarDolar();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Para o timer antes de descartar o form; senao ele continua disparando.
            if (i_Timer != null)
                i_Timer.Dispose();
            this.Dispose();
        }

        private void CotarDolar()
        {
            decimal comercial;
            decimal turismo;

            // So notifica quando as duas cotacoes foram lidas; antes, uma falha
            // de rede ou de parse gerava um aviso com "Atual(R$.00)".
            if (!FonteCotacao.TryLer(CotacaoParser.UrlComercial, CotacaoParser.PadraoComercial, out comercial)
                || !FonteCotacao.TryLer(CotacaoParser.UrlTurismo, CotacaoParser.PadraoTurismo, out turismo))
            {
                return;
            }

            decimal comercialAnterior = monitor.UltimoComercial;
            decimal turismoAnterior = monitor.UltimoTurismo;
            if (!monitor.Atualizar(comercial, turismo))
                return;

            DateTime agora = DateTime.Now;
            string mensagem = MonitorCotacao.MontarMensagem(comercialAnterior, comercial, turismoAnterior, turismo, CultureInfo.CurrentCulture);
            string dica = MonitorCotacao.MontarDica(comercial, turismo, CultureInfo.CurrentCulture);

            // O System.Timers.Timer dispara em outra thread; controles so podem
            // ser alterados na thread da interface.
            if (IsDisposed || !IsHandleCreated)
                return;
            BeginInvoke((MethodInvoker)delegate
            {
                if (IsDisposed)
                    return;
                notifyIcon1.ShowBalloonTip(100, "Alteração no Dolar!", mensagem, ToolTipIcon.Info);
                notifyIcon1.Text = dica.Length > 63 ? dica.Substring(0, 63) : dica;
                GraficoGeral.Add(new Grafico { DataC = agora, DataT = agora, ValorC = comercial, ValorT = turismo });
                AtualizaGrafico(GraficoGeral.Last());
            });
        }

        private void AtualizaGrafico( Grafico ultimo)
        {
            //chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            //chart1.Series.Add("Comercial");
           // chart1.Series.Add("Turismo");


            chart1.Series["Comercial"].Points.AddXY(ultimo.DataC, ultimo.ValorC);
            chart1.Series["Turismo"].Points.AddXY(ultimo.DataT, ultimo.ValorT);


            //if (chart1.Series["Comercial"].Points.Count > 0)
            //{
            //    chart1.Series["Comercial"].Points.Clear();
            //    chart1.Series["Turismo"].Points.Clear();
            //}

            
            

            //foreach (Grafico item in GraficoGeral)
            //{
            //    chart1.Series["Comercial"].Points.AddXY(ultimo.DataC, ultimo.ValorC);
            //    chart1.Series["Turismo"].Points.AddXY(ultimo.DataT, ultimo.ValorT);
            //}
            
            
        }


        private void esconderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }
    }
  
}
