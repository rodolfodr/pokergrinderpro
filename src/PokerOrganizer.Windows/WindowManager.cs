using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;
using PokerOrganizer.Core.Interfaces;
using PokerOrganizer.Core.Models;

namespace PokerOrganizer.Windows
{
    public class WindowManager : IWindowManager
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint SWP_NOSENDCHANGING = 0x0400;
        private const uint SWP_ASYNCWINDOWPOS = 0x4000;
        private const uint SWP_DEFERERASE = 0x2000;
        private const uint SWP_NOCOPYBITS = 0x0100;

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_NORMAL = 1;

        private const int POSITION_TOLERANCE = 20;

        private readonly string _logFile = "debug_log.txt";

        private readonly Dictionary<string, string[]> _pokerRoomPatterns = new Dictionary<string, string[]>
        {
            { "pokerstars", new[] { "pokerstars" } },
            { "ggpoker", new[] { "ggpoker", "ggnet" } },
            { "Poker888", new[] { "Poker888", "pacific", "poker" } },
            { "partypoker", new[] { "partypoker", "bwin", "party" } },
            { "winning", new[] { "winning", "blackchip", "americas cardroom", "true poker" } },
            { "wptglobal", new[] { "wptglobal" } },
            { "coinpoker", new[] { "coinpoker" } },
            { "bodog", new[] { "bodog", "bovada", "ignition" } },
            { "ipoker", new[] { "ipoker", "bet365", "betfair" } },
            { "chico", new[] { "betonline", "sportsbetting" } },
            { "horizon", new[] { "horizon" } },
            { "microgaming", new[] { "microgaming", "mpn" } }
        };

        private readonly string[] _excludedTitles = new[]
        {
            "lobby",
            "salão",
            "client",
            "launcher",
            "login",
            "menu",
            "cashier",
            "caixa",
            "settings",
            "configurações",
            "options",
            "opções",
            "account",
            "conta",
            "profile",
            "perfil",
            "help",
            "ajuda",
            "about",
            "sobre",
            "statistics",
            "estatísticas",
            "stats",
            "hand history",
            "histórico",
            "notes",
            "notas",
            "chat",
            "support",
            "suporte"
        };

        private readonly string[] _tableIdentifiers = new[]
        {
            "nlh",
            "plo",
            "nl",
            "pl",
            "fl",
            "$",
            "€",
            "£",
            "¥",
            "₹",
            "hold'em",
            "holdem",
            "omaha",
            "cash",
            "table",
            "mesa",
            "#",
            "bb",
            "sb",
            "ante",
            "tournament",
            "torneio"
        };

