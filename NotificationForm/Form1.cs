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
using CotacaoNotification;

namespace NotificationForm
{
    public partial class Output : Form
    {
        private readonly MonitorCotacao monitor = new MonitorCotacao();
        protected System.Timers.Timer i_Timer = null;
        
        public Output()
        {
            InitializeComponent();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void fecharToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
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

        private void loadPage()
        {
            CotarDolar();   
        }


        protected void CotacaoSchedulada(object sender, System.Timers.ElapsedEventArgs e)
        {
            CotarDolar();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
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
            });
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void sairToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            // Para o timer antes de descartar o form; senao ele continua disparando.
            if (i_Timer != null)
                i_Timer.Dispose();
            this.Dispose();
        }

        private void Output_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void Output_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void Output_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
