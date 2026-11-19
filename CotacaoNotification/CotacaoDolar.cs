using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace CotacaoNotification
{
    /// <summary>
    /// Lê o dólar comercial e o turismo da resposta JSON da AwesomeAPI
    /// (https://economia.awesomeapi.com.br). Não depende de Windows Forms nem de rede,
    /// para poder ser testada.
    /// </summary>
    public static class CotacaoParser
    {
        /// <summary>Uma única chamada traz o dólar comercial (USD-BRL) e o turismo (USD-BRLT).</summary>
        public const string UrlApi = "https://economia.awesomeapi.com.br/json/last/USD-BRL,USD-BRLT";

        /// <summary>Chave do dólar comercial na resposta.</summary>
        public const string ChaveComercial = "USDBRL";

        /// <summary>Chave do dólar turismo na resposta.</summary>
        public const string ChaveTurismo = "USDBRLT";

        /// <summary>
        /// Extrai o valor de compra ("bid") das duas cotações. Retorna false se a resposta
        /// não for um JSON válido ou se faltar alguma das cotações.
        /// </summary>
        public static bool TryExtrair(string json, out decimal comercial, out decimal turismo)
        {
            comercial = 0m;
            turismo = 0m;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            Dictionary<string, object> raiz;
            try
            {
                raiz = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return raiz != null
                && TryLerBid(raiz, ChaveComercial, out comercial)
                && TryLerBid(raiz, ChaveTurismo, out turismo);
        }

        private static bool TryLerBid(Dictionary<string, object> raiz, string chave, out decimal valor)
        {
            valor = 0m;
            object item;
            if (!raiz.TryGetValue(chave, out item))
                return false;

            var cotacao = item as Dictionary<string, object>;
            object bid;
            if (cotacao == null || !cotacao.TryGetValue("bid", out bid) || bid == null)
                return false;

            // A API envia os números como texto com ponto decimal ("5.883"),
            // independentemente da cultura da máquina.
            string texto = Convert.ToString(bid, CultureInfo.InvariantCulture);
            return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out valor)
                && valor > 0m;
        }
    }

    /// <summary>
    /// Consulta a API. Falhas de rede viram "sem valor" em vez de exceção.
    /// </summary>
    public static class FonteCotacao
    {
        private const int TimeoutMs = 10000;

        public static bool TryLer(out decimal comercial, out decimal turismo)
        {
            comercial = 0m;
            turismo = 0m;
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var req = (HttpWebRequest)WebRequest.Create(CotacaoParser.UrlApi);
                req.Timeout = TimeoutMs;
                req.ReadWriteTimeout = TimeoutMs;
                req.UserAgent = "DolarNotification";
                req.Accept = "application/json";

                using (WebResponse res = req.GetResponse())
                using (var reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                {
                    return CotacaoParser.TryExtrair(reader.ReadToEnd(), out comercial, out turismo);
                }
            }
            catch (WebException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Guarda a última cotação conhecida e decide se houve variação.
    /// </summary>
    public class MonitorCotacao
    {
        /// <summary>Formato usado nas mensagens desde a versão 1.0.</summary>
        public const string FormatoValor = "#,#.00#;(#,#.00#)";

        public decimal UltimoComercial { get; private set; }
        public decimal UltimoTurismo { get; private set; }

        /// <summary>
        /// Registra uma nova leitura. Retorna true quando algum dos dois valores mudou
        /// (inclusive na primeira leitura, quando os anteriores são zero).
        /// </summary>
        public bool Atualizar(decimal comercial, decimal turismo)
        {
            if (comercial == UltimoComercial && turismo == UltimoTurismo)
                return false;

            UltimoComercial = comercial;
            UltimoTurismo = turismo;
            return true;
        }

        public static string MontarMensagem(decimal comercialAnterior, decimal comercialAtual,
            decimal turismoAnterior, decimal turismoAtual, IFormatProvider cultura)
        {
            return "Dolar Comercial: Ultimo(R$" + comercialAnterior.ToString(FormatoValor, cultura) + ") Atual(R$" + comercialAtual.ToString(FormatoValor, cultura) + ") \n"
                 + "Dolar Turismo: Ultimo(R$" + turismoAnterior.ToString(FormatoValor, cultura) + ") Atual(R$" + turismoAtual.ToString(FormatoValor, cultura) + ") \n";
        }

        public static string MontarDica(decimal comercial, decimal turismo, IFormatProvider cultura)
        {
            return "Comercial: R$" + comercial.ToString(FormatoValor, cultura) + " \n"
                 + "Turismo: R$" + turismo.ToString(FormatoValor, cultura);
        }
    }
}
