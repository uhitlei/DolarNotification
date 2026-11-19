using System.Globalization;
using CotacaoNotification;
using NUnit.Framework;

namespace CotacaoNotification.Tests
{
    [TestFixture]
    public class MonitorCotacaoTests
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        [Test]
        public void Atualizar_PrimeiraLeituraContaComoVariacao()
        {
            var monitor = new MonitorCotacao();
            Assert.That(monitor.Atualizar(3.87m, 4.05m), Is.True);
            Assert.That(monitor.UltimoComercial, Is.EqualTo(3.87m));
            Assert.That(monitor.UltimoTurismo, Is.EqualTo(4.05m));
        }

        [Test]
        public void Atualizar_MesmosValores_NaoNotifica()
        {
            var monitor = new MonitorCotacao();
            monitor.Atualizar(3.87m, 4.05m);
            Assert.That(monitor.Atualizar(3.87m, 4.05m), Is.False);
        }

        [Test]
        public void Atualizar_MudancaSoNoComercial_Notifica()
        {
            var monitor = new MonitorCotacao();
            monitor.Atualizar(3.87m, 4.05m);
            Assert.That(monitor.Atualizar(3.88m, 4.05m), Is.True);
            Assert.That(monitor.UltimoComercial, Is.EqualTo(3.88m));
        }

        [Test]
        public void Atualizar_MudancaSoNoTurismo_Notifica()
        {
            var monitor = new MonitorCotacao();
            monitor.Atualizar(3.87m, 4.05m);
            Assert.That(monitor.Atualizar(3.87m, 4.01m), Is.True);
            Assert.That(monitor.UltimoTurismo, Is.EqualTo(4.01m));
        }

        [Test]
        public void Atualizar_ValorQueVoltaAoAnterior_NotificaDeNovo()
        {
            var monitor = new MonitorCotacao();
            monitor.Atualizar(3.87m, 4.05m);
            monitor.Atualizar(3.90m, 4.05m);
            Assert.That(monitor.Atualizar(3.87m, 4.05m), Is.True);
        }

        [Test]
        public void MontarMensagem_MantemOTextoOriginal()
        {
            string msg = MonitorCotacao.MontarMensagem(3.87m, 3.9m, 4.05m, 4.1m, PtBr);
            Assert.That(msg, Is.EqualTo(
                "Dolar Comercial: Ultimo(R$3,87) Atual(R$3,90) \n"
                + "Dolar Turismo: Ultimo(R$4,05) Atual(R$4,10) \n"));
        }

        [Test]
        public void MontarMensagem_PrimeiraLeituraMostraZeroComoNoOriginal()
        {
            string msg = MonitorCotacao.MontarMensagem(0m, 3.87m, 0m, 4.05m, PtBr);
            StringAssert.StartsWith("Dolar Comercial: Ultimo(R$,00) Atual(R$3,87)", msg);
        }

        [Test]
        public void MontarDica_FormataComercialETurismo()
        {
            Assert.That(MonitorCotacao.MontarDica(3.87m, 4.05m, PtBr),
                Is.EqualTo("Comercial: R$3,87 \nTurismo: R$4,05"));
        }

        [Test]
        public void MontarDica_CabeNoLimiteDoNotifyIcon()
        {
            // NotifyIcon.Text aceita no maximo 63 caracteres no .NET Framework.
            Assert.That(MonitorCotacao.MontarDica(1234.567m, 1234.567m, PtBr).Length, Is.LessThanOrEqualTo(63));
        }

        [TestCase("5.8830", "5,883")]
        [TestCase("1234.5", "1.234,50")]
        [TestCase("-1.5", "(1,50)")]
        public void FormatoValor_ArredondaParaAteTresCasas(string valor, string esperado)
        {
            decimal d = decimal.Parse(valor, CultureInfo.InvariantCulture);
            Assert.That(d.ToString(MonitorCotacao.FormatoValor, PtBr), Is.EqualTo(esperado));
        }
    }
}
