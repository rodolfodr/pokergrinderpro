# Organizador de Poker

Um aplicativo para organizar janelas de poker automaticamente.

## Recursos

- Detecta automaticamente janelas do PokerStars, GGPoker, PartyPoker, 888Poker, WPT Global, CoinPoker, Bodog/Bovada e iPoker Network
- Salva e carrega layouts personalizados
- Interface moderna e intuitiva
- Suporte a múltiplos monitores
- Movimentação precisa de janelas usando API nativa do Windows

## Requisitos

- Windows 10 ou superior
- .NET 8.0 Runtime

## Instalação

1. Baixe a última versão do [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Execute o instalador do .NET Runtime
3. Baixe a última versão do Organizador de Poker
4. Extraia o arquivo zip para uma pasta de sua preferência
5. Execute o arquivo `PokerOrganizer.UI.exe`

## Como Usar

1. Abra suas mesas de poker normalmente
2. Execute o Organizador de Poker
3. Clique em "Atualizar" para detectar as janelas
4. Selecione um layout existente ou crie um novo
5. O programa organizará as janelas automaticamente

## Layouts

- Os layouts são salvos automaticamente
- Você pode criar quantos layouts quiser
- Cada layout pode ter posições diferentes para cada número de janelas
- Os layouts são salvos em `%AppData%\PokerOrganizer\layouts.json`

## Desenvolvimento

Para desenvolver o projeto:

1. Instale o [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone o repositório
3. Abra a solution no Visual Studio ou VS Code
4. Execute `dotnet build` para compilar
5. Execute `dotnet run --project src/PokerOrganizer.UI/PokerOrganizer.UI.csproj` para rodar

## Estrutura do Projeto

- `PokerOrganizer.Core`: Lógica de negócios e modelos
- `PokerOrganizer.Windows`: Interação com Win32 API
- `PokerOrganizer.UI`: Interface do usuário em WPF

## Licença

MIT License