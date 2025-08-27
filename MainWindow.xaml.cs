using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO;
using System.Runtime.InteropServices;

namespace DollOverlay
{
    // Класс для хранения настроек окна
    public class WindowSettings
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public string? SelectedTableId { get; set; }
        public bool IsBackgroundOpaque { get; set; }
    }

    public class ButtonSettings
    {
        public List<int> HiddenLevels { get; set; } = new List<int>();
    }

    // Модели для аутентификации
    public class AuthRequest
    {
        public string? email { get; set; }
        public string? password { get; set; }
        public string? user { get; set; }
    }

    public class AuthResponse
    {
        public bool success { get; set; }
        public AuthData? data { get; set; }
    }

    public class AuthData
    {
        public string? token { get; set; }
    }

    // Модели для получения списка таблиц
    public class TablesResponse
    {
        public bool success { get; set; }
        public TablesResponseData? data { get; set; }
    }

    public class TablesResponseData
    {
        public List<TablesWrapper>? tables { get; set; }
    }

    public class TablesWrapper
    {
        public DynamicTableInfo? dynamic { get; set; }
    }

    public class DynamicTableInfo
    {
        [JsonPropertyName("id")]
        public string? id { get; set; }

        [JsonPropertyName("tableName")]
        public string? tableName { get; set; }

        [JsonPropertyName("castles")]
        public List<Castle>? castles { get; set; }
    }

    // Модели для WebSocket-сообщений
    public class WebSocketAuthMessage
    {
        public string? type { get; set; } = "auth";
        public string? token { get; set; }
    }

    public class WebSocketJoinMessage
    {
        public string? type { get; set; } = "join_table";
        public string? tableId { get; set; }
    }

    public class WebSocketDataUpdate
    {
        public string? type { get; set; }
        public string? tableId { get; set; }
        public string? castleId { get; set; }
        public CastleUpdateData? castleData { get; set; }
        public UserData? userData { get; set; }
    }

    public class CastleUpdateData
    {
        public int id { get; set; }
        public string? fillingDatetime { get; set; }
        public int? fillingLvl { get; set; }
        public string? fillingSpheretime { get; set; }
        public string? ownerClan { get; set; }
        public string? commentary { get; set; }
        public string? lastChangeUser { get; set; }
    }

    public class UserData
    {
        public string? id { get; set; }
        public string? username { get; set; }
    }

    // Вспомогательный класс для расчёта времени
    public static class CastleHelper
    {
        // Таблица для расчета времени, когда замок станет белым
        private static readonly Dictionary<int, Dictionary<int, double>> WhiteTimeTable = new Dictionary<int, Dictionary<int, double>>
        {
            { 15, new Dictionary<int, double> { { 1, 7.0 }, { 2, 8.0 }, { 3, 8.0 }, { 4, 8.0 }, { 5, 8.0 }, { 6, 8.0 }, { 7, 8.0 } } },
            { 30, new Dictionary<int, double> { { 1, 7.0 }, { 2, 11.0 }, { 3, 12.5 }, { 4, 12.5 }, { 5, 12.5 }, { 6, 12.5 }, { 7, 12.5 } } },
            { 45, new Dictionary<int, double> { { 1, 7.0 }, { 2, 11.0 }, { 3, 15.0 }, { 4, 17.0 }, { 5, 17.0 }, { 6, 17.0 }, { 7, 17.0 } } },
            { 60, new Dictionary<int, double> { { 1, 7.0 }, { 2, 11.0 }, { 3, 15.0 }, { 4, 18.0 }, { 5, 20.0 }, { 6, 20.5 }, { 7, 24.0 } } },
            { 75, new Dictionary<int, double> { { 1, 7.0 }, { 2, 11.0 }, { 3, 15.0 }, { 4, 18.0 }, { 5, 20.0 }, { 6, 21.0 }, { 7, 23.0 } } },
            { 90, new Dictionary<int, double> { { 1, 7.0 }, { 2, 11.0 }, { 3, 15.0 }, { 4, 18.0 }, { 5, 20.0 }, { 6, 21.0 }, { 7, 24.0 } } }
        };

        // Таблица для времени "Красный" (в часах)
        private static readonly Dictionary<int, double> RedTimeTable = new Dictionary<int, double>
        {
            { 15, 1.0 }, { 30, 1.5 }, { 45, 2.0 }, { 60, 2.5 }, { 75, 3.0 }, { 90, 3.5 }
        };

        public static DateTime GetWhiteTime(long fillingDatetime, int? lvl, int? fillingLvl)
        {
            if (lvl == null || fillingLvl == null)
            {
                return DateTime.MinValue;
            }

            int levelKey;
            if (lvl >= 90) levelKey = 90;
            else if (lvl >= 75) levelKey = 75;
            else if (lvl >= 60) levelKey = 60;
            else if (lvl >= 45) levelKey = 45;
            else if (lvl >= 30) levelKey = 30;
            else levelKey = 15;

            if (WhiteTimeTable.ContainsKey(levelKey) && WhiteTimeTable[levelKey].ContainsKey(fillingLvl.Value))
            {
                double hoursToAdd = WhiteTimeTable[levelKey][fillingLvl.Value];

                DateTimeOffset filledTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(fillingDatetime);
                return filledTimeOffset.AddHours(hoursToAdd).LocalDateTime;
            }

            return DateTime.MinValue;
        }

        // Новый метод для получения порогового времени "Красный" из таблицы
        public static TimeSpan GetRedTimeThreshold(int? lvl)
        {
            if (lvl == null) return TimeSpan.MaxValue;

            int levelKey;
            if (lvl >= 90) levelKey = 90;
            else if (lvl >= 75) levelKey = 75;
            else if (lvl >= 60) levelKey = 60;
            else if (lvl >= 45) levelKey = 45;
            else if (lvl >= 30) levelKey = 30;
            else levelKey = 15;

            if (RedTimeTable.ContainsKey(levelKey))
            {
                return TimeSpan.FromHours(RedTimeTable[levelKey]);
            }
            return TimeSpan.MaxValue;
        }
    }

    // Модель для данных замка
    public class Castle : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private int _id;
        public int id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int? _lvl;
        public int? lvl
        {
            get => _lvl;
            set
            {
                if (_lvl != value)
                {
                    _lvl = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string? _nameRu;
        public string? nameRu
        {
            get => _nameRu;
            set
            {
                if (_nameRu != value)
                {
                    _nameRu = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string? _continentNameRu;
        public string? continentNameRu
        {
            get => _continentNameRu;
            set
            {
                if (_continentNameRu != value)
                {
                    _continentNameRu = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string? _fillingSpheretime;
        public string? fillingSpheretime
        {
            get => _fillingSpheretime;
            set
            {
                if (_fillingSpheretime != value)
                {
                    _fillingSpheretime = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private int? _fillingLvl;
        public int? fillingLvl
        {
            get => _fillingLvl;
            set
            {
                if (_fillingLvl != value)
                {
                    _fillingLvl = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string? _ownerClan;
        public string? ownerClan
        {
            get => _ownerClan;
            set
            {
                if (_ownerClan != value)
                {
                    _ownerClan = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string? _commentary;
        public string? commentary
        {
            get => _commentary;
            set
            {
                if (_commentary != value)
                {
                    _commentary = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string? _fillingDatetime;
        [JsonPropertyName("fillingDatetime")]
        public string? fillingDatetime
        {
            get => _fillingDatetime;
            set
            {
                if (_fillingDatetime != value)
                {
                    _fillingDatetime = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(WhiteTimeExact));
                    NotifyPropertyChanged(nameof(WhiteTime));
                    NotifyPropertyChanged(nameof(StatusText));
                    NotifyPropertyChanged(nameof(StatusColor));
                }
            }
        }

        public DateTime WhiteTimeExact
        {
            get
            {
                if (string.IsNullOrEmpty(fillingDatetime) || !long.TryParse(fillingDatetime, out long unixTime))
                {
                    return DateTime.MinValue;
                }
                return CastleHelper.GetWhiteTime(unixTime, lvl, fillingLvl);
            }
        }

        public string WhiteTime
        {
            get
            {
                if (string.IsNullOrEmpty(fillingDatetime) || !long.TryParse(fillingDatetime, out long unixTime))
                {
                    return "N/A";
                }

                DateTime whiteTime = CastleHelper.GetWhiteTime(unixTime, lvl, fillingLvl);
                TimeSpan timeRemaining = whiteTime - DateTime.Now;

                // Если текущее время больше, чем WhiteTime на 5 минут
                if (timeRemaining.TotalMinutes <= -3)
                {
                    return "-";
                }

                // Если осталось менее 5 минут, но еще не прошло 5 минут после WhiteTime
                if (timeRemaining.TotalMinutes <= 0 && timeRemaining.TotalMinutes > -3)
                {
                    return "Белый";
                }

                return $"{(int)timeRemaining.TotalHours}ч {timeRemaining.Minutes}м";
            }
        }

        public string StatusText
        {
            get
            {
                if (string.IsNullOrEmpty(fillingDatetime) || !long.TryParse(fillingDatetime, out long unixTime) || lvl == null || fillingLvl == null)
                {
                    return "N/A";
                }

                DateTime now = DateTime.Now;
                DateTime whiteTime = CastleHelper.GetWhiteTime(unixTime, lvl, fillingLvl);
                TimeSpan timeRemaining = whiteTime - DateTime.Now;

                // Если текущее время больше, чем WhiteTime на 5 минут
                if (timeRemaining.TotalMinutes >= -3 && timeRemaining.TotalMinutes < 0)
                {
                    return "Белый";
                }

                // Если замок уже белый
                if (timeRemaining.TotalMinutes < -3)
                {
                    return "-";
                }

                // Расчет временных порогов
                TimeSpan redThreshold = CastleHelper.GetRedTimeThreshold(lvl);
                TimeSpan yellowThreshold = TimeSpan.FromHours(1); // 1 час до красного

                DateTime redTime = whiteTime.Subtract(redThreshold);
                DateTime yellowTime = redTime.Subtract(yellowThreshold);

                if (now >= redTime)
                {
                    return "Красный";
                }
                else if (now >= yellowTime)
                {
                    return "Желтый";
                }
                else
                {
                    return "Синий";
                }
            }
        }

        public SolidColorBrush StatusColor
        {
            get
            {
                if (StatusText == "Красный")
                {
                    return Brushes.Red;
                }
                else if (StatusText == "Желтый")
                {
                    return Brushes.Yellow;
                }
                else if (StatusText == "Белый")
                {
                    return Brushes.White;
                }
                else if (StatusText == "N/A" || StatusText == "-")
                {
                    return Brushes.DarkSlateGray;
                }
                else
                {
                    return Brushes.DodgerBlue;
                }
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Модель для хранения токена в файле
    public class TokenStorage
    {
        public string? authToken { get; set; }
    }

    //---------------------------------------------------------

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string? _token;
        private string? _selectedTableId;
        private readonly HttpClient _httpClient = new HttpClient();
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private List<DynamicTableInfo>? _allTables;

        private readonly ObservableCollection<Castle> _castlesObservable = new ObservableCollection<Castle>();
        private List<Castle> originalCastles = new List<Castle>(); // Сохраняем полную коллекцию
        private List<int> hiddenLevels = new List<int>(); // Список уровней для фильтрации

        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();

        private const string AuthUrl = "https://sphere-doll.ru/api/login";
        private const string TablesUrl = "https://sphere-doll.ru/api/table";
        private const string WebSocketUrl = "ws://sphere-doll.ru/ws";

        private const string TokenFileName = "token.json";
        private const string SettingsFileName = "settings.json";
        private const string ButtonSettingsFileName = "button_settings.json";
        private DispatcherTimer _topmostTimer = new DispatcherTimer();
        private bool isBackgroundOpaque = true;
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    NotifyPropertyChanged(nameof(IsLoading));
                }
            }
        }

        // Метод для вызова события PropertyChanged в MainWindow
        private void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void TopmostTimer_Tick(object sender, EventArgs e)
        {
            // Этот "хак" заставляет окно всплыть на самый верх
            // Сначала делаем его не topmost, а затем сразу topmost.
            // Затем активируем окно.
            this.Topmost = false;
            this.Topmost = true;

            // Получаем дескриптор окна
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetForegroundWindow(hwnd);
        }

        private async Task PerformActionAfterDelay()
        {
            await Task.Delay(5000);

            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetForegroundWindow(hwnd);
        }

        private async void Window_Deactivated(object? sender, EventArgs e)
        {
            await Task.Delay(1000);

            // Возвращаем UI-поток для безопасного вызова
            await Dispatcher.InvokeAsync(() =>
            {
                this.Topmost = false;
                this.Topmost = true;

                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                SetForegroundWindow(hwnd);
            });
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            // Проверяем, был ли фон изначально установлен как прозрачный
            if (!isBackgroundOpaque)
            {
                // Делаем фон непрозрачным при наведении
                this.Background = new SolidColorBrush(Color.FromArgb(255, 6, 27, 61));
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            // Проверяем, был ли фон изначально установлен как прозрачный
            if (!isBackgroundOpaque)
            {
                // Возвращаем фон к прозрачному состоянию при уходе мыши
                this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
            }
        }

        public MainWindow()
        {
            LoadWindowPosition();
            InitializeComponent();
            ApplyBackgroundState();

            DataContext = this;

            // Настраиваем и запускаем таймер для поддержания окна Topmost
            _topmostTimer.Interval = TimeSpan.FromSeconds(3);
            _topmostTimer.Tick += TopmostTimer_Tick;
            _topmostTimer.Start();

            // this.Background = new SolidColorBrush(Color.FromArgb(255, 6, 27, 61));

            // this.Deactivated += Window_Deactivated;

            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += RefreshTimer_Tick;

            CastlesDataGrid.ItemsSource = _castlesObservable;
            this.Closing += Window_Closing;

            LoadTokenFromFile();
            if (!string.IsNullOrEmpty(_token))
            {
                Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await TryLoadTablesWithTokenAsync();
                    });
                });
            }
            else
            {
                SwitchToEmailLogin();
            }
        }

        private void ToggleBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBackgroundOpaque)
            {
                this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
                isBackgroundOpaque = false;
            }
            else
            {
                this.Background = new SolidColorBrush(Color.FromArgb(255, 6, 27, 61));
                isBackgroundOpaque = true;
            }
        }

        private void SaveWindowPosition()
        {
            try
            {
                var settings = new WindowSettings
                {
                    Top = this.Top,
                    Left = this.Left,
                    Height = this.Height,
                    Width = this.Width,
                    SelectedTableId = _selectedTableId,
                    IsBackgroundOpaque = isBackgroundOpaque
                };

                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsFileName, json);
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки сохранения, так как это не критично для функционала
            }
        }

        private void LoadWindowPosition()
        {
            if (File.Exists(SettingsFileName))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFileName);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                    if (settings != null)
                    {
                        this.Top = settings.Top;
                        this.Left = settings.Left;
                        this.Height = settings.Height;
                        this.Width = settings.Width;
                        _selectedTableId = settings.SelectedTableId;
                        isBackgroundOpaque = settings.IsBackgroundOpaque;
                    }
                }
                catch (Exception ex)
                {
                    this.Height = 500;
                    this.Width = 400;
                }
            }
            else
            {
                this.Height = 500;
                this.Width = 400;
            }
        }

        private void ApplyBackgroundState()
        {
            if (isBackgroundOpaque)
            {
                // Если фон должен быть непрозрачным
                this.Background = new SolidColorBrush(Color.FromArgb(255, 6, 27, 61));
            }
            else
            {
                // Если фон должен быть прозрачным
                this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
            }
        }

        private void SaveTokenToFile(string? token)
        {
            try
            {
                var tokenData = new TokenStorage { authToken = token };
                var json = JsonSerializer.Serialize(tokenData);
                File.WriteAllText(TokenFileName, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения токена: {ex.Message}");
            }
        }

        private async void TokenLoginButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading = true;

            string? token = TokenTextBox.Text.Trim();
            if (string.IsNullOrEmpty(token))
            {
                StatusTextBlock.Text = "Введите токен с сайта";
                IsLoading = false;
                return;
            }

            _token = token;
            SaveTokenToFile(_token);

            await TryLoadTablesWithTokenAsync();
        }

        private void SwitchToEmailLogin(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";
            SwitchToEmailLogin();

        }

        private void SwitchToTokenLogin(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";
            SwitchToTokenLogin();
        }

        private void SwitchToEmailLogin()
        {
            LoginPanel.Visibility = Visibility.Visible;
            EmailLoginPanel.Visibility = Visibility.Visible;
            TokenLoginPanel.Visibility = Visibility.Collapsed;
            TablesPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Collapsed;
        }

        private void SwitchToTokenLogin()
        {
            LoginPanel.Visibility = Visibility.Visible;
            EmailLoginPanel.Visibility = Visibility.Collapsed;
            TokenLoginPanel.Visibility = Visibility.Visible;
            TablesPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Collapsed;
        }

        private void LoadTokenFromFile()
        {
            if (File.Exists(TokenFileName))
            {
                try
                {
                    var json = File.ReadAllText(TokenFileName);
                    var tokenData = JsonSerializer.Deserialize<TokenStorage>(json);
                    _token = tokenData?.authToken;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки токена: {ex.Message}");
                    _token = null;
                }
            }
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            if (MainContent.Visibility == Visibility.Visible)
            {
                // Обновляем свойства для всех замков в оригинальной коллекции
                foreach (var castle in originalCastles)
                {
                    castle.NotifyPropertyChanged(nameof(castle.WhiteTime));
                    castle.NotifyPropertyChanged(nameof(castle.StatusText));
                    castle.NotifyPropertyChanged(nameof(castle.StatusColor));
                }

                // После обновления свойств, повторно применяем фильтр и сортировку
                ApplyFilterAndSort();
            }
        }

        private async Task TryLoadTablesWithTokenAsync()
        {
            IsLoading = true;
            StatusTextBlock.Text = "Загрузка данных с сохраненным токеном...";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                var tablesResponse = await _httpClient.GetFromJsonAsync<TablesResponse>(TablesUrl);

                if (tablesResponse?.success == true && tablesResponse.data?.tables != null)
                {
                    _allTables = tablesResponse.data.tables
                                                     .Where(t => t.dynamic != null)
                                                     .Select(t => t.dynamic)
                                                     .ToList();
                    TablesDataGrid.ItemsSource = _allTables;
                    SwitchToTablesSelection();

                    if (!string.IsNullOrEmpty(_selectedTableId))
                    {
                        var savedTable = _allTables.FirstOrDefault(t => t.id == _selectedTableId);
                        if (savedTable != null)
                        {
                            HandleTableSelectionAsync(savedTable);
                        }
                    }

                    _refreshTimer.Start();
                }
                else
                {
                    _token = null;
                    SaveTokenToFile(null);
                    StatusTextBlock.Text = "Сохраненный токен недействителен. Пожалуйста, войдите снова.";
                    SwitchToLoginPanel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке с токеном: {ex.Message}");
                _token = null;
                SaveTokenToFile(null);
                SwitchToLoginPanel();
            }
            finally
            {
                IsLoading = false;
                StatusTextBlock.Text = "";
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "";
            _token = null;

            // 2. Очищаем токен в файле
            SaveTokenToFile(null);

            // 3. Переключаем на панель входа
            SwitchToLoginPanel();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading = true;
            StatusTextBlock.Text = "Выполняется вход...";

            var requestBody = new AuthRequest
            {
                email = EmailTextBox.Text,
                password = PasswordBox.Password,
                user = "login"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(AuthUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse?.success == true && authResponse.data?.token != null)
                {
                    _token = authResponse.data.token;
                    SaveTokenToFile(_token);
                    _refreshTimer.Start();

                    StatusTextBlock.Text = "Аутентификация успешна. Получение списка таблиц...";
                    await GetTablesAsync();

                    SwitchToTablesSelection();
                }
                else
                {
                    StatusTextBlock.Text = "Ошибка аутентификации. Проверьте данные.";
                }
            }
            catch (HttpRequestException ex)
            {
                StatusTextBlock.Text = $"Ошибка HTTP: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GetTablesAsync()
        {
            if (_token == null) return;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            try
            {
                var tablesResponse = await _httpClient.GetFromJsonAsync<TablesResponse>(TablesUrl);

                if (tablesResponse?.success == true && tablesResponse.data?.tables != null)
                {
                    _allTables = tablesResponse.data.tables
                                                     .Where(t => t.dynamic != null)
                                                     .Select(t => t.dynamic)
                                                     .ToList();

                    TablesDataGrid.ItemsSource = _allTables;
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка при получении списка таблиц: {ex.Message}");
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка при десериализации JSON: {ex.Message}");
            }
        }

        private void TablesDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(TablesDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;

            if (row != null)
            {
                var selectedTable = row.DataContext as DynamicTableInfo;

                if (selectedTable != null)
                {
                    HandleTableSelectionAsync(selectedTable);
                    e.Handled = true;
                }
            }
        }

        private async void HandleTableSelectionAsync(DynamicTableInfo selectedTable)
        {
            if (selectedTable?.id != null)
            {
                _selectedTableId = selectedTable.id; 

                var tableData = _allTables?.FirstOrDefault(t => t.id == selectedTable.id);

                if (tableData?.castles != null)
                {
                    originalCastles = new List<Castle>(tableData.castles);
                    LoadButtonSettings(); // Загружаем состояние кнопок
                    UpdateButtonAppearance(); // Обновляем их внешний вид
                    ApplyFilterAndSort();
                }
                await ConnectAndJoinTableAsync(selectedTable.id);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            
            if (scrollViewer != null)
            {
                // Проверяем направление прокрутки
                if (e.Delta > 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                }
                else
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                }

                // Помечаем событие как обработанное, чтобы оно не дошло до DataGrid
                e.Handled = true;
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            await DisconnectWebSocket();
            _selectedTableId = null;
            TablesDataGrid.SelectedItem = null;
            SwitchToTablesSelection();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SwitchToTablesSelection()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            TablesPanel.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
        }

        private void SwitchToMainContent()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            TablesPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }

        private void SwitchToLoginPanel()
        {
            LoginPanel.Visibility = Visibility.Visible;
            TablesPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Collapsed;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void TimeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null) return;

            if (int.TryParse(button.Tag.ToString(), out int level))
            {
                if (hiddenLevels.Contains(level))
                {
                    hiddenLevels.Remove(level);
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.White;
                }
                else
                {
                    hiddenLevels.Add(level);
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.DarkSlateGray;
                }

                ApplyFilterAndSort();
            }
        }

        private void ApplyFilterAndSort()
        {
            _castlesObservable.Clear();

            var filteredCastles = originalCastles.Where(c => c.lvl.HasValue && !hiddenLevels.Contains(c.lvl.Value)).ToList();

            foreach (var castle in filteredCastles)
            {
                _castlesObservable.Add(castle);
            }

            SortCastles();
        }

        private void UpdateDataGrid(WebSocketDataUpdate? update)
        {
            if (update?.castleId == null) return;

            Dispatcher.Invoke(() =>
            {
                if (int.TryParse(update.castleId, out int castleId))
                {
                    var existingCastleInObservable = _castlesObservable.FirstOrDefault(c => c.id == castleId);
                    var existingCastleInOriginal = originalCastles.FirstOrDefault(c => c.id == castleId);

                    if (existingCastleInObservable != null)
                    {
                        existingCastleInObservable.fillingLvl = update.castleData?.fillingLvl;
                        existingCastleInObservable.fillingSpheretime = update.castleData?.fillingSpheretime;
                        existingCastleInObservable.commentary = update.castleData?.commentary;
                        existingCastleInObservable.ownerClan = update.castleData?.ownerClan;
                        existingCastleInObservable.fillingDatetime = update.castleData?.fillingDatetime;
                    }
                    if (existingCastleInOriginal != null)
                    {
                        existingCastleInOriginal.fillingLvl = update.castleData?.fillingLvl;
                        existingCastleInOriginal.fillingSpheretime = update.castleData?.fillingSpheretime;
                        existingCastleInOriginal.commentary = update.castleData?.commentary;
                        existingCastleInOriginal.ownerClan = update.castleData?.ownerClan;
                        existingCastleInOriginal.fillingDatetime = update.castleData?.fillingDatetime;
                    }
                }

                ApplyFilterAndSort();
            });
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Находим родительский ScrollViewer
            var scrollViewer = FindVisualChild<ScrollViewer>(TablesDataGrid);

            if (scrollViewer != null)
            {
                // Проверяем направление прокрутки
                if (e.Delta > 0)
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                }
                else
                {
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                }

                // Помечаем событие как обработанное, чтобы оно не дошло до других элементов
                e.Handled = true;
            }
        }

        // Вспомогательный метод для поиска дочерних элементов в визуальном дереве
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                var foundChild = FindVisualChild<T>(child);
                if (foundChild != null)
                {
                    return foundChild;
                }
            }
            return null;
        }

        private void SortCastles()
        {
            var castlesWithTime = _castlesObservable
                .Where(c => c.WhiteTime != "N/A" && c.WhiteTime != "-")
                .OrderBy(c => c.WhiteTimeExact)
                .ToList();

            var castlesReadyToFill = _castlesObservable
                .Where(c => c.WhiteTime == "-")
                .OrderByDescending(c => c.WhiteTimeExact)
                .ToList();

            var castlesNotAvailable = _castlesObservable
                .Where(c => c.WhiteTime == "N/A")
                .ToList();

            var combinedList = castlesWithTime.Concat(castlesReadyToFill).Concat(castlesNotAvailable).ToList();

            for (int i = 0; i < combinedList.Count; i++)
            {
                if (i >= _castlesObservable.Count || !ReferenceEquals(_castlesObservable[i], combinedList[i]))
                {
                    var itemToMove = _castlesObservable.FirstOrDefault(c => ReferenceEquals(c, combinedList[i]));
                    if (itemToMove != null)
                    {
                        int oldIndex = _castlesObservable.IndexOf(itemToMove);
                        _castlesObservable.Move(oldIndex, i);
                    }
                }
            }
        }
        private async Task DisconnectWebSocket()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                _cts.Cancel();
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Switching table", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // Игнорируем исключения при закрытии
                }
                finally
                {
                    _webSocket?.Dispose();
                    _cts?.Dispose();
                    _cts = new CancellationTokenSource();
                }
            }
        }

        private async Task ConnectAndJoinTableAsync(string tableId)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            _cts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            try
            {
                await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cts.Token);
                // await SendWebSocketAuthMessage();
                await SendWebSocketJoinMessage(tableId);
                SwitchToMainContent();
                await ReceiveWebSocketMessagesAsync();
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {

            }
        }

        private async Task SendWebSocketAuthMessage()
        {
            if (_token == null) return;
            var authMessage = new WebSocketAuthMessage { token = _token };
            var json = System.Text.Json.JsonSerializer.Serialize(authMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task SendWebSocketJoinMessage(string tableId)
        {
            var joinMessage = new WebSocketJoinMessage { tableId = tableId };
            var json = System.Text.Json.JsonSerializer.Serialize(joinMessage);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        private async Task ReceiveWebSocketMessagesAsync()
        {
            var buffer = new byte[1024 * 4];
            while (_webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var stringBuilder = new StringBuilder();
                WebSocketReceiveResult result;

                try
                {
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        }
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = stringBuilder.ToString();
                        var update = JsonSerializer.Deserialize<WebSocketDataUpdate>(message);
                        UpdateDataGrid(update);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveWindowPosition();
            SaveButtonSettings(); // Сохраняем состояние кнопок

            if (_webSocket.State == WebSocketState.Open)
            {
                _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            _cts?.Dispose();
            _webSocket?.Dispose();
            _httpClient?.Dispose();
            _refreshTimer.Stop();
        }

        private void SaveButtonSettings()
        {
            try
            {
                var settings = new ButtonSettings

                {
                    HiddenLevels = this.hiddenLevels
                };

                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(ButtonSettingsFileName, json);
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки сохранения
            }
        }

        private void LoadButtonSettings()
        {
            if (File.Exists(ButtonSettingsFileName))
            {
                try
                {
                    var json = File.ReadAllText(ButtonSettingsFileName);
                    var settings = JsonSerializer.Deserialize<ButtonSettings>(json);
                    if (settings != null)
                    {
                        this.hiddenLevels = settings.HiddenLevels ?? new List<int>();
                    }
                }
                catch (Exception ex)
                {
                    this.hiddenLevels = new List<int>();
                }
            }
            else
            {
                this.hiddenLevels = new List<int>();
            }
        }
        
        private void UpdateButtonAppearance()
        {
            // Найдем StackPanel, содержащий кнопки
            if (TimeButtonsPanel == null) return;

            foreach (var child in TimeButtonsPanel.Children)
            {
                if (child is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out int level))
                {
                    if (hiddenLevels.Contains(level))
                    {
                        button.Background = Brushes.Transparent;
                        button.Foreground = Brushes.DarkSlateGray;
                    }
                    else
                    {
                        button.Background = Brushes.Transparent;
                        button.Foreground = Brushes.White;
                    }
                }
            }
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }

    public class AppConfiguration
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public string? SelectedTableId { get; set; }
    }

    public class WhiteTimeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string whiteTime && whiteTime == "-")
            {
                return Brushes.LightGray;
            }
            // Возвращаем специальное значение, чтобы использовать цвет по умолчанию (Foreground из TextBlock)
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WhiteTimeFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string whiteTime)
            {
                if (whiteTime == "-" || whiteTime == "N/A" || whiteTime == "Ready")
                {
                    return FontWeights.Normal;
                }
            }
            return FontWeights.Bold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}