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

1. A cada 30 segundos, um `System.Timers.Timer` baixa duas páginas do site Dólar Hoje: a principal (`dolarhoje.net.br`, dólar comercial) e `dolarturismo.php` (dólar turismo).
2. Uma expressão regular procura o elemento `<span id="moeda">` de cada página e lê o número que vem depois do último `R$`, no formato brasileiro (`3,87`).
3. Se as duas cotações foram lidas e alguma delas é diferente da última conhecida, o programa:
   - mostra um balão **"Alteração no Dolar!"** com o valor anterior e o atual de cada cotação;
   - atualiza a dica do ícone (`Comercial: R$… / Turismo: R$…`);
   - acrescenta um ponto em cada linha do gráfico (séries *Comercial* e *Turismo*).

A leitura do HTML e a decisão de notificar ficam em [`CotacaoDolar.cs`](CotacaoNotification/CotacaoDolar.cs), separadas da interface:

| Classe | Papel |
|---|---|
| `CotacaoParser` | Extrai o valor do HTML e converte o texto `R$ 3,87` em `decimal`, sempre com a cultura pt-BR. |
| `FonteCotacao` | Baixa a página; erro de rede vira "sem valor" em vez de exceção. |
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

- **A fonte de dados não funciona mais como em 2015.** O site Dólar Hoje continua no ar, mas mudou de layout: o elemento `<span id="moeda">` que o programa procura não existe mais. Com isso nenhuma cotação é encontrada e nenhum aviso é exibido. Fazer o aplicativo voltar a funcionar exige trocar a fonte, de preferência por uma API pública em vez de leitura de HTML.
- A extração depende da estrutura exata da página, então qualquer mudança no site quebra a leitura.
- O intervalo de 30 segundos está fixo no código.
- Só roda no Windows (Windows Forms, `NotifyIcon` e o controle de gráfico `System.Windows.Forms.DataVisualization`).
- O `TesteConsole` é um rascunho: chama serviços externos sem tratamento de erro e não tem testes.

## Melhorias de 2026

Ao revisitar o projeto, foram feitas estas correções e adições:

- A leitura do HTML e a detecção de variação saíram dos formulários para `CotacaoDolar.cs`, compartilhado pelos dois aplicativos e coberto por testes. Os textos do balão e da dica continuam os mesmos.
- O valor era convertido com a cultura da máquina: em um Windows em inglês, `3,87` virava `387`. Agora a conversão usa sempre pt-BR.
- Só eram lidos 4 caracteres depois do `R$`: `R$ 3,87` virava `3,8` e `R$10,50` virava `10,5`. Agora o número é lido inteiro.
- Uma falha de rede ou de leitura gerava um balão com `Atual(R$.00)`. Agora o aviso só aparece quando as duas cotações foram lidas.
- As respostas HTTP não eram fechadas em caso de erro, e a primeira nunca era; agora são descartadas com `using`.
- O timer roda em outra thread e alterava o ícone e o gráfico diretamente; as atualizações agora passam pela thread da interface.
- O timer continuava disparando depois de **Sair**; agora é descartado junto com a janela.
- Endereços do site atualizados para `https`.
- Projetos migrados de .NET Framework 4.5/4.5.1 para 4.8.
- Projeto de testes, pipeline de CI/CD, licença e esta documentação.

## Licença

Distribuído sob a [licença MIT](LICENSE).
