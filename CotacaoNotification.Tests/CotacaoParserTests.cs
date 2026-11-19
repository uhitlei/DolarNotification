using System.Globalization;
using System.Threading;
using CotacaoNotification;
using NUnit.Framework;

namespace CotacaoNotification.Tests
{
    [TestFixture]
    public class CotacaoParserTests
    {
        // Resposta real da AwesomeAPI (18/11/2026), com os dois pares pedidos.
        private const string RespostaReal =
            "{\"USDBRL\":{\"code\":\"USD\",\"codein\":\"BRL\",\"name\":\"Dólar Americano/Real Brasileiro\","
            + "\"high\":\"5.8893\",\"low\":\"5.778\",\"varBid\":\"0.105\",\"pctChange\":\"1.81724\","
            + "\"bid\":\"5.883\",\"ask\":\"5.8835\",\"timestamp\":\"1795039210\",\"create_date\":\"2026-11-18 19:00:10\"},"
            + "\"USDBRLT\":{\"code\":\"USD\",\"codein\":\"BRLT\",\"name\":\"Dólar Americano/Real Brasileiro Turismo\","
            + "\"high\":\"5.84473\",\"low\":\"5.73417\",\"varBid\":\"0.1042\",\"pctChange\":\"1.817177\","
            + "\"bid\":\"5.83837\",\"ask\":\"6.29524\",\"timestamp\":\"1795042202\",\"create_date\":\"2026-11-18 19:50:02\"}}";

        [Test]
        public void TryExtrair_RespostaReal_LeComercialETurismo()
        {
            decimal comercial, turismo;

            Assert.That(CotacaoParser.TryExtrair(RespostaReal, out comercial, out turismo), Is.True);
            Assert.That(comercial, Is.EqualTo(5.883m));
            Assert.That(turismo, Is.EqualTo(5.83837m));
        }

        [Test]
        public void TryExtrair_UsaOValorDeCompra()
        {
            const string json = "{\"USDBRL\":{\"bid\":\"5.10\",\"ask\":\"5.20\"},\"USDBRLT\":{\"bid\":\"5.30\",\"ask\":\"5.90\"}}";
            decimal comercial, turismo;

            CotacaoParser.TryExtrair(json, out comercial, out turismo);

            Assert.That(comercial, Is.EqualTo(5.10m));
            Assert.That(turismo, Is.EqualTo(5.30m));
        }

        [Test]
        public void TryExtrair_AceitaNumeroSemAspas()
        {
            const string json = "{\"USDBRL\":{\"bid\":5.5},\"USDBRLT\":{\"bid\":5.75}}";
            decimal comercial, turismo;

            Assert.That(CotacaoParser.TryExtrair(json, out comercial, out turismo), Is.True);
            Assert.That(comercial, Is.EqualTo(5.5m));
            Assert.That(turismo, Is.EqualTo(5.75m));
        }

        [Test]
        public void TryExtrair_NaoDependeDaCulturaDaMaquina()
        {
            var original = Thread.CurrentThread.CurrentCulture;
            try
            {
                foreach (var nome in new[] { "pt-BR", "en-US", "de-DE" })
                {
                    Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(nome);
                    decimal comercial, turismo;

                    Assert.That(CotacaoParser.TryExtrair(RespostaReal, out comercial, out turismo), Is.True, nome);
                    Assert.That(comercial, Is.EqualTo(5.883m), nome);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [TestCase("{\"USDBRL\":{\"bid\":\"5.88\"}}", TestName = "TryExtrair_SemTurismo_RetornaFalse")]
        [TestCase("{\"USDBRLT\":{\"bid\":\"5.88\"}}", TestName = "TryExtrair_SemComercial_RetornaFalse")]
        [TestCase("{\"USDBRL\":{\"ask\":\"5.88\"},\"USDBRLT\":{\"bid\":\"5.90\"}}", TestName = "TryExtrair_SemBid_RetornaFalse")]
        [TestCase("{\"USDBRL\":{\"bid\":\"abc\"},\"USDBRLT\":{\"bid\":\"5.90\"}}", TestName = "TryExtrair_BidInvalido_RetornaFalse")]
        [TestCase("{\"USDBRL\":{\"bid\":\"0\"},\"USDBRLT\":{\"bid\":\"5.90\"}}", TestName = "TryExtrair_BidZero_RetornaFalse")]
        [TestCase("{\"USDBRL\":\"5.88\",\"USDBRLT\":\"5.90\"}", TestName = "TryExtrair_FormatoInesperado_RetornaFalse")]
        [TestCase("[1,2,3]", TestName = "TryExtrair_JsonNaoEObjeto_RetornaFalse")]
        public void TryExtrair_RespostaIncompleta_RetornaFalse(string json)
        {
            decimal comercial, turismo;

            Assert.That(CotacaoParser.TryExtrair(json, out comercial, out turismo), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("<html>erro 503</html>")]
        [TestCase("{\"status\":429,\"message\":\"limite excedido\"}")]
        [TestCase("{ json quebrado")]
        public void TryExtrair_RespostaInvalida_RetornaFalseSemExcecao(string json)
        {
            decimal comercial, turismo;

            Assert.That(CotacaoParser.TryExtrair(json, out comercial, out turismo), Is.False);
            Assert.That(comercial, Is.EqualTo(0m));
            Assert.That(turismo, Is.EqualTo(0m));
        }

        [Test]
        public void UrlApi_PedeComercialETurismoNumaChamada()
        {
            StringAssert.StartsWith("https://economia.awesomeapi.com.br/", CotacaoParser.UrlApi);
            StringAssert.Contains("USD-BRL,USD-BRLT", CotacaoParser.UrlApi);
        }
    }
}
