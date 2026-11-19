using CotacaoNotification;
using NUnit.Framework;

namespace CotacaoNotification.Tests
{
    [TestFixture]
    public class CotacaoParserTests
    {
        private const string HtmlComercial =
            "<html><body><div class=\"cotacao\">"
            + "<span id=\"moeda\" class>US$ 1,00 = R$3,87</span>"
            + "</div></body></html>";

        private const string HtmlTurismo =
            "<html><body><span id=\"moeda\">US$ 1,00 = R$4,05</span></body></html>";

        [Test]
        public void TryExtrair_LeDolarComercialDoLayoutOriginal()
        {
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(HtmlComercial, CotacaoParser.PadraoComercial, out valor), Is.True);
            Assert.That(valor, Is.EqualTo(3.87m));
        }

        [Test]
        public void TryExtrair_LeDolarTurismoDoLayoutOriginal()
        {
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(HtmlTurismo, CotacaoParser.PadraoTurismo, out valor), Is.True);
            Assert.That(valor, Is.EqualTo(4.05m));
        }

        [Test]
        public void TryExtrair_PadroesNaoSeConfundem()
        {
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(HtmlTurismo, CotacaoParser.PadraoComercial, out valor), Is.False);
            Assert.That(CotacaoParser.TryExtrair(HtmlComercial, CotacaoParser.PadraoTurismo, out valor), Is.False);
        }

        [Test]
        public void TryExtrair_IgnoraMaiusculasEQuebrasDeLinha()
        {
            const string html = "<SPAN ID=\"moeda\">\n  R$ 3,99\n</SPAN>";
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(html, CotacaoParser.PadraoTurismo, out valor), Is.True);
            Assert.That(valor, Is.EqualTo(3.99m));
        }

        [Test]
        public void TryExtrair_ComVariasOcorrenciasValeAUltima()
        {
            const string html = "<span id=\"moeda\">R$3,10</span> ... <span id=\"moeda\">R$3,20</span>";
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(html, CotacaoParser.PadraoTurismo, out valor), Is.True);
            Assert.That(valor, Is.EqualTo(3.20m));
        }

        [Test]
        public void TryExtrair_LayoutAtualDoSiteNaoEReconhecido()
        {
            // Trecho do layout publicado em 2026: o <span id="moeda"> deixou de existir.
            const string html = "<a class=\"card\" href=\"/dolar-comercial/\"><small>Dólar comercial</small><b>R$ 5,8830</b></a>";
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(html, CotacaoParser.PadraoComercial, out valor), Is.False);
            Assert.That(valor, Is.EqualTo(0m));
        }

        [TestCase(null)]
        [TestCase("")]
        public void TryExtrair_HtmlVazio_RetornaFalse(string html)
        {
            decimal valor;
            Assert.That(CotacaoParser.TryExtrair(html, CotacaoParser.PadraoComercial, out valor), Is.False);
        }

        [TestCase("US$ 1,00 = R$3,87", "3.87")]
        [TestCase("R$ 3,87", "3.87")]          // espaco depois do simbolo (antes virava 3,8)
        [TestCase("R$10,50", "10.50")]         // dois digitos inteiros (antes virava 10,5)
        [TestCase("R$ 5,8830", "5.8830")]      // quatro casas decimais
        [TestCase("R$1.234,56", "1234.56")]    // separador de milhar brasileiro
        [TestCase("R$4", "4")]
        [TestCase("R$1,00 ou R$2,50", "2.50")] // vale o ultimo R$
        public void TryConverterValor_UsaFormatoBrasileiro(string texto, string esperado)
        {
            decimal valor;
            Assert.That(CotacaoParser.TryConverterValor(texto, out valor), Is.True);
            Assert.That(valor, Is.EqualTo(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("3,87")]            // sem "R$"
        [TestCase("R$")]
        [TestCase("R$ indisponivel")]
        public void TryConverterValor_TextoInvalido_RetornaFalse(string texto)
        {
            decimal valor;
            Assert.That(CotacaoParser.TryConverterValor(texto, out valor), Is.False);
            Assert.That(valor, Is.EqualTo(0m));
        }

        [Test]
        public void TryConverterValor_NaoDependeDaCulturaDaMaquina()
        {
            var original = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                decimal valor;
                Assert.That(CotacaoParser.TryConverterValor("R$3,87", out valor), Is.True);
                Assert.That(valor, Is.EqualTo(3.87m)); // em en-US o codigo original lia 387
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = original;
            }
        }
    }
}
