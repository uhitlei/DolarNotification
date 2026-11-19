using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace CotacaoNotification
{
    /// <summary>
    /// Extrai o valor do dólar do HTML da página de cotação.
    /// Não depende de Windows Forms nem de rede, para poder ser testada.
    /// </summary>
    public static class CotacaoParser
    {
        /// <summary>Página do dólar comercial.</summary>
        public const string UrlComercial = "https://www.dolarhoje.net.br";

        /// <summary>Página do dólar turismo.</summary>
        public const string UrlTurismo = "https://www.dolarhoje.net.br/dolarturismo.php";

        /// <summary>Trecho do HTML que continha o dólar comercial (layout de 2015).</summary>
        public const string PadraoComercial = @"<span id=""moeda"" class>(.*?)</span>";

        /// <summary>Trecho do HTML que continha o dólar turismo (layout de 2015).</summary>
        public const string PadraoTurismo = @"<span id=""moeda"">(.*?)</span>";

        /// <summary>A página publica os valores no formato brasileiro (ex.: "3,87").</summary>
        public static readonly CultureInfo CulturaFonte = CultureInfo.GetCultureInfo("pt-BR");

        private static readonly Regex Numero = new Regex(
            @"^\s*(\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d+(?:,\d+)?)",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// Procura o padrão no HTML e converte o valor que vem depois do último "R$".
        /// Se houver mais de uma ocorrência, vale a última (como no código original).
        /// </summary>
        public static bool TryExtrair(string html, string padrao, out decimal valor)
        {
            valor = 0m;
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(padrao))
                return false;

            bool encontrou = false;
            MatchCollection matches = Regex.Matches(html, padrao, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                decimal atual;
                if (match.Success && match.Groups.Count > 1 && TryConverterValor(match.Groups[1].Value, out atual))
                {
                    valor = atual;
                    encontrou = true;
                }
            }
            return encontrou;
        }

        /// <summary>
        /// Converte um texto como "US$ 1,00 = R$ 3,87" no número que vem depois do último "R$".
        /// </summary>
        public static bool TryConverterValor(string texto, out decimal valor)
        {
            valor = 0m;
            if (string.IsNullOrEmpty(texto))
                return false;

            int posicao = texto.LastIndexOf("R$", StringComparison.Ordinal);
            if (posicao < 0)
                return false;

            Match m = Numero.Match(texto.Substring(posicao + 2));
            if (!m.Success)
                return false;

            return decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CulturaFonte, out valor);
        }
    }

    /// <summary>
    /// Baixa a página e extrai o valor. Falhas de rede viram "sem valor" em vez de exceção.
    /// </summary>
    public static class FonteCotacao
    {
        public static bool TryLer(string url, string padrao, out decimal valor)
        {
            valor = 0m;
            try
            {
                WebRequest req = WebRequest.Create(url);
                using (WebResponse res = req.GetResponse())
                using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                {
                    return CotacaoParser.TryExtrair(reader.ReadToEnd(), padrao, out valor);
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
