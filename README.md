<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/logo-dark.png">
    <img src="docs/logo.png" alt="DolarNotification" width="460">
  </picture>
</p>

<p align="center">
  Ícone na bandeja do Windows que avisa, com um balão de notificação, quando a cotação do dólar comercial ou turismo muda.
</p>

<p align="center">
  <a href="https://github.com/uhitlei/DolarNotification/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/uhitlei/DolarNotification/actions/workflows/ci.yml/badge.svg"></a>
  <img alt=".NET Framework 4.8" src="https://img.shields.io/badge/.NET%20Framework-4.8-512BD4">
  <img alt="Windows Forms" src="https://img.shields.io/badge/UI-Windows%20Forms-0078D4">
  <img alt="Testes NUnit" src="https://img.shields.io/badge/testes-NUnit%203-2E8B57">
  <a href="LICENSE"><img alt="Licença MIT" src="https://img.shields.io/badge/licen%C3%A7a-MIT-blue"></a>
</p>

---

## Sobre

Projeto pessoal de dezembro de 2015, feito no Visual Studio 2013. A ideia era simples: acompanhar o dólar sem precisar abrir o navegador. O programa fica na área de notificação do Windows, consulta a cotação periodicamente e só chama a atenção quando o valor muda.

## Como funciona

1. A cada 30 segundos, um `System.Timers.Timer` consulta a [AwesomeAPI](https://docs.awesomeapi.com.br/api-de-moedas), uma API pública e gratuita de cotações, sem chave de acesso. Uma única chamada traz o dólar comercial (`USD-BRL`) e o turismo (`USD-BRLT`):
   ```
   GET https://economia.awesomeapi.com.br/json/last/USD-BRL,USD-BRLT
   ```
2. De cada cotação é usado o valor de compra (`bid`), que a API envia como texto com ponto decimal (`"5.883"`).
3. Se as duas cotações foram lidas e alguma delas é diferente da última conhecida, o programa:
   - mostra um balão **"Alteração no Dolar!"** com o valor anterior e o atual de cada cotação;
   - atualiza a dica do ícone (`Comercial: R$… / Turismo: R$…`);
   - acrescenta um ponto em cada linha do gráfico (séries *Comercial* e *Turismo*).

A leitura da resposta e a decisão de notificar ficam em [`CotacaoDolar.cs`](CotacaoNotification/CotacaoDolar.cs), separadas da interface:

| Classe | Papel |
|---|---|
| `CotacaoParser` | Lê o JSON da API e extrai as duas cotações, sem depender da cultura da máquina. Resposta incompleta ou inválida vira "sem valor". |
| `FonteCotacao` | Faz a chamada HTTPS com tempo limite de 10 segundos; erro de rede vira "sem valor" em vez de exceção. |
| `MonitorCotacao` | Guarda a última cotação, informa se houve variação e monta os textos do balão e da dica. |

## Como usar

1. Baixe o `DolarNotification-win.zip` da [página de releases](https://github.com/uhitlei/DolarNotification/releases) e extraia em qualquer pasta, ou compile a partir do código (veja abaixo).
2. Execute `CotacaoNotification.exe`. O ícone aparece na área de notificação, ao lado do relógio.
3. Pelo ícone:
   - **duplo clique** ou **Open** no menu de contexto abre a janela com o gráfico;
   - **Sair** encerra o programa.
4. Na janela, **Arquivo > Esconder** (`Ctrl+E`) volta a deixá-la só na bandeja.

O histórico do gráfico fica apenas em memória e começa do zero a cada execução.

## Estrutura

```
Notification.sln
├── CotacaoNotification/         Aplicativo da bandeja (versão final, com gráfico)
│   └── CotacaoDolar.cs          Leitura da cotação e detecção de variação
├── NotificationForm/            Primeira versão do aplicativo, sem gráfico (usa o mesmo CotacaoDolar.cs)
├── TesteConsole/                Console de experimentos com outras fontes de cotação
│   └── Service References/      Cliente SOAP (WCF) do web service SGS do Banco Central
└── CotacaoNotification.Tests/   Testes unitários (NUnit)
```

O `TesteConsole` foi usado para avaliar alternativas ao site: ele chama `getUltimosValoresSerieVO` no web service SGS do Banco Central (`https://www3.bcb.gov.br/wssgs/services/FachadaWSSGS`) e faz uma requisição HTTP a uma API pública de cotações. Nenhuma das duas fontes foi adotada pelo aplicativo da bandeja.

## Compilar e testar

Com Visual Studio 2019 ou superior no Windows, abra `Notification.sln`, restaure os pacotes NuGet e compile. Os testes aparecem no Test Explorer.

Pela linha de comando:

```powershell
nuget restore Notification.sln
msbuild Notification.sln /p:Configuration=Release
packages\NUnit.ConsoleRunner.3.11.1\tools\nunit3-console.exe CotacaoNotification.Tests\bin\Release\CotacaoNotification.Tests.dll
```

Em macOS ou Linux, o build e os testes rodam com Mono via Docker (o aplicativo da bandeja não):

```bash
docker run --rm -v "$PWD":/src -w /src mono:6.12 bash -c '
  nuget restore Notification.sln &&
  msbuild Notification.sln /p:Configuration=Release /p:DebugSymbols=false /p:DebugType=none /v:m &&
  mono packages/NUnit.ConsoleRunner.3.11.1/tools/nunit3-console.exe \
    CotacaoNotification.Tests/bin/Release/CotacaoNotification.Tests.dll'
```

Os quatro projetos, inclusive o cliente WCF do `TesteConsole`, compilam no Mono 6.12.

## CI/CD

O workflow [`ci.yml`](.github/workflows/ci.yml) roda no GitHub Actions a cada push e pull request:

1. Restaura os pacotes e compila a solução em um runner Windows.
2. Executa os testes e guarda o `TestResult.xml` como artefato.
3. Empacota `CotacaoNotification.exe` e seu `.config` em `DolarNotification-win.zip`.
4. Quando o push é de uma tag `v*` (ex.: `v1.1.0`), publica uma release no GitHub com o zip anexado.

Para lançar uma versão:

```bash
git tag v1.1.0
git push origin v1.1.0
```

## Limitações conhecidas

- Depende da disponibilidade da AwesomeAPI, um serviço gratuito de terceiros. Se ela sair do ar ou limitar as requisições, o programa simplesmente deixa de avisar até a API voltar.
- O intervalo de 30 segundos está fixo no código.
- Só roda no Windows (Windows Forms, `NotifyIcon` e o controle de gráfico `System.Windows.Forms.DataVisualization`).
- O `TesteConsole` é um rascunho: chama serviços externos sem tratamento de erro e não tem testes.

## Melhorias de 2026

Ao revisitar o projeto, foram feitas estas correções e adições:

- **Fonte de dados nova:** a versão de 2015 lia o HTML do site Dólar Hoje, que mudou de layout e quebrou a leitura (o programa nunca mais avisava nada). Agora a cotação vem da AwesomeAPI, numa única chamada para as duas cotações, e o programa voltou a funcionar.
- A leitura da cotação e a detecção de variação saíram dos formulários para `CotacaoDolar.cs`, compartilhado pelos dois aplicativos e coberto por testes. Os textos do balão e da dica continuam os mesmos.
- Na leitura antiga, o valor era convertido com a cultura da máquina (em um Windows em inglês, `3,87` virava `387`) e só eram lidos 4 caracteres depois do `R$` (`R$10,50` virava `10,5`). A leitura nova não depende da cultura e lê o número inteiro.
- Uma falha de rede ou de leitura gerava um balão com `Atual(R$.00)`. Agora o aviso só aparece quando as duas cotações foram lidas.
- As respostas HTTP não eram fechadas em caso de erro, e a primeira nunca era; agora são descartadas com `using`.
- O timer roda em outra thread e alterava o ícone e o gráfico diretamente; as atualizações agora passam pela thread da interface.
- O timer continuava disparando depois de **Sair**; agora é descartado junto com a janela.
- A consulta usa HTTPS com TLS 1.2 e tempo limite.
- Projetos migrados de .NET Framework 4.5/4.5.1 para 4.8.
- Projeto de testes, pipeline de CI/CD, licença e esta documentação.

## Licença

Distribuído sob a [licença MIT](LICENSE).