        private void Log(string message)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Debug.WriteLine(logMessage);
            File.AppendAllText(_logFile, logMessage + Environment.NewLine);
        }

        public List<PokerWindow> FindPokerWindows()
        {
            var windows = new List<PokerWindow>();

            bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var title = GetWindowTitle(hWnd);
                var processName = GetProcessName(hWnd);

                Log($"Encontrada janela - Título: {title}, Processo: {processName}");

                if (IsPokerWindow(title, processName))
                {
                    if (GetWindowRect(hWnd, out RECT rect))
                    {
                        var room = DetectPokerRoom(processName);
                        var window = new PokerWindow(
                            handle: hWnd,
                            title: title,
                            x: rect.Left,
                            y: rect.Top,
                            width: rect.Right - rect.Left,
                            height: rect.Bottom - rect.Top,
                            room: room
                        );
                        windows.Add(window);
                        Log($"Adicionada janela de poker: {title} ({room})");
                    }
                }

                return true;
            }

            EnumWindows(EnumWindowsProc, IntPtr.Zero);

            // Ordena as janelas para garantir que o 888poker seja processado primeiro
            return windows.OrderBy(w => w.Room != PokerRoom.Poker888).ToList();
        }

        public bool MoveWindow(PokerWindow window, WindowPosition position)
        {
            try
            {
                Log($"============= INÍCIO DA MOVIMENTAÇÃO =============");
                Log($"Tentando mover janela: {window.Title} ({window.Room})");
                Log($"Handle da janela: {window.Handle}");
                Log($"Posição desejada: X={position.X}, Y={position.Y}, W={position.Width}, H={position.Height}");

                // Verifica se a janela é válida
                if (!IsWindow(window.Handle))
                {
                    Log("ERRO: Handle da janela não é válido!");
                    return false;
                }

                // Verifica se a janela já está na posição correta
                if (GetWindowRect(window.Handle, out RECT currentRect))
                {
                    Log($"Posição atual: X={currentRect.Left}, Y={currentRect.Top}, W={currentRect.Right-currentRect.Left}, H={currentRect.Bottom-currentRect.Top}");

                    bool alreadyInPosition = Math.Abs(currentRect.Left - position.X) <= POSITION_TOLERANCE &&
                                           Math.Abs(currentRect.Top - position.Y) <= POSITION_TOLERANCE &&
                                           Math.Abs((currentRect.Right - currentRect.Left) - position.Width) <= POSITION_TOLERANCE &&
                                           Math.Abs((currentRect.Bottom - currentRect.Top) - position.Height) <= POSITION_TOLERANCE;

                    if (alreadyInPosition)
                    {
                        Log("Janela já está na posição correta, ignorando movimentação");
                        Log($"============= FIM DA MOVIMENTAÇÃO (JÁ POSICIONADA) =============");
                        return true;
                    }

                    Log($"Diferença de posição: X={Math.Abs(currentRect.Left - position.X)}, Y={Math.Abs(currentRect.Top - position.Y)}");
                }

                // Verifica se a janela está minimizada
                if (IsIconic(window.Handle))
                {
                    ShowWindow(window.Handle, SW_RESTORE);
                    Thread.Sleep(50); // Aumentado o tempo de espera
                }

                bool success = false;

                // Tratamento especial para o 888Poker
                if (window.Room == PokerRoom.Poker888)
                {
                    Log("Aplicando tratamento especial para 888poker");

                    // Primeira tentativa: SetWindowPos com TOPMOST e flags específicas
                    success = SetWindowPos(
                        window.Handle,
                        HWND_TOPMOST,
                        position.X,
                        position.Y,
                        position.Width,
                        position.Height,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS | SWP_NOOWNERZORDER
                    );

                    if (!success)
                    {
                        Log("Primeira tentativa falhou, tentando segunda abordagem");
                        
                        // Segunda tentativa: Trazer a janela para frente primeiro
                        SetForegroundWindow(window.Handle);
                        BringWindowToTop(window.Handle);
                        Thread.Sleep(50);

                        success = SetWindowPos(
                            window.Handle,
                            HWND_TOP,
                            position.X,
                            position.Y,
                            position.Width,
                            position.Height,
                            SWP_SHOWWINDOW | SWP_FRAMECHANGED | SWP_NOACTIVATE
                        );

                        if (!success)
                        {
                            Log("Segunda tentativa falhou, tentando terceira abordagem");

                            // Terceira tentativa: MoveWindow com SetWindowPos
                            success = MoveWindow(
                                window.Handle,
                                position.X,
                                position.Y,
                                position.Width,
                                position.Height,
                                true
                            );

                            if (success)
                            {
                                Thread.Sleep(50);
                                SetWindowPos(
                                    window.Handle,
                                    HWND_TOPMOST,
                                    0, 0, 0, 0,
                                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER
                                );
                            }
                        }
                    }
                }
                else
                {
                    // Para outras salas, usa o SetWindowPos normal
                    success = SetWindowPos(
                        window.Handle,
                        HWND_NOTOPMOST,
                        position.X,
                        position.Y,
                        position.Width,
                        position.Height,
                        SWP_SHOWWINDOW | SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_ASYNCWINDOWPOS
                    );

                    if (!success)
                    {
                        Log("Primeira tentativa falhou para sala não-888, tentando abordagem alternativa");
                        
                        success = MoveWindow(
                            window.Handle,
                            position.X,
                            position.Y,
                            position.Width,
                            position.Height,
                            true
                        );
                    }
                }

                // Verifica se a posição está correta
                if (success)
                {
                    Thread.Sleep(50); // Aumentado o tempo de espera

                    if (GetWindowRect(window.Handle, out RECT rect))
                    {
                        bool positionCorrect = Math.Abs(rect.Left - position.X) <= POSITION_TOLERANCE &&
                                             Math.Abs(rect.Top - position.Y) <= POSITION_TOLERANCE;

                        Log($"Posição final: X={rect.Left}, Y={rect.Top}, W={rect.Right-rect.Left}, H={rect.Bottom-rect.Top}");
                        Log($"Movimento bem sucedido? {positionCorrect}");
                        Log($"============= FIM DA MOVIMENTAÇÃO =============");

                        return positionCorrect;
                    }
                }

                Log("Falha ao mover a janela");
                Log($"============= FIM DA MOVIMENTAÇÃO =============");
                return false;
            }
            catch (Exception ex)
            {
                Log($"Erro ao mover janela: {ex.Message}");
                Log($"Stack trace: {ex.StackTrace}");
                Log($"============= FIM DA MOVIMENTAÇÃO COM ERRO =============");
                return false;
            }
        }

        private string GetWindowTitle(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            if (length == 0) return string.Empty;

            var builder = new StringBuilder(length + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            return builder.ToString();
        }

        private string GetProcessName(IntPtr hWnd)
        {
            try
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                var process = Process.GetProcessById((int)processId);
                return process?.ProcessName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsPokerWindow(string title, string processName)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(processName))
                return false;

            processName = processName.ToLower();
            title = title.ToLower();

            Log($"Verificando janela - Título: {title}, Processo: {processName}");

            // Verifica se o título contém palavras excluídas
            if (title.Contains("lobby") || title.Contains("salão"))
            {
                Log($"Título contém palavra excluída: {title}");
                return false;
            }

            // Verifica se é 888poker
            if (processName.Contains("poker") && !processName.Contains("pokerstars"))
            {
                // Para o 888poker, verifica se é mesa cash ou torneio
                bool is888CashTable = (
                    (title.Contains("nlh") || title.Contains("plo") || title.Contains("nl") || title.Contains("pl")) && 
                    (title.Contains("$") || title.Contains("€") || title.Contains("¢")) &&
                    (title.Contains("/") || title.Contains("-"))  // Separadores comuns para blinds
                );
                bool is888TourneyTable = title.Contains("tournament") || 
                                        title.Contains("tourney") || 
                                        title.Contains("sit & go") || 
                                        title.Contains("sit&go");
                
                if (is888CashTable || is888TourneyTable)
                {
                    Log($"Mesa Poker888 detectada: {title}");
                    return true;
                }
            }
            // Verifica se é PokerStars
            else if (processName.Contains("pokerstars"))
            {
                // Para o PokerStars, aceita $ ou hold'em/omaha ou tournament (desde que não seja lobby)
                bool isStarsTable = title.Contains("$") || 
                                   title.Contains("hold'em") || 
                                   title.Contains("holdem") || 
                                   title.Contains("omaha") ||
                                   title.Contains("tournament") ||
                                   title.Contains("torneio") ||
                                   title.Contains("table");

                if (isStarsTable)
                {
                    Log($"Mesa PokerStars detectada: {title}");
                    return true;
                }
            }
            // Verifica se é GGPoker
            else if (processName.Contains("ggpoker"))
            {
                // Para o GGPoker, precisa ter a palavra "mesa" ou "table" e um dos identificadores
                bool isGGTable = (title.Contains("mesa") || title.Contains("table")) && (
                    title.Contains("$") || 
                    title.Contains("hold'em") || 
                    title.Contains("holdem") || 
                    title.Contains("omaha")
                );

                if (isGGTable)
                {
                    Log($"Mesa GGPoker detectada: {title}");
                    return true;
                }
            }
            // Verifica se é PartyPoker
            else if (processName.Contains("partypoker") || processName.Contains("bwin"))
            {
                // Para o PartyPoker, basta ter $ ou hold'em/omaha
                bool isPartyTable = title.Contains("$") || 
                                   title.Contains("hold'em") || 
                                   title.Contains("holdem") || 
                                   title.Contains("omaha");

                if (isPartyTable)
                {
                    Log($"Mesa PartyPoker detectada: {title}");
                    return true;
                }
            }
            // Verifica se é CoinPoker
            else if (processName.Contains("coinpoker"))
            {
                bool isCoinPokerTable = title.Contains("usdt") || 
                                       title.Contains("table") ||
                                       title.Contains("nlh") || 
                                       title.Contains("plo");

                if (isCoinPokerTable)
                {
                    Log($"Mesa CoinPoker detectada: {title}");
                    return true;
                }
            }
            // Verifica se é WPT Global
            else if (processName.Contains("wpt") || processName.Contains("wptglobal"))
            {
                bool isWPTTable = title.Contains("$") || 
                                  title.Contains("hold'em") || 
                                  title.Contains("holdem") || 
                                  title.Contains("omaha") ||
                                  title.Contains("table");

                if (isWPTTable)
                {
                    Log($"Mesa WPT Global detectada: {title}");
                    return true;
                }
            }

            // Para outras salas, verifica se tem $ ou hold'em/omaha
            bool hasMoneySymbol = title.Contains("$");
            bool hasGameType = title.Contains("hold'em") || title.Contains("holdem") || title.Contains("omaha");

            bool isValidTable = hasMoneySymbol || hasGameType;

            Log($"Tem símbolo de dinheiro? {hasMoneySymbol} - Tem tipo de jogo? {hasGameType}");
            Log($"É uma mesa válida? {isValidTable} - {title}");

            return isValidTable;
        }

        private PokerRoom DetectPokerRoom(string processName)
        {
            processName = processName.ToLower();

            if (processName.Contains("pokerstars"))
                return PokerRoom.PokerStars;
            if (processName.Contains("partypoker") || processName.Contains("bwin") || processName.Contains("partygaming"))
                return PokerRoom.PartyPoker;
            if (processName.Contains("poker") && !processName.Contains("pokerstars") && !processName.Contains("ggpoker"))
                return PokerRoom.Poker888;
            if (processName.Contains("ggpoker") || processName.Contains("ggpokerok") || processName.Contains("ggnet"))
                return PokerRoom.GGPoker;
            if (processName.Contains("coinpoker"))
                return PokerRoom.CoinPoker;
            if (processName.Contains("wpt") || processName.Contains("wptglobal"))
                return PokerRoom.WPTGlobal;

            return PokerRoom.Unknown;
        }

        public WindowPosition? GetWindowPosition(IntPtr handle)
        {
            try
            {
                if (GetWindowRect(handle, out RECT rect))
                {
                    return new WindowPosition(
                        rect.Left,
                        rect.Top,
                        rect.Right - rect.Left,
                        rect.Bottom - rect.Top
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao obter posição da janela: {ex.Message}");
            }
            return null;
        }
    }
} 