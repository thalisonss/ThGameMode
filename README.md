# ThGameMode

Aplicativo de bandeja (**tray app**) para Windows que alterna automaticamente o plano de energia do sistema conforme jogos, aplicativos ou serviços monitorados estão em execução. O projeto também embute um **dashboard web** com métricas do PC em tempo real: CPU, GPU, memória, disco, temperaturas, clima e consumo de energia.

> **Versão atual:** `1.0.4`

---

## Índice

- [Visão geral](#visão-geral)
- [Funcionalidades](#funcionalidades)
- [Como funciona](#como-funciona)
- [Requisitos](#requisitos)
- [Compilação e execução](#compilação-e-execução)
- [Publicação](#publicação)
- [Como usar](#como-usar)
- [Arquivo de configuração](#arquivo-de-configuração)
- [Dashboard web e API](#dashboard-web-e-api)
- [Consumo de energia](#consumo-de-energia)
- [Estrutura do projeto](#estrutura-do-projeto)
- [Logs e atualizações](#logs-e-atualizações)
- [Limitações e segurança](#limitações-e-segurança)
- [Licença](#licença)

---

## Visão geral

O ThGameMode foi criado para eliminar a troca manual de plano de energia antes e depois de jogar. Ele fica na bandeja do Windows, verifica periodicamente se algum dos itens monitorados está em execução e:

- aplica o **plano de energia de aplicativo aberto** quando encontra um jogo/app/serviço ativo;
- aplica o **plano de energia de aplicativo fechado** quando nada monitorado está rodando;
- restaura o plano que estava ativo antes da intervenção quando a detecção é desativada ou o app é encerrado.

Além disso, o app sobe um servidor HTTP próprio na porta `5000` e serve um dashboard acessível pelo navegador, inclusive por outros dispositivos da rede local.

---

## Funcionalidades

### Troca automática de plano de energia
- Monitora serviços do Windows e processos por nome.
- Intervalo de verificação configurável (padrão: 60 segundos).
- Aplica o plano escolhido para "aplicativo aberto" e "aplicativo fechado".
- Restaura o plano original ao parar a detecção.
- Reavalia o estado imediatamente ao adicionar/remover itens ou salvar a configuração.

### Fechamento e reabertura de aplicativos
- Permite escolher aplicativos que devem ser fechados ao entrar no modo de alto desempenho.
- Os mesmos aplicativos são reabertos quando o monitor volta para o modo de economia, quando a detecção é desativada ou quando o app é encerrado.
- A lista é configurada na seção **Open and Close Application** da janela principal, com busca, refresh e opção de adicionar um `.exe` manualmente.
- Os caminhos dos executáveis são persistidos no JSON para permitir a reabertura depois.
- Só entram na reabertura os aplicativos que o ThGameMode conseguiu fechar; apps que já estavam fechados não são abertos sem necessidade.

### Bandeja do Windows
- Ícone muda conforme o estado: economia, alto desempenho ou desativado.
- Tooltip com modo atual, tempo ativo, endereço do dashboard e horário da última mudança.
- Fechar a janela apenas esconde o app; para encerrar de verdade use **Sair** no menu da bandeja.
- Opção **Iniciar com Windows**, que registra o app no `Run` do usuário com o argumento `--minimized`.
- Menu para ligar/desligar o servidor web do dashboard.

### Dashboard web embutido
- Servido pelo próprio executável, sem depender de Python, Flask ou outro processo.
- Atualização automática a cada 2 segundos.
- Interface escura e responsiva.
- Acessível em `http://localhost:5000` ou `http://<IP-do-PC>:5000`.
- Card de consumo de energia com detalhamento por componente.

### Monitoramento de hardware
- CPU: utilização, núcleos, frequência e temperatura.
- GPU: nome, utilização e temperatura.
- Memória RAM: percentual, usado, total e temperatura.
- Disco: percentual, usado, livre, total e temperatura (unidades fixas somadas).
- Placa-mãe: sensores de Sistema, VRM, PCH e Aux/Soquete.
- Uptime do sistema.
- IP local.
- Clima de São Paulo via [Open-Meteo](https://open-meteo.com/).

### Energia
- Consumo estimado de cada componente em watts.
- Soma atual de todos os componentes.
- Estimativa de consumo na tomada considerando eficiência da fonte.
- Energia da sessão e energia acumulada em kWh, persistida entre execuções.

### Recursos auxiliares
- Logs diários em arquivo, com retenção de 7 dias.
- Verificação de novas versões nas releases do GitHub.
- Notificação na bandeja e atalho para abrir a página da release.

---

## Como funciona

### Fluxo de inicialização

1. `Program.Main` configura o Windows Forms.
2. O `WebDashboardServer` é criado e iniciado na porta `5000`.
3. A janela principal (`frmConfig`) é aberta (ou escondida na bandeja, com `--minimized`).
4. `frmConfig` carrega o `ThGameModeConfig.json`, popula os planos de energia e a lista de serviços/processos.
5. O monitor de planos de energia é iniciado automaticamente.

### Monitor de planos de energia

- A cada `CheckInterval` segundos, o app percorre `ListServices`.
- Cada item é testado primeiro como **serviço** e, se não existir, como **processo**.
- Se qualquer item monitorado estiver ativo, o app entra no modo de alto desempenho.
- Quando o último item monitorado é encerrado, o app aplica o plano de economia.
- Ao parar a detecção, o plano que estava ativo antes da primeira troca é restaurado.

> Na primeira avaliação sem nenhum item em execução, o plano atual é mantido. O plano de economia é aplicado na transição de "algo em execução" para "nada em execução".

### Fechamento e reabertura de aplicativos

- A lista `ListServicesOpenClose` define os aplicativos que devem ser fechados no modo de alto desempenho.
- Ao entrar no modo de alto desempenho, o app tenta fechar cada aplicativo da lista:
  1. procura processos com o nome configurado;
  2. tenta fechar pela janela principal (`CloseMainWindow`);
  3. se não responder em 5 segundos, encerra o processo (`Kill`).
- Ao voltar para o modo de economia, ao desativar a detecção ou ao sair, o app reabre somente os aplicativos que ele próprio fechou.
- O caminho do executável é descoberto no momento do fechamento e salvo em `OpenCloseExecutablePaths`. Também é possível informar o `.exe` manualmente pela interface.
- Se o caminho não existir mais ou não estiver salvo, a reabertura daquele aplicativo falha e fica registrada no log.

### Dashboard web

- `SystemMonitorService` coleta as métricas em um timer de 2 segundos.
- `DashboardHtml` gera o HTML do dashboard.
- `wwwroot/js/app.js` consulta `/api/stats` a cada 2 segundos e atualiza a tela.
- `PowerConsumptionService` estima o consumo e integra a energia ao longo do tempo.

---

## Requisitos

### Para executar
- Windows 10 ou 11 (x64).
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).
- Porta `5000` livre.
- Permissão de administrador/UAC na primeira vez que o dashboard precisar registrar a URL na rede local (o app tenta fazer isso automaticamente).

### Para compilar
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior.
- Visual Studio 2022 com a carga de trabalho **Desenvolvimento para desktop com .NET** (opcional).

### Tecnologias

| Item | Valor |
|---|---|
| Linguagem | C# |
| Framework | .NET 8 |
| Target Framework | `net8.0-windows7.0` |
| Interface | Windows Forms |
| Servidor HTTP | `HttpListener` |
| WMI | `System.Management` |
| Serviços do Windows | `System.ServiceProcess.ServiceController` |
| Versão | `1.0.4` |

---

## Compilação e execução

### Visual Studio

1. Abra `ThGameMode.sln`.
2. Selecione o projeto `ThGameMode`.
3. Execute com `F5` ou `Ctrl+F5`.

### CLI do .NET

```powershell
dotnet restore
dotnet build
dotnet run --project ThGameMode.csproj
```

Para abrir direto na bandeja:

```powershell
dotnet run --project ThGameMode.csproj -- --minimized
```

Também é possível gerar a build de release:

```powershell
dotnet build -c Release
```

---

## Publicação

O projeto é publicado como aplicativo **framework-dependent**, ou seja, o computador de destino precisa do .NET 8 Desktop Runtime instalado.

```powershell
dotnet publish ThGameMode.csproj -c Release -o "D:\Arquivos de Programas\ThGameMode"
```

> Feche o ThGameMode antes de publicar na mesma pasta, pois o executável em execução bloqueia a cópia dos arquivos.

A pasta publicada contém o executável, as DLLs, os ícones, o `wwwroot` e o `ThGameModeConfig.json` (quando já existir). A versão do aplicativo é definida em `ThGameMode.csproj`:

```xml
<Version>1.0.4</Version>
<AssemblyVersion>1.0.4</AssemblyVersion>
<FileVersion>1.0.4</FileVersion>
```

---

## Como usar

### Configuração inicial

1. Abra o aplicativo. Ele inicia na bandeja do Windows.
2. Dê duplo clique no ícone (ou use **Abrir configurações**) para abrir a janela.
3. Escolha o **plano de energia com aplicativo aberto** e o **plano de energia com aplicativo fechado**.
4. Defina o **intervalo de verificação** em segundos.
5. Na lista da esquerda, pesquise e adicione os jogos, aplicativos ou serviços que devem ativar o modo de alto desempenho.
6. Na seção **Open and Close Application**, adicione os aplicativos que devem ser fechados no modo de alto desempenho e reabertos na economia. Use **Refresh** para atualizar a lista de processos em execução ou **Adicionar .exe** para escolher um executável manualmente.
7. Clique em **Save**.

A lista de planos é obtida com `powercfg /list`. Se necessário, consulte os GUIDs manualmente:

```powershell
powercfg /list
powercfg /getactivescheme
```

### Menu da bandeja

| Opção | Ação |
|---|---|
| Detecção ativada | Item com marca de seleção: liga/desliga o monitor de serviços/processos. Ao desligar, o plano original é restaurado e os aplicativos fechados são reabertos. |
| Abrir configurações | Mostra a janela principal. |
| Verificar atualizações | Consulta a última release no GitHub. |
| Baixar nova versão | Abre a página da release (habilitado quando há atualização). |
| Iniciar com Windows | Liga/desliga a inicialização automática. |
| Abrir Dashboard | Abre o dashboard no navegador padrão. |
| Ligar/Desligar Servidor Web | Controla o servidor HTTP embutido. |
| Sair | Encerra o app, o monitor e o servidor. |

O tooltip do ícone mostra o modo atual, o tempo ativo, o endereço do dashboard e a última mudança de estado. O Windows limita o tooltip a aproximadamente 63 caracteres.

---

## Arquivo de configuração

O arquivo fica ao lado do executável:

```text
ThGameModeConfig.json
```

Exemplo:

```json
{
  "CheckInterval": 60,
  "PowerPlanOpenApp": "3ff9831b-6f80-4830-8178-736cd4229e7b",
  "PowerPlanClosedApp": "634a9dba-54c9-44b6-8909-2ecec1245352",
  "ListServices": [
    "RDR2",
    "Sam2",
    "PEAK",
    "ProtonVPNService"
  ],
  "ListServicesOpenClose": [
    "MEGAsync",
    "IDMan",
    "ProtonVPN.Client"
  ],
  "ManualExecutablePaths": {},
  "OpenCloseExecutablePaths": {
    "IDMan": "C:\\Program Files (x86)\\Internet Download Manager\\IDMan.exe",
    "MEGAsync": "C:\\Users\\usuario\\AppData\\Local\\MEGAsync\\MEGAsync.exe",
    "ProtonVPN.Client": "C:\\Program Files\\Proton\\VPN\\v4.4.1\\ProtonVPN.Client.exe"
  }
}
```

| Campo | Descrição |
|---|---|
| `CheckInterval` | Intervalo entre verificações, em segundos. O mínimo efetivo usado pelo monitor é 1 segundo. |
| `PowerPlanOpenApp` | GUID do plano aplicado quando algum item monitorado está em execução. |
| `PowerPlanClosedApp` | GUID do plano aplicado quando nada monitorado está em execução. |
| `ListServices` | Serviços e/ou processos monitorados. Aceita `NomeDoProcesso` ou `NomeDoProcesso\|trecho da janela` (formato suportado pelo monitor). |
| `ListServicesOpenClose` | Aplicativos que devem ser fechados no modo de alto desempenho e reabertos ao voltar para economia. |
| `ManualExecutablePaths` | Caminhos informados manualmente para itens monitorados. Campo mantido para compatibilidade. |
| `OpenCloseExecutablePaths` | Caminhos dos executáveis usados para reabrir os aplicativos de `ListServicesOpenClose`. |

Observações:

- O arquivo é criado/atualizado ao salvar a configuração ou ao adicionar/remover itens.
- Alterar o intervalo e clicar em **Save** reinicia o monitor para aplicar a mudança imediatamente.
- Arquivos de configuração de versões antigas podem conter campos extras; eles são ignorados na leitura.
- Se nenhum plano for encontrado, a interface mostra a opção **Padrão do sistema** (GUID vazio), que não executa troca de plano.

---

## Dashboard web e API

O servidor é iniciado junto com o aplicativo. Por padrão ele escuta em todas as interfaces:

```text
http://localhost:5000
http://<IP-do-PC>:5000
```

Se o registro de URL (`urlacl`) não existir, o app solicita elevação via UAC para executar:

```powershell
netsh http add urlacl url=http://*:5000/ user="<usuário>"
```

Caso o registro falhe ou o usuário cancele, o servidor tenta funcionar apenas em `http://localhost:5000`.

### Endpoints

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Página HTML do dashboard. |
| `GET` | `/api/stats` | Métricas atuais em JSON. |
| `GET` | `/css/style.css` | Estilo do dashboard. |
| `GET` | `/js/app.js` | Script de atualização automática. |

### Exemplo de `/api/stats`

```json
{
  "timestamp": "22:30:00",
  "uptime": "1d 2h 3m",
  "local_ip": "192.168.0.10",
  "cpu": {
    "percent": 12.5,
    "count": 16,
    "freq_current": 4200,
    "temperature": 55
  },
  "memory": {
    "percent": 41.2,
    "used": 13.2,
    "total": 32.0,
    "temperature": 38
  },
  "disk": {
    "percent": 55.0,
    "used": 550.0,
    "free": 450.0,
    "total": 1000.0,
    "temperature": 37
  },
  "gpu": {
    "name": "NVIDIA GeForce RTX 4070",
    "percent": 30,
    "temperature": 55
  },
  "motherboard": {
    "system_temperature": 40,
    "vrm_temperature": 50,
    "pch_temperature": 45,
    "aux_temperature": 42
  },
  "power": {
    "total_watts": 151.0,
    "wall_watts": 171.6,
    "cpu_watts": 44.6,
    "gpu_watts": 50.6,
    "memory_watts": 15.8,
    "disk_watts": 8.0,
    "motherboard_watts": 18.0,
    "fans_watts": 6.0,
    "peripherals_watts": 8.0,
    "cpu_tdp_watts": 95.0,
    "gpu_tdp_watts": 200.0,
    "session_kwh": 0.0123,
    "total_kwh": 1.8452,
    "estimated": true
  },
  "weather": {
    "temp": 22,
    "desc": "Céu claro"
  }
}
```

O campo `freq_current` é retornado em MHz.

---

## Consumo de energia

O Windows não expõe a potência real de cada componente em desktops comuns. Por isso o `PowerConsumptionService` usa um **modelo de estimativa** baseado em TDP e utilização.

### Fórmulas e valores usados

| Componente | Cálculo |
|---|---|
| CPU | `8 W + (TDP - 8 W) × (uso/100)^1,25` |
| GPU | `8 W + (TDP - 8 W) × (uso/100)^1,25` |
| RAM | `3 W + 0,4 W por GB` |
| Disco | `4 W por unidade fixa` |
| Placa-mãe | `18 W` |
| Fans | `6 W` |
| Periféricos/rede/áudio | `8 W` |
| Consumo na tomada | `total / 0,88` (eficiência estimada de 88%) |

O TDP da CPU é estimado pela quantidade de núcleos e pelo nome do processador. O TDP da GPU usa uma tabela de modelos conhecidos (NVIDIA, AMD e Intel) e, quando necessário, valores padrão por fabricante. GPUs integradas são tratadas como ~20 W.

### Energia acumulada

- `session_kwh`: energia consumida desde que o aplicativo iniciou.
- `total_kwh`: energia acumulada enquanto o PC esteve ligado, persistida entre execuções.
- O acumulado é salvo em:

```text
%LocalAppData%\ThGameMode\power-usage.json
```

- A gravação acontece a cada 30 segundos e ao encerrar o app.
- Ao reabrir o app, se o computador **não reiniciou**, o período em que ele ficou fechado é somado usando a última potência conhecida (limitado a 12 horas).
- Após reiniciar o PC, o intervalo não é contado.
- Amostras com mais de 30 segundos (suspensão/hibernação) são ignoradas para não inflar o total.

> Todos os valores de energia são **estimativas**. Para medição real seria necessário um medidor de tomada ou sensores de hardware específicos.

---

## Estrutura do projeto

```text
ThGameMode/
├── Program.cs                       # Ponto de entrada; sobe o dashboard e o formulário principal
├── Configuracao.cs                  # Modelo do ThGameModeConfig.json
├── ThGameMode.csproj                # Configuração do projeto e versão
├── ThGameMode.sln                   # Solução do Visual Studio
├── Screens/
│   ├── frmConfig.cs                 # Janela principal, bandeja, monitor e troca de planos
│   ├── frmConfig.Designer.cs
│   ├── frmConfig.resx
│   ├── DashboardControl.cs          # Legado (não usado pelo fluxo atual)
│   └── frmDashboardSimple.cs        # Legado (não usado pelo fluxo atual)
├── Services/                        # Legado: integração com um Dashboard.exe externo
│   ├── DashboardControlForm.cs
│   ├── DashboardMenuItem.cs
│   └── DashboardToggleService.cs
├── Utils/
│   ├── AppLogger.cs                 # Log diário com retenção de 7 dias
│   └── GitHubReleaseChecker.cs      # Verificação de novas versões
├── WebServer/
│   ├── WebDashboardServer.cs        # Servidor HttpListener na porta 5000
│   ├── SystemMonitorService.cs      # Coleta de CPU, GPU, RAM, disco, clima etc.
│   ├── PowerConsumptionService.cs   # Estimativa e acúmulo de energia
│   ├── DashboardHtml.cs             # HTML gerado em C#
│   └── wwwroot/
│       ├── css/style.css
│       └── js/app.js
└── Properties/
    └── PublishProfiles/
```

Os arquivos em `Screens/DashboardControl.cs`, `Screens/frmDashboardSimple.cs` e `Services/` pertencem a uma abordagem antiga, que abria um `Dashboard.exe` separado com caminho fixo. O fluxo atual usa o servidor embutido em `WebServer/`.

---

## Logs e atualizações

### Logs

Os logs ficam na pasta `logs` ao lado do executável:

```text
logs\log_YYYYMMDD.txt
```

- Formato: `AAAA-MM-DD HH:mm:ss [Info|Warning|Error] mensagem`
- Um arquivo por dia.
- Arquivos com mais de 7 dias são removidos automaticamente na inicialização.
- Falhas de escrita no log nunca interrompem o aplicativo.

### Verificação de atualizações

- Repositório consultado: `thalissonss/ThGameMode`.
- Endpoint: `https://api.github.com/repos/thalissonss/ThGameMode/releases/latest`.
- A versão da release é comparada com a versão do assembly em execução.
- Tags com prefixo `v` e sufixos como `-beta` são normalizadas antes da comparação.
- A verificação automática ocorre na inicialização e também pode ser disparada manualmente pela bandeja.
- Quando há atualização, o app exibe uma notificação e habilita **Baixar nova versão**.

---

## Limitações e segurança

- **Temperaturas:** quando o hardware/Windows não expõe sensores via WMI, o app usa estimativas (CPU, GPU e memória). A temperatura do disco é fixa em `37 °C` e as da placa-mãe são derivadas da temperatura da CPU.
- **GPU:** a utilização é obtida pelos contadores PDH do Windows, com fallback baseado no uso de VRAM. Em alguns sistemas pode ficar em `0%` ou apresentar valores aproximados.
- **Energia:** valores estimados, não medidos. O total pode variar em relação a um medidor de tomada.
- **Clima:** as coordenadas estão fixas para São Paulo.
- **Dashboard sem autenticação:** qualquer dispositivo na mesma rede que acesse a porta `5000` consegue ver as métricas. Use apenas em redes confiáveis.
- **Porta 5000:** precisa estar livre. O registro de `urlacl` pode exigir UAC na primeira execução.
- **Firewall:** se o dashboard não abrir por outro dispositivo, verifique se a porta `5000` está liberada no Firewall do Windows.
- **Plano de economia na inicialização:** na primeira avaliação sem nenhum item monitorado, o plano atual é mantido; a troca para o plano de economia acontece na transição de alto desempenho para ocioso.
- **Fechamento de aplicativos:** o recurso encerra processos pelo nome configurado. Salve o trabalho antes de usar em aplicativos com edição aberta, pois o encerramento forçado pode descartar alterações não salvas.
- **Serviços na lista de fechamento:** a interface permite adicionar serviços, mas o fechamento é feito pelo processo correspondente. Serviços protegidos ou reiniciados automaticamente pelo Windows podem continuar ativos.
- **Reabertura:** depende de o caminho do executável estar salvo em `OpenCloseExecutablePaths` e ainda existir. Aplicativos atualizados em pastas versionadas podem precisar ter o caminho atualizado manualmente.
- **Instâncias múltiplas:** o app não bloqueia a execução de mais de uma instância ao mesmo tempo.

---

## Licença

Nenhuma licença explícita foi definida neste repositório.

---

Repositório: [github.com/thalissonss/ThGameMode](https://github.com/thalissonss/ThGameMode)
