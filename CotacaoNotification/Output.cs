﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        Decimal DolarT;
        Decimal DolarC;
        public string formatValor = "#,#.00#;(#,#.00#)";
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
            this.Dispose();
        }

        private void CotarDolar()
        {

            WebRequest req = WebRequest.Create("http://www.dolarhoje.net.br");
            WebResponse res = req.GetResponse();
            Stream dataStream = res.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string Resposta = reader.ReadToEnd();
            Decimal DolarCom = 0;
            Decimal DolarTur = 0;

            try
            {
                System.Text.RegularExpressions.MatchCollection matches =
                System.Text.RegularExpressions.Regex.Matches(Resposta, @"<span id=""moeda"" class>(.*?)</span>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        Resposta = match.Groups[1].Value;
                        Resposta = Resposta.Substring(Resposta.LastIndexOf("R$") + 2, 4);
                        DolarCom = Decimal.Parse(Resposta, System.Globalization.NumberStyles.Currency);
                        //richTextBox1.Text = DateTime.Now.ToString("dd/MM/yy HH:mm") + " Dolar Comercial: " + match.Groups[1].Value.Substring(match.Groups[1].Value.LastIndexOf("R$")) + "\n" + richTextBox1.Text;

                    }
                }

                req = WebRequest.Create("http://www.dolarhoje.net.br/dolarturismo.php");
                res = req.GetResponse();
                dataStream = res.GetResponseStream();
                reader = new StreamReader(dataStream);
                Resposta = reader.ReadToEnd();


                matches = System.Text.RegularExpressions.Regex.Matches(Resposta, @"<span id=""moeda"">(.*?)</span>", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        Resposta = match.Groups[1].Value;
                        Resposta = Resposta.Substring(Resposta.LastIndexOf("R$") + 2, 4);
                        DolarTur = Decimal.Parse(Resposta, System.Globalization.NumberStyles.Currency);
                        //richTextBox1.Text = DateTime.Now.ToString("dd/MM/yy HH:mm") + " Dolar Turismo: " + match.Groups[1].Value.Substring(match.Groups[1].Value.LastIndexOf("R$")) + "\n" + richTextBox1.Text;
                    }
                }


            }
            catch (Exception)
            {


            }


            if (DolarTur != DolarT || DolarCom != DolarC)
            {
                notifyIcon1.ShowBalloonTip(100
                    , "Alteração no Dolar!"
                    , "Dolar Comercial: Ultimo(R$" + DolarC.ToString(formatValor) + ") Atual(R$" + DolarCom.ToString(formatValor) + ") \n"
                    + "Dolar Turismo: Ultimo(R$" + DolarT.ToString(formatValor) + ") Atual(R$" + DolarTur.ToString(formatValor) + ") \n"
                    , ToolTipIcon.Info);

                notifyIcon1.Text = "Comercial: R$" + DolarCom.ToString(formatValor) + " \n"
                                 + "Turismo: R$" + DolarTur.ToString(formatValor);
                DolarT = DolarTur;
                DolarC = DolarCom;
                GraficoGeral.Add(new Grafico{ DataC = DateTime.Now, DataT = DateTime.Now, ValorC = DolarC, ValorT= DolarT });
                AtualizaGrafico(GraficoGeral.Last());
            }

            reader.Close();
            res.Close();
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
