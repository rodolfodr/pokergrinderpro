using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using PokerOrganizer.Core.Interfaces;
using PokerOrganizer.Core.Models;
using PokerOrganizer.Core.Services;
using PokerOrganizer.Windows;

namespace PokerOrganizer.UI
{
    public class LayoutItem : INotifyPropertyChanged
    {
        private string _name;
        private bool _isCurrentLayout;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public bool IsCurrentLayout
        {
            get => _isCurrentLayout;
            set
            {
                if (_isCurrentLayout != value)
                {
                    _isCurrentLayout = value;
                    OnPropertyChanged(nameof(IsCurrentLayout));
                }
            }
        }

        public LayoutItem(string name, bool isCurrent = false)
        {
            _name = name;
            _isCurrentLayout = isCurrent;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class MainWindow : Window
    {
        private readonly IWindowManager _windowManager;
        private readonly ILayoutManager _layoutManager;
        private List<PokerWindow> _currentWindows;
        private string? _currentLayoutName;
        private ObservableCollection<LayoutItem> _layouts;
        private UserSettings _userSettings;
        private bool _isInitializing = true;
        private ResourceDictionary? _currentResources;

        public MainWindow()
        {
            _userSettings = UserSettings.Load();
            InitializeComponent();

            _windowManager = new WindowManager();
            _layoutManager = new LayoutManager();
            _currentWindows = new List<PokerWindow>();
            _layouts = new ObservableCollection<LayoutItem>();
            lstLayouts.ItemsSource = _layouts;

            // Seleciona o idioma atual no ComboBox
            foreach (ComboBoxItem item in cboLanguage.Items)
            {
                if (item.Tag.ToString() == _userSettings.Language)
                {
                    cboLanguage.SelectedItem = item;
                    break;
                }
            }

            // Aplica o idioma após selecionar no ComboBox
            ApplyLanguage(_userSettings.Language);

            LoadLayouts();
            RefreshWindows();
            _isInitializing = false;

            // Adiciona handler para seleção de layout
            lstLayouts.SelectionChanged += LstLayouts_SelectionChanged;
        }

        private void LstLayouts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing && lstLayouts.SelectedItem is LayoutItem selectedItem)
            {
                _currentLayoutName = selectedItem.Name;
                UpdateLayoutSelection(_currentLayoutName);
            }
        }

        private void ApplyLanguage(string languageCode)
        {
            try
            {
                // Carrega o arquivo de recursos correspondente
                var dict = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/PokerOrganizer.Core;component/Resources/Strings.{languageCode}.xaml")
                };

                // Atualiza os recursos da aplicação
                System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);

                // Guarda a referência aos recursos atuais
                _currentResources = dict;

                // Atualiza os textos da interface
                UpdateUITexts(dict);

                // Salva a preferência do usuário
                _userSettings.Language = languageCode;
                _userSettings.Save();

