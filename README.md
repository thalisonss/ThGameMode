# ThGameMode

Aplicativo desktop em C# com Windows Forms, direcionado para Windows, criado sobre `.NET 8`.

## Resumo

O código-fonte presente neste repositório está, no momento, em um estado inicial. A aplicação apenas inicializa o ambiente do Windows Forms e abre uma janela padrão (`Form1`) sem componentes visíveis, eventos ou regras de negócio implementadas no código versionado.

Ao mesmo tempo, o diretório `bin/Debug/net8.0-windows7.0/` contém artefatos gerados que sugerem uma proposta maior para o projeto, com foco em:

- alternância de planos de energia do Windows;
- leitura e gravação de um arquivo de configuração JSON;
- monitoramento de serviços e/ou processos;
- geração de logs de execução.

Como essa lógica não aparece no código-fonte atual, este README documenta principalmente o que realmente existe no projeto hoje, sem assumir funcionalidades que não podem ser verificadas nos arquivos fonte.

## Tecnologias

- C#
- .NET 8
- Windows Forms
- Aplicação desktop para Windows

## Estrutura do projeto

- `Program.cs`: ponto de entrada da aplicação. Inicializa a configuração padrão do WinForms e abre a janela principal.
- `Form1.cs`: classe parcial da janela principal. No estado atual, apenas chama `InitializeComponent()`.
- `Form1.Designer.cs`: definição visual gerada pelo designer. A janela abre com tamanho `800x450` e título padrão `Form1`.
- `Form1.resx`: arquivo de recursos da tela.
- `ThGameMode.csproj`: configuração do projeto, com `OutputType` como `WinExe`, `TargetFramework` em `net8.0-windows` e `UseWindowsForms=true`.
- `ThGameMode.sln`: solução do Visual Studio.

## Comportamento atual

Quando executado, o programa:

1. inicializa a configuração padrão do Windows Forms;
2. cria a instância de `Form1`;
3. abre uma janela vazia.

Não há, no código versionado:

- botões;
- campos;
- leitura de configuração;
- troca de plano de energia;
- monitor em background;
- integração com serviços/processos;
- tratamento de eventos da interface.

## Evidências de uma proposta funcional maior

Os arquivos gerados em `bin/Debug/net8.0-windows7.0/` indicam que o projeto pode ter tido, ou pretender ter, uma funcionalidade mais completa.

### Arquivo de configuração encontrado

Existe um arquivo `ThGameModeConfig.json` com a seguinte estrutura lógica:

- `CheckInterval`: intervalo de verificação;
- `PowerPlanOpenApp`: GUID de um plano de energia;
- `PowerPlanClosedApp`: GUID de outro plano de energia;
- `ListServices`: lista de serviços monitorados.

### Logs encontrados

Também existe um arquivo de log com mensagens como:

- inicialização da interface;
- carregamento de configurações;
- início e parada de monitor;
- verificação de estado atual como `Economia`;
- carregamento de planos de energia.

Isso sugere fortemente que a ideia do projeto é automatizar a troca de modo de energia conforme o estado de aplicativos, processos ou serviços monitorados.

## Como executar

### Visual Studio

1. Abra o arquivo `ThGameMode.sln`.
2. Selecione o projeto `ThGameMode`.
3. Execute com `F5` ou `Ctrl+F5`.

### CLI do .NET

No diretório do projeto:

```powershell
dotnet build
dotnet run
```

## Requisitos

- Windows
- SDK do .NET 8 instalado
- Visual Studio 2022 ou CLI do .NET

## Estado do repositório

Hoje o repositório funciona como uma base inicial de um aplicativo WinForms. Ele compila, abre uma janela e serve como ponto de partida para evoluir a interface e a lógica de negócio.

Se a intenção for transformar este projeto no utilitário sugerido pelos artefatos gerados, os próximos passos naturais seriam:

- recriar ou versionar a lógica de monitoramento;
- implementar leitura e gravação do arquivo JSON de configuração;
- adicionar interface para escolher planos de energia;
- integrar chamadas ao Windows para alternar o plano ativo;
- versionar apenas código-fonte e remover artefatos gerados de `bin/` e `obj/`.

## Observação importante

Os diretórios `bin/` e `obj/` normalmente são artefatos de build e já aparecem no `.gitignore`. Mesmo assim, há arquivos gerados presentes no repositório atual. Eles ajudam a entender a intenção do projeto, mas não substituem o código-fonte correspondente.