                // Se a janela já foi inicializada e não está no processo de inicialização, recarrega para aplicar o novo idioma
                if (IsInitialized && !_isInitializing)
                {
                    var newWindow = new MainWindow();
                    newWindow.WindowState = this.WindowState;
                    newWindow.Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                var errorMessage = _currentResources != null
                    ? string.Format((string)_currentResources["ErrorLoadingLanguage"], ex.Message)
                    : $"Error loading language: {ex.Message}";

                MessageBox.Show(
                    errorMessage,
                    _currentResources != null ? (string)_currentResources["Error"] : "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateUITexts(ResourceDictionary resources)
        {
            txtSlogan.Text = (string)resources["Slogan"];
            txtDetectedTables.Text = (string)resources["DetectedTables"];
            txtRefresh.Text = (string)resources["Refresh"];
            txtSavedLayouts.Text = (string)resources["SavedLayouts"];
            txtNew.Text = (string)resources["New"];
            txtNewLayout.Text = (string)resources["NewLayout"];
            txtSave.Text = (string)resources["Save"];
            txtCancel.Text = (string)resources["Cancel"];
            txtApply.Text = (string)resources["Apply"];
            txtDelete.Text = (string)resources["Delete"];
            txtStatus.Text = (string)resources["Ready"];

            // Atualiza as colunas do ListView
            colTitle.Header = (string)resources["Title"];
            colTitle.DisplayMemberBinding = new System.Windows.Data.Binding("Title");
            colRoom.Header = (string)resources["Room"];
            colRoom.DisplayMemberBinding = new System.Windows.Data.Binding("Room");
        }

        private void LoadLayouts()
        {
            var layouts = _layoutManager.GetLayouts();
            var selectedLayout = lstLayouts.SelectedItem as LayoutItem;
            var selectedName = selectedLayout?.Name ?? _currentLayoutName;

            _layouts.Clear();
            foreach (var layout in layouts)
            {
                var item = new LayoutItem(layout.Name, layout.Name == _currentLayoutName);
                _layouts.Add(item);
                
                if (layout.Name == selectedName)
                {
                    lstLayouts.SelectedItem = item;
                    _currentLayoutName = layout.Name;
                }
            }

            // Se não houver layout selecionado mas temos um _currentLayoutName, seleciona ele
            if (lstLayouts.SelectedItem == null && _currentLayoutName != null)
            {
                var layoutToSelect = _layouts.FirstOrDefault(l => l.Name == _currentLayoutName);
                if (layoutToSelect != null)
                {
                    lstLayouts.SelectedItem = layoutToSelect;
                }
            }
        }

        private void UpdateLayoutSelection(string layoutName)
        {
            _currentLayoutName = layoutName;

            // Primeiro, remove o negrito de todos os itens
            foreach (var item in _layouts)
            {
                if (item.IsCurrentLayout)
                {
                    item.IsCurrentLayout = false;
                }
            }

            // Depois, marca o item selecionado
            var selectedItem = _layouts.FirstOrDefault(l => l.Name == layoutName);
            if (selectedItem != null)
            {
                selectedItem.IsCurrentLayout = true;
                
                // Atualiza a seleção visual se necessário
                if (!ReferenceEquals(lstLayouts.SelectedItem, selectedItem))
                {
                    lstLayouts.SelectedItem = selectedItem;
                }
            }
        }

        private bool IsValidGGPokerWindow(string title)
        {
            title = title.ToLower();
            
            // Para mesas cash do GGPoker
            if ((title.Contains("nlh") || title.Contains("plo")) && 
                title.Contains("$") &&
                title.Contains("/") &&  // Separador de blinds para cash games
                title.Contains("-"))
                return true;
            
            // Para mesas de torneio do GGPoker
            if (title.Contains("buy-in") && 
                title.Contains("mesa") && 
                title.Contains("blinds") &&
                title.Contains("|") &&
                title.Contains("-") &&
                title.Contains(":"))
                return true;
            
            return false;
        }

        private bool IsValid888PokerWindow(string title)
        {
            title = title.ToLower();
            
            // Para mesas cash do 888Poker
            if ((title.Contains("nlh") || title.Contains("plo") || title.Contains("nl") || title.Contains("pl")) && 
                (title.Contains("$") || title.Contains("€") || title.Contains("¢")) &&
                (title.Contains("/") || title.Contains("-")))
                return true;
            
            // Para mesas de torneio do 888Poker
            if (title.Contains("tournament") || 
                title.Contains("tourney") || 
                title.Contains("sit & go") || 
                title.Contains("sit&go"))
                return true;
            
            return false;
        }

        private string FormatPokerRoomName(PokerRoom room)
        {
            return room.Equals(PokerRoom.Poker888) ? "888Poker" : room.ToString();
        }

        private void RefreshWindows()
        {
            try
            {
                _currentWindows = _windowManager.FindPokerWindows()
                    .Select(window => new PokerWindow(
                        handle: window.Handle,
                        title: window.Title,
                        x: window.X,
                        y: window.Y,
                        width: window.Width,
                        height: window.Height,
                        room: window.Room
                    ))
                    .Where(window => 
                    {
                        if (window.Room.Equals(PokerRoom.GGPoker))
                            return IsValidGGPokerWindow(window.Title);
                            
                        if (window.Room.Equals(PokerRoom.Poker888))
                            return IsValid888PokerWindow(window.Title);
                            
                        return true;
                    })
                    .ToList();

                UpdateWindowsList(_currentWindows);
                
                if (_currentResources != null)
                {
                    var message = string.Format((string)_currentResources["TablesFound"], _currentWindows.Count);
                    UpdateStatus(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar janelas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                if (_currentResources != null)
                {
                    UpdateStatus((string)_currentResources["ErrorUpdatingTables"]);
                }
            }
        }

        private void UpdateWindowsList(List<PokerWindow> windows)
        {
            var formattedWindows = windows.Select(w => new
            {
                Title = w.Title,
                Room = FormatPokerRoomName(w.Room),
                Handle = w.Handle,
                X = w.X,
                Y = w.Y,
                Width = w.Width,
                Height = w.Height
            }).ToList();

            lstWindows.ItemsSource = formattedWindows;
        }

        private void ApplySelectedLayout()
        {
            try
            {
                var selectedItem = lstLayouts.SelectedItem as LayoutItem;
                if (selectedItem == null)
                {
                    MessageBox.Show(
                        (string)_currentResources!["SelectLayoutFirst"],
                        (string)_currentResources!["Warning"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var layoutName = selectedItem.Name;
                var layout = _layoutManager.GetLayout(layoutName);
                if (layout == null)
                {
                    MessageBox.Show(
                        (string)_currentResources!["LayoutNotFound"],
                        (string)_currentResources!["Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Atualiza a lista de janelas antes de aplicar, usando o mesmo filtro do RefreshWindows
                _currentWindows = _windowManager.FindPokerWindows()
                    .Where(window => 
                    {
                        if (window.Room != PokerRoom.GGPoker) 
                            return true;
                            
                        return IsValidGGPokerWindow(window.Title);
                    })
                    .ToList();

                Console.WriteLine($"\nAplicando layout: {layoutName}");
                Console.WriteLine($"Número de janelas: {_currentWindows.Count}");
                Console.WriteLine($"Número de posições no layout: {layout.Positions.Count}");

                foreach (var pos in layout.Positions)
                {
                    Console.WriteLine($"Posição do Layout: X={pos.X}, Y={pos.Y}, W={pos.Width}, H={pos.Height}");
                }

                var positions = _layoutManager.CalculatePositions(_currentWindows.Count, layout);
                
                Console.WriteLine("\nPosições calculadas:");
                foreach (var pos in positions)
                {
                    Console.WriteLine($"Posição Calculada: X={pos.X}, Y={pos.Y}, W={pos.Width}, H={pos.Height}");
                }

                bool allWindowsMoved = true;
                for (int i = 0; i < _currentWindows.Count && i < positions.Count; i++)
                {
                    var window = _currentWindows[i];
                    var position = positions[i];

                    Console.WriteLine($"\nMovendo janela: {window.Title}");
                    Console.WriteLine($"De: X={window.X}, Y={window.Y}, W={window.Width}, H={window.Height}");
                    Console.WriteLine($"Para: X={position.X}, Y={position.Y}, W={position.Width}, H={position.Height}");

                    // Tenta mover a janela várias vezes se necessário
                    bool moved = false;
                    for (int attempt = 0; attempt < 3 && !moved; attempt++)
                    {
                        moved = _windowManager.MoveWindow(window, position);
                        if (!moved)
                        {
                            Console.WriteLine($"Tentativa {attempt + 1} falhou, tentando novamente...");
                            System.Threading.Thread.Sleep(100); // Pequena pausa entre tentativas
                        }
                    }

                    if (!moved)
                    {
                        UpdateStatus(string.Format((string)_currentResources!["ErrorMovingWindow"], window.Title));
                        Console.WriteLine("Falha ao mover janela após todas as tentativas!");
                        allWindowsMoved = false;
                    }
                    else
                    {
                        Console.WriteLine("Janela movida com sucesso!");
                    }
                }

                // Atualiza o layout atual e a seleção
                _currentLayoutName = layoutName;
                UpdateLayoutSelection(layoutName);

                if (allWindowsMoved)
                {
                    UpdateStatus(string.Format((string)_currentResources!["LayoutApplied"], layoutName));
                }
                else
                {
                    UpdateStatus(string.Format((string)_currentResources!["LayoutPartiallyApplied"], layoutName));
                }
                
                // Atualiza a lista de janelas para mostrar as novas posições
                RefreshWindows();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format((string)_currentResources!["ErrorApplyingLayout"], ex.Message),
                    (string)_currentResources!["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                UpdateStatus((string)_currentResources!["ErrorUpdatingTables"]);
            }
        }

        private void SaveNewLayout()
        {
            try
            {
                var layoutName = txtNewLayoutName.Text.Trim();
                if (string.IsNullOrEmpty(layoutName))
                {
                    MessageBox.Show(
                        (string)_currentResources!["EnterLayoutName"],
                        (string)_currentResources!["Warning"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Atualiza a lista de janelas antes de salvar
                _currentWindows = _windowManager.FindPokerWindows();

                var layout = new Layout(layoutName);
                Console.WriteLine($"\nSalvando layout: {layoutName}");
                Console.WriteLine($"Número de janelas: {_currentWindows.Count}");

                foreach (var window in _currentWindows)
                {
                    // Obtém a posição atual da janela antes de salvar
                    var currentPosition = _windowManager.GetWindowPosition(window.Handle);
                    if (currentPosition != null)
                    {
                        Console.WriteLine($"Salvando janela: {window.Title}");
                        Console.WriteLine($"Posição: X={currentPosition.X}, Y={currentPosition.Y}, W={currentPosition.Width}, H={currentPosition.Height}");
                        
                        layout.Positions.Add(new WindowPosition(
                            currentPosition.X,
                            currentPosition.Y,
                            currentPosition.Width,
                            currentPosition.Height
                        ));
                    }
                    else
                    {
                        Console.WriteLine($"Erro ao obter posição da janela: {window.Title}");
                    }
                }

                _layoutManager.SaveLayout(layout);
                _currentLayoutName = layoutName;
                LoadLayouts();
                UpdateStatus(string.Format((string)_currentResources!["LayoutSaved"], layoutName));
                
                txtNewLayoutName.Text = "";
                pnlNewLayout.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format((string)_currentResources!["ErrorSavingLayout"], ex.Message),
                    (string)_currentResources!["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                UpdateStatus((string)_currentResources!["ErrorUpdatingTables"]);
            }
        }

        private void DeleteSelectedLayout()
        {
            try
            {
                var selectedItem = lstLayouts.SelectedItem as LayoutItem;
                if (selectedItem == null)
                {
                    MessageBox.Show(
                        (string)_currentResources!["SelectLayoutFirst"],
                        (string)_currentResources!["Warning"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var layoutName = selectedItem.Name;
                if (MessageBox.Show(
                    string.Format((string)_currentResources!["ConfirmDelete"], layoutName),
                    (string)_currentResources!["ConfirmDeleteTitle"],
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _layoutManager.DeleteLayout(layoutName);
                    if (layoutName == _currentLayoutName)
                    {
                        _currentLayoutName = null;
                    }
                    LoadLayouts();
                    UpdateStatus(string.Format((string)_currentResources!["LayoutDeleted"], layoutName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format((string)_currentResources!["ErrorDeletingLayout"], ex.Message),
                    (string)_currentResources!["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                UpdateStatus((string)_currentResources!["ErrorUpdatingTables"]);
            }
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshWindows();
        }

        private void btnNewLayout_Click(object sender, RoutedEventArgs e)
        {
            pnlNewLayout.Visibility = Visibility.Visible;
            txtNewLayoutName.Focus();
        }

        private void btnSaveNewLayout_Click(object sender, RoutedEventArgs e)
        {
            SaveNewLayout();
        }

        private void btnCancelNewLayout_Click(object sender, RoutedEventArgs e)
        {
            txtNewLayoutName.Text = "";
            pnlNewLayout.Visibility = Visibility.Collapsed;
        }

        private void btnApplyLayout_Click(object sender, RoutedEventArgs e)
        {
            ApplySelectedLayout();
        }

        private void btnDeleteLayout_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedLayout();
        }

        private void cboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboLanguage.SelectedItem is ComboBoxItem selectedItem)
            {
                string languageCode = selectedItem.Tag.ToString()!;
                ApplyLanguage(languageCode);
            }
        }
    }
}