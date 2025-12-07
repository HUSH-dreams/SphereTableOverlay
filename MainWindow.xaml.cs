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
using System.Globalization;
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
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DollOverlay
{

    public class OpacityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double opacity)
            {
                // Считаем видимым, если Opacity > 0.5 (то есть, если он 1.0)
                if (opacity > 0.5) 
                {
                    return Visibility.Visible;
                }
            }
            // Считаем невидимым, если Opacity = 0.0
            return Visibility.Collapsed; 
            // Примечание: Visibility.Collapsed освобождает место, Visibility.Hidden - скрывает, но оставляет место. 
            // Visibility.Collapsed обычно лучше для кнопок управления.
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
    public class StaticCastleInfo
    {
        public int id { get; set; }
        [JsonPropertyName("castleLvl")]
        public int? lvl { get; set; }
        [JsonPropertyName("castleNameRu")]
        public string? nameRu { get; set; }
        [JsonPropertyName("continentNameRu")]
        public string? continentNameRu { get; set; }
    }

    public class UserTableInfo
    {
        public string? id { get; set; }
        [JsonPropertyName("name")]
        public string? name { get; set; }
        public string? roleRu { get; set; }
    }

    public class AllTablesData
    {
        public List<StaticCastleInfo>? @static { get; set; }
        public List<UserTableInfo>? tables { get; set; }
    }

    public class AllTablesResponse
    {
        public bool success { get; set; }
        public AllTablesData? data { get; set; }
    }

    public class TableCastle
    {
        public int id { get; set; }
        public int lvl { get; set; }
        public string? nameEng { get; set; }
        public string? nameRu { get; set; }
        public string? continentNameEng { get; set; }
        public string? continentNameRu { get; set; }
        public string? fillingDatetime { get; set; }
        public int? fillingLvl { get; set; }
        public string? fillingSpheretime { get; set; }
        public string? ownerClan { get; set; }
        public string? commentary { get; set; }
        public string? lastChangeUser { get; set; }
    }

    public class Clan
    {
        public string? id { get; set; } // UUID, поэтому тип string
        public string? name { get; set; }
    }

    // Эта модель теперь будет использоваться для данных конкретной таблицы
    public class DynamicTableInfo
    {
        [JsonPropertyName("id")]
        public string? id { get; set; }

        [JsonPropertyName("tableName")]
        public string? tableName { get; set; }

        [JsonPropertyName("castles")]
        public List<Castle>? castles { get; set; }
    }

    public class SingleTableDataWrapper
    {
        public DynamicTableInfo? dynamic { get; set; }
        public List<Clan>? clans { get; set; }
    }

    public class SingleTableData
    {
        public SingleTableDataWrapper? data { get; set; }
    }

    public class SingleTableResponse
    {
        public bool success { get; set; }
        public SingleTableData? data { get; set; }
    }

    public class WebSocketAuthMessage
    {
        public string? type { get; set; } = "auth";
        public string? token { get; set; }
    }

    public class CastleUpdateDto
    {
        [JsonPropertyName("id")]
        public int id { get; set; }

        [JsonPropertyName("fillingDatetime")]
        public long? fillingDatetime { get; set; }

        [JsonPropertyName("fillingLvl")]
        public string? fillingLvl { get; set; }

        [JsonPropertyName("fillingSpheretime")]
        public string? fillingSpheretime { get; set; }

        [JsonPropertyName("ownerClan")]
        public string? ownerClan { get; set; }

        [JsonPropertyName("commentary")]
        public string? commentary { get; set; }

        [JsonPropertyName("tableId")]
        public string? tableId { get; set; }

        [JsonPropertyName("name")]
        public string? name { get; set; }
    }

    public class CastleUpdateRequest
    {
        [JsonPropertyName("castle")]
        public CastleUpdateDto? Castle { get; set; }
    }

    public class WebSocketJoinMessage
    {
        public string? type { get; set; } = "join_table";
        public string? tableId { get; set; }
    }

    public class WebSocketUpdatePayload
    {
        public CastleUpdateData? castleData { get; set; }
        // actionData можно не добавлять, если оно не используется
    }

    public class WebSocketDataUpdate
    {
        public string? type { get; set; }
        public string? tableId { get; set; }
        public WebSocketUpdatePayload? data { get; set; }
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
            { 60, new Dictionary<int, double> { { 1, 7.0 }, { 2, 11.0 }, { 3, 15.0 }, { 4, 18.0 }, { 5, 20.0 }, { 6, 20.5 }, { 7, 20.5 } } },
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

        private string? _ownerClanName;
        public string? OwnerClanName
        {
            get => _ownerClanName;
            set
            {
                if (_ownerClanName != value)
                {
                    _ownerClanName = value;
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
                    NotifyPropertyChanged(nameof(RedTimeExact));
                    NotifyPropertyChanged(nameof(RedTime));
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

        public DateTime RedTimeExact
        {
            get
            {
                // Проверяем, есть ли все необходимые данные для расчета
                if (string.IsNullOrEmpty(fillingDatetime) || !long.TryParse(fillingDatetime, out _))
                {
                    return DateTime.MinValue;
                }

                // Получаем время, когда замок станет белым
                DateTime whiteTime = WhiteTimeExact;
                if (whiteTime == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }

                // Получаем длительность "красной" фазы
                TimeSpan redThreshold = CastleHelper.GetRedTimeThreshold(lvl);

                // Вычисляем время начала "красной" фазы
                return whiteTime.Subtract(redThreshold);
            }
        }

        public string RedTime
        {
            get
            {
                DateTime redTime = RedTimeExact;
                // Если время не удалось рассчитать, возвращаем N/A
                if (redTime == DateTime.MinValue)
                {
                    return "N/A";
                }

                TimeSpan timeRemaining = redTime - DateTime.Now;

                // Если "красная" фаза уже наступила или прошла
                if (timeRemaining.TotalMinutes <= 0)
                {
                    return "-";
                }

                // Возвращаем оставшееся время в формате "Хч Yм"
                return $"{(int)timeRemaining.TotalHours}ч {timeRemaining.Minutes}м";
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

    // Добавьте этот класс в файл MainWindow.xaml.cs (например, перед классом MainWindow)
    public static class FocusHelper
    {
        public static readonly DependencyProperty IsOverlayFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsOverlayFocused",
                typeof(bool),
                typeof(FocusHelper),
                new PropertyMetadata(false));

        public static void SetIsOverlayFocused(UIElement element, bool value)
        {
            element.SetValue(IsOverlayFocusedProperty, value);
        }

        public static bool GetIsOverlayFocused(UIElement element)
        {
            return (bool)element.GetValue(IsOverlayFocusedProperty);
        }
    }

    //---------------------------------------------------------

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string? _token;
        private string? _selectedTableId;
        private string _currentSortColumn = "DefaultSort";
        private bool _isUserInitiatedDisconnect = false;
        private readonly object _castlesLock = new object();
        private bool IsLocked { get; set; } = false;
        private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;
        private readonly HttpClient _httpClient = new HttpClient();
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private List<UserTableInfo>? _userTables;
        private List<StaticCastleInfo>? _staticCastleData;
        private List<Castle> originalCastles = new List<Castle>();
        private List<Clan> _clans = new List<Clan>();
        private UserTableInfo _selectedTable;
        private Castle _selectedCastle;
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        // Константы для SetWindowPos
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;

        private readonly ObservableCollection<Castle> _castlesObservable = new ObservableCollection<Castle>();
        private List<int> hiddenLevels = new List<int>();
        private readonly List<int> AllLevels = new List<int> { 15, 30, 45, 60, 75, 90, 120, 250, 350 }; 

        private readonly DispatcherTimer _refreshTimer = new DispatcherTimer();

        // private const string AuthUrl = "http://localhost:1337/api/login";
        // private const string TablesUrl = "http://localhost:1337/api/table";
        // private const string WebSocketUrl = "ws://localhost:8099/ws";

        private const string AuthUrl = "https://sphere-doll.ru/api/login";
        private const string TablesUrl = "https://sphere-doll.ru/api/table";
        private const string WebSocketUrl = "ws://sphere-doll.ru/ws";

        private const string TokenFileName = "token.json";
        private const string SettingsFileName = "settings.json";
        private const string ButtonSettingsFileName = "button_settings.json";
        private const string ColumnOrderFileName = "column_order.json";
        private DispatcherTimer _topmostTimer = new DispatcherTimer();
        private bool isBackgroundOpaque = true;
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public event PropertyChangedEventHandler? PropertyChanged;
        private DispatcherTimer _windowDiscoveryTimer = new DispatcherTimer();


        private Storyboard? _loadingStoryboard;
        private bool _isLoading;
        private bool _isSavedLoading;
        private double _expandedHeight; // Для хранения высоты до сворачивания
        private bool _isContentCollapsed;
        private bool _isOverlayOnTop = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    NotifyPropertyChanged(nameof(IsLoading));
                    UpdateLoadingAnimation();
                }
            }
        }

        public bool IsContentCollapsed
        {
            get => _isContentCollapsed;
            set
            {
                if (_isContentCollapsed != value)
                {
                    _isContentCollapsed = value;
                    OnIsContentCollapsedChanged(); // Метод для изменения высоты и фона
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsSavedLoading
        {
            get => _isSavedLoading;
            set
            {
                if (_isSavedLoading != value)
                {
                    _isSavedLoading = value;
                    NotifyPropertyChanged(nameof(IsSavedLoading));
                    UpdateLoadingAnimation();
                }
            }
        }
        
        // ========= НАЧАЛО ИЗМЕНЕНИЙ =========
        
        // Имя процесса игры для отслеживания
        private const string TargetProcessName = "sphereclient";
        private IntPtr _gameWindowHandle = IntPtr.Zero;

        private void ToggleCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            IsContentCollapsed = !IsContentCollapsed;


            if (IsContentCollapsed)
            {
                ToggleCollapseButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/reveal.png"));
            }
            else
            {
                ToggleCollapseButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/hide.png"));
            }

            UpdateButtonIconState();
        }

        private void OnIsContentCollapsedChanged()
        {
            const double collapsedHeight = 30.0; // Высота верхней панели
            if (IsContentCollapsed)
            {
                // Сохраняем текущую высоту перед сворачиванием
                _expandedHeight = this.Height;

                // Сворачиваем окно
                this.MinHeight = collapsedHeight;
                this.Height = collapsedHeight;

                // Если фон прозрачный, делаем его видимым принудительно
                if (!isBackgroundOpaque)
                {
                    this.Background = new SolidColorBrush(Color.FromArgb(255, 6, 27, 61));
                }
            }
            else // Разворачиваем
            {
                this.MinHeight = 200; // Минимальная высота в развернутом виде
                this.Height = _expandedHeight;

                if (!isBackgroundOpaque)
                {
                    this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
                }
            }
        }

        // Новая логика для таймера
        private void TopmostTimer_Tick(object sender, EventArgs e)
        {
            var gameWindows = FindAllGameWindows();
    
            if (gameWindows.Count == 0)
            {
                return;
            }
            
            // Получаем хэндл текущего активного окна
            IntPtr activeWindowHandle = GetForegroundWindow();
            
            // Проверяем, является ли активное окно одним из окон игры
            bool isAnyGameWindowActive = gameWindows.Contains(activeWindowHandle);
            
            if (isAnyGameWindowActive)
            {
                // Убеждаемся, что оверлей всегда поверх
                EnsureOverlayOnTop();
            }
        }

        private void EnsureOverlayOnTop()
        {
            if (_isOverlayOnTop) return;
    
            IntPtr myWindowHandle = new WindowInteropHelper(this).Handle;
            
            this.Topmost = true;
            SetWindowPos(myWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            
            _isOverlayOnTop = true;
        }

        private void ResetOverlayState()
        {
            _isOverlayOnTop = false;
        }

        // Новая вспомогательная функция для поиска окна игры
        private IntPtr FindGameWindow()
        {
            var gameWindows = FindAllGameWindows();
            return gameWindows.Count > 0 ? gameWindows[0] : IntPtr.Zero;
        }
        
        // ========= КОНЕЦ ИЗМЕНЕНИЙ =========


        public class BooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        // Метод для вызова события PropertyChanged в MainWindow
        private void NotifyPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
            {
                // Используем наш новый метод, чтобы обновить визуальный стиль
                FocusAndSelect(control);
            }
        }

        // Это обновленная функция, которая срабатывает при клике на замок в таблице
        private void CastlesDataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(CastlesDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;

            if (row != null)
            {
                var castle = row.Item as Castle;

                if (castle != null)
                {
                    _selectedCastle = castle;
                    string currentTime = DateTime.Now.ToString("HH:mm");
                    FillingTimeTextBox.Text = currentTime;
                    // Заполняем элементы управления на форме
                    CastleNameTextBlock.Text = castle.nameRu;
                    FillingLvlComboBox.ItemsSource = new List<string> { "7", "6", "5", "4", "3", "2", "1" };
                    FillingLvlComboBox.SelectedIndex = 0;

                    FillingDateComboBox.ItemsSource = new List<string> { "Сегодня", "Вчера", "Позавчера" };
                    FillingDateComboBox.SelectedIndex = 0;


                    if (_clans != null)
                    {
                        ClanComboBox.ItemsSource = _clans;
                        ClanComboBox.DisplayMemberPath = "name";
                        ClanComboBox.SelectedValuePath = "id";

                        // Находим клан по ID и устанавливаем его как выбранный элемент
                        // Это решает вашу первую задачу (выбор по умолчанию)
                        var selectedClan = _clans.FirstOrDefault(c => c.id == castle.ownerClan);
                        if (selectedClan != null)
                        {
                            ClanComboBox.SelectedItem = selectedClan;
                        }
                        else
                        {
                            // Если клана не найдено или castle.ownerClan пуст, сбрасываем выбор
                            ClanComboBox.SelectedItem = null;
                        }
                    }

                    TablesScrollViewer.Visibility = Visibility.Collapsed;
                    EditCastleScroll.Visibility = Visibility.Visible;
                    Dispatcher.BeginInvoke(new Action(() => {
                        FocusAndSelect(FillingTimeTextBox);
                    }), DispatcherPriority.Input);
                }
            }
        }

        private void FillingTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var text = FillingTimeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Заменяем точки и пробелы на двоеточия для унификации формата
            text = text.Replace('.', ':').Replace(' ', ':');

            // Проверяем, является ли строка валидным временем
            if (TimeSpan.TryParse(text, out TimeSpan parsedTime))
            {
                // Если да, форматируем в hh:mm и обновляем Textbox
                FillingTimeTextBox.Text = parsedTime.ToString(@"hh\:mm");
            }
            else
            {
                EditErrorTextBlock.Text = "Введите время в правильном формате: hh mm, hh:mm или hh.mm";
                FillingTimeTextBox.Text = string.Empty;
            }
        }

        // Это обновленная функция для сохранения изменений, привязанная к кнопке "Отправить"
        private async void SaveCastleButton_Click(object sender, RoutedEventArgs e)
        {
            ClearError();

            if (_selectedCastle == null)

            {
                ShowError("Замок не выбран");
                return;
            }

            // ВАЛИДАЦИЯ ВРЕМЕНИ
            var timeText = FillingTimeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(timeText))
            {
                ShowError("Введите время");
                return;
            }
            if (!TimeSpan.TryParse(timeText, out TimeSpan selectedTime))
            {
                ShowError("Введите время в одном из форматов: hh:mm, hh.mm или hh mm.");
                return;
            }

            // ОБРАБОТКА ДАТЫ И СОЗДАНИЕ DTO
            DateTime selectedDate;
            string selectedDateStr = FillingDateComboBox.SelectedItem?.ToString();
            if (selectedDateStr == "Сегодня")
            {
                selectedDate = DateTime.Now.Date;
            }
            else if (selectedDateStr == "Вчера")
            {
                selectedDate = DateTime.Now.AddDays(-1).Date;
            }
            else // Позавчера
            {
                selectedDate = DateTime.Now.AddDays(-2).Date;
            }

            // ВАЛИДАЦИЯ КЛАНА
            if (ClanComboBox.SelectedItem == null)
            {
                ShowError("Выберите клан");
                return;
            }

            DateTime combinedDateTime = selectedDate.Add(selectedTime);
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixTimestamp = (long)(combinedDateTime.ToUniversalTime() - unixEpoch).TotalMilliseconds;

            // Создаем DTO для вложенного объекта
            var castleDto = new CastleUpdateDto
            {
                id = _selectedCastle.id,
                fillingLvl = FillingLvlComboBox.Text,
                ownerClan = ((Clan)ClanComboBox.SelectedItem).id,
                commentary = CommentaryTextBox.Text.Trim(),
                fillingSpheretime = FillingSpheretimeTextBox.Text.Trim(),
                fillingDatetime = unixTimestamp,
                tableId = _selectedTableId,
                name = _selectedCastle.nameRu
            };

            var request = new CastleUpdateRequest
            {
                Castle = castleDto
            };

            try
            {
                await UpdateCastleOnServer(request);
                ClearFormFields();
                EditCastleScroll.Visibility = Visibility.Collapsed;
                TablesScrollViewer.Visibility = Visibility.Visible;
                CastlesDataGrid.SelectedItem = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить изменения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Просто скрываем форму редактирования и показываем основное окно
            EditCastleScroll.Visibility = Visibility.Collapsed;
            TablesScrollViewer.Visibility = Visibility.Visible;
            CastlesDataGrid.SelectedItem = null;
        }

        private async Task UpdateCastleOnServer(CastleUpdateRequest request)
        {
            var jsonPayload = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("Токен авторизации не установлен.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            var response = await _httpClient.PostAsync($"{TablesUrl}/{_selectedTableId}/save-castle", content);
            response.EnsureSuccessStatusCode();

        }

        private bool IsAnyComboBoxDropdownOpen(DependencyObject parent)
        {
            if (parent == null) return false;

            // Проверяем сам элемент, если это ComboBox
            if (parent is ComboBox comboBox && comboBox.IsDropDownOpen)
            {
                return true;
            }

            // Рекурсивно ищем в дочерних элементах
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (IsAnyComboBoxDropdownOpen(child))
                {
                    return true;
                }
            }
            return false;
        }

        private void FillingSpheretimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var text = FillingSpheretimeTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return; // Поле пустое, валидация не нужна
            }

            // Заменяем точки и пробелы на двоеточия для унификации
            text = text.Replace('.', ':').Replace(' ', ':');

            // Проверяем, является ли строка валидным временем
            if (TimeSpan.TryParse(text, out TimeSpan parsedTime))
            {
                // Если да, форматируем в hh:mm и обновляем Textbox
                FillingSpheretimeTextBox.Text = parsedTime.ToString(@"hh\:mm");
            }
            else
            {
                // Если нет, очищаем поле
                FillingSpheretimeTextBox.Text = string.Empty;
                ShowError("Введите СВ в одном из форматов: hh:mm, hh.mm или hh mm.");
            }
        }
        
        /* * Старая логика TopmostTimer_Tick была заменена новой выше
         * private void TopmostTimer_Tick(object sender, EventArgs e)
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
        */

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

        private void MainFrame_MouseEnter(object sender, MouseEventArgs e)
        {
            MainFrame.Opacity = 1.0;
        }

        private void MainFrame_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsLocked)
            {
                MainFrame.Opacity = 0.0; // Делаем MainFrame невидимым
            }
        }

        private void UpdateLoadingAnimation()
        {
            if (IsLoading || IsSavedLoading)
            {
                _loadingStoryboard?.Begin();
            }
            else
            {
                _loadingStoryboard?.Stop();
            }
        }

        public MainWindow()
        {
            LoadWindowPosition();
            InitializeComponent();
            LoadColumnOrder();
            ApplyBackgroundState();
            UpdateButtonIconState();


            DataContext = this;
            _loadingStoryboard = this.FindResource("LoadingAnimation") as Storyboard;

            // Настраиваем и запускаем таймер для поддержания окна Topmost
            // ========= ИЗМЕНЕНИЕ ИНТЕРВАЛА ТАЙМЕРА =========
            _topmostTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _topmostTimer.Tick += TopmostTimer_Tick;
            _topmostTimer.Start();

            // this.Background = new SolidColorBrush(Color.FromArgb(255, 6, 27, 61));

            // this.Deactivated += Window_Deactivated;

            _refreshTimer.Interval = TimeSpan.FromSeconds(5);
            _refreshTimer.Tick += RefreshTimer_Tick;

            CastlesDataGrid.ItemsSource = _castlesObservable;
            this.Closing += Window_Closing;

            this.Topmost = true;
    
            // Когда окно полностью создано, настраиваем его стили
            SourceInitialized += MainWindow_SourceInitialized;

            // Устанавливаем хук при запуске
            _keyboardHook = HookCallback;
            _keyboardHookID = SetHook(_keyboardHook);

            _windowDiscoveryTimer.Interval = TimeSpan.FromSeconds(2);
            _windowDiscoveryTimer.Tick += WindowDiscoveryTimer_Tick;
            _windowDiscoveryTimer.Start();

            LoadTokenFromFile();
            if (!string.IsNullOrEmpty(_token))
            {
                IsSavedLoading = true;

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
                IsLoading = false;
                IsSavedLoading = false;
            }
        }

        private void WindowDiscoveryTimer_Tick(object sender, EventArgs e)
        {
            if (IsAnyComboBoxDropdownOpen(this))
            {
                return; 
            }

            // Периодически проверяем появление новых окон игры
            var currentGameWindows = FindAllGameWindows();

            if (currentGameWindows.Count > 0)
            {
                // Если нашли окна игры, принудительно обновляем позиционирование
                IntPtr myWindowHandle = new WindowInteropHelper(this).Handle;
                foreach (var gameWindow in currentGameWindows)
                {
                    SetWindowPos(myWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                }
            }
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            // Основные стили для оверлея
            exStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            
            // Если хотите, чтобы клики проходили сквозь оверлей - раскомментируйте:
            // exStyle |= WS_EX_TRANSPARENT;
            
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Получаем список активных контролов для ТЕКУЩЕГО экрана
                var formControls = GetActiveInputControls();
                
                // Если активных полей нет (например, мы просто смотрим таблицу), хук не перехватывает ввод
                if (formControls.Count > 0)
                {
                    var focusedControl = Keyboard.FocusedElement as Control;

                    // Логика Tab (работает глобально для всех форм)
                    // if ((focusedControl == null || !formControls.Contains(focusedControl)) && vkCode == 9)
                    // {
                    //     Dispatcher.Invoke(() =>
                    //     {
                    //         if (formControls.Count > 0) FocusAndSelect(formControls[0]);
                    //     });
                    //     return (IntPtr)1;
                    // }

                    if (focusedControl != null && formControls.Contains(focusedControl))
                    {
                        // 1. Обработка COMBOBOX (только для экрана редактирования)
                        if (focusedControl is ComboBox comboBox)
                        {
                            if (vkCode == 38) // Вверх
                            {
                                Dispatcher.Invoke(() => { if (comboBox.SelectedIndex > 0) comboBox.SelectedIndex--; });
                                return (IntPtr)1;
                            }
                            if (vkCode == 40) // Вниз
                            {
                                Dispatcher.Invoke(() => { if (comboBox.SelectedIndex < comboBox.Items.Count - 1) comboBox.SelectedIndex++; });
                                return (IntPtr)1;
                            }
                            // if (vkCode == 27) return (IntPtr)1; // Esc  
                        }

                        // 2. TAB - Навигация
                        if (vkCode == 9)
                        {
                            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;
                            Dispatcher.Invoke(() => HandleTabNavigation(focusedControl, isShiftPressed));
                            return (IntPtr)1;
                        }

                        if (vkCode == 27) 
                        {
                            // Если нажат Escape, и мы на экране редактирования
                            if (EditCastleScroll.Visibility == Visibility.Visible)
                            {
                                // Выполняем BackFromEditButton_Click
                                Dispatcher.Invoke(() => BackFromEditButton_Click(null, null));
                            }
                            // Блокируем Esc, чтобы не передавать его дальше в игру или систему
                            return (IntPtr)1; 
                        }

                        // 3. ENTER - Действие
                       if (vkCode == 13 && !(focusedControl is Button))
                        {
                            // Логика для входа (Email/Token), если панели видимы
                            if (EmailLoginPanel.Visibility == Visibility.Visible)
                                Dispatcher.Invoke(() => LoginButton_Click(null, null));
                            else if (TokenLoginPanel.Visibility == Visibility.Visible)
                                Dispatcher.Invoke(() => TokenLoginButton_Click(null, null));
                            // Если мы на экране редактирования (EditCastleScroll.Visibility == Visible)
                            else if (EditCastleScroll.Visibility == Visibility.Visible)
                                MessageBox.Show("EST");
                                Dispatcher.Invoke(() => SaveCastleButton_Click(null, null));
                            
                            // Блокируем Enter
                            return (IntPtr)1;
                        }

                        // 4. BACKSPACE / DELETE
                        if (vkCode == 8 || vkCode == 46)
                        {
                            Dispatcher.Invoke(() => HandleSpecialKey(focusedControl, vkCode));
                            return (IntPtr)1;
                        }

                        // 5. CTRL+V (ВСТАВКА)
                        // VK_V = 86, VK_CONTROL = 17 (Проверяем, что клавиша Ctrl нажата: 0x8000 = high-order bit set)
                        if (vkCode == 86 && (GetAsyncKeyState(0x11) & 0x8000) != 0) 
                        {
                            // Проверяем, что фокус находится на поле ввода, поддерживающем вставку
                            if (focusedControl is TextBox || focusedControl is PasswordBox)
                            {
                                Dispatcher.Invoke(() => HandlePaste(focusedControl));
                                return (IntPtr)1; // Блокируем дальнейшую обработку этого события
                            }
                        }

                        // 6. ВВОД ТЕКСТА (TextBox и PasswordBox)
                        // Проверяем, что это поле ввода
                        if (focusedControl is TextBox || focusedControl is PasswordBox)
                        {
                            char? character = GetCharacterFromVkCode((uint)vkCode);

                            if (character.HasValue)
                            {
                                Dispatcher.Invoke(() => HandleKeyPress(focusedControl, character.Value));
                                return (IntPtr)1;
                            }
                        }
                    }
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        private void HandlePaste(Control focusedControl)
        {
            // Проверяем, есть ли текст в буфере обмена
            if (Clipboard.ContainsText())
            {
                // Получаем текст и очищаем его от переносов строк, чтобы не ломать однострочные поля
                string textToPaste = Clipboard.GetText()
                                            .Replace("\r", "")
                                            .Replace("\n", "");

                if (focusedControl is TextBox textBox)
                {
                    // Логика вставки в TextBox с учетом курсора и выделения
                    int caretIndex = textBox.CaretIndex;
                    string currentText = textBox.Text;

                    // Если что-то выделено, удаляем выделенное
                    if (textBox.SelectionLength > 0)
                    {
                        caretIndex = textBox.SelectionStart;
                        currentText = currentText.Remove(caretIndex, textBox.SelectionLength);
                    }

                    // Вставляем текст и устанавливаем курсор
                    textBox.Text = currentText.Insert(caretIndex, textToPaste);
                    textBox.CaretIndex = caretIndex + textToPaste.Length;
                }
                else if (focusedControl is PasswordBox passwordBox)
                {
                    // Для PasswordBox просто добавляем текст в конец
                    passwordBox.Password += textToPaste;
                }
            }
        }

        private char? GetCharacterFromVkCode(uint vkCode)
        {
            // Игнорируем управляющие клавиши (Shift, Ctrl, Alt, Caps, и т.д. по отдельности)
            if ((vkCode >= 16 && vkCode <= 18) || vkCode == 20 || vkCode == 160 || vkCode == 161)
                return null;

            // 1. Подготавливаем массив состояния клавиатуры
            byte[] keyboardState = new byte[256];

            // Так как наше окно WS_EX_NOACTIVATE, обычный GetKeyboardState может вернуть неактуальные данные.
            // Надежнее проверить физическое состояние клавиш Shift и CapsLock.
            
            // Проверяем Shift (левый или правый)
            if ((GetAsyncKeyState(0x10) & 0x8000) != 0)
            {
                keyboardState[0x10] = 0x80; 
            }

            // Проверяем CapsLock (младший бит = 1, если включен)
            if ((GetKeyState(0x14) & 0x0001) != 0)
            {
                keyboardState[0x14] = 0x01; 
            }

            // Дополнительно можно подтянуть остальное состояние, если нужно
            // GetKeyboardState(keyboardState); 

            // 2. Определяем раскладку клавиатуры АКТИВНОГО окна (игры), а не нашего оверлея
            IntPtr activeWindow = GetForegroundWindow();
            uint processId;
            uint activeThreadId = GetWindowThreadProcessId(activeWindow, out processId);

            // Получаем раскладку именно того потока, где сейчас фокус ввода
            IntPtr hkl = GetKeyboardLayout(activeThreadId);

            uint scanCode = MapVirtualKey(vkCode, 0); // 0 = MAPVK_VK_TO_SC
            
            StringBuilder outChar = new StringBuilder(5);
            
            // Используем флаг 4 (MENU_IS_NOT_ACTIVE), чтобы не вызывать срабатывание меню
            int result = ToUnicodeEx(vkCode, scanCode, keyboardState, outChar, outChar.Capacity, 0, hkl);

            if (result > 0) 
            {
                return outChar[0];
            }
            
            return null;
        }

        private List<Control> GetActiveInputControls()
        {
            var controls = new List<Control>();

            // 1. ПРИОРИТЕТ 1: Редактирование замка (проверяем первым)
            if (EditCastleScroll.Visibility == Visibility.Visible)
            {
                if (FillingTimeTextBox != null) controls.Add(FillingTimeTextBox);
                if (FillingDateComboBox != null) controls.Add(FillingDateComboBox);
                if (FillingLvlComboBox != null) controls.Add(FillingLvlComboBox);
                if (FillingSpheretimeTextBox != null) controls.Add(FillingSpheretimeTextBox);
                if (ClanComboBox != null) controls.Add(ClanComboBox);
                if (CommentaryTextBox != null) controls.Add(CommentaryTextBox);
                if (CancelEditButton != null) controls.Add(CancelEditButton);
                if (SaveCastleButton != null) controls.Add(SaveCastleButton);
            }
            // 2. ПРИОРИТЕТ 2: Вход по Email
            else if (EmailLoginPanel.Visibility == Visibility.Visible)
            {
                controls.Add(EmailTextBox);
                controls.Add(PasswordBox);
                controls.Add(LoginButton);
            }
            // 3. ПРИОРИТЕТ 3: Вход по Токену
            else if (TokenLoginPanel.Visibility == Visibility.Visible)
            {
                controls.Add(TokenTextBox);
                controls.Add(TokenLoginButton);
            }

            return controls;
        }
        
        private void HandleSpecialKey(Control focusedControl, int vkCode)
        {
            // --- TEXTBOX ---
            if (focusedControl is TextBox textBox)
            {
                int caretIndex = textBox.CaretIndex;
                string currentText = textBox.Text;

                if (textBox.SelectionLength > 0)
                {
                    int start = textBox.SelectionStart;
                    textBox.Text = currentText.Remove(start, textBox.SelectionLength);
                    textBox.CaretIndex = start;
                }
                else if (vkCode == 8 && caretIndex > 0) // Backspace
                {
                    textBox.Text = currentText.Remove(caretIndex - 1, 1);
                    textBox.CaretIndex = caretIndex - 1;
                }
                else if (vkCode == 46 && caretIndex < currentText.Length) // Delete
                {
                    textBox.Text = currentText.Remove(caretIndex, 1);
                    textBox.CaretIndex = caretIndex;
                }
            }
            // --- PASSWORDBOX ---
            else if (focusedControl is PasswordBox passwordBox)
            {
                // Только Backspace (удаляем последний символ)
                if (vkCode == 8 && passwordBox.Password.Length > 0)
                {
                    passwordBox.Password = passwordBox.Password.Substring(0, passwordBox.Password.Length - 1);
                }
            }
        }

        private void FocusAndSelect(Control control)
        {
            var allControls = GetActiveInputControls();
            foreach (var c in allControls)
            {
                FocusHelper.SetIsOverlayFocused(c, false);
            }

            // 2. Устанавливаем "виртуальный фокус" на целевой элемент
            FocusHelper.SetIsOverlayFocused(control, true);

            // 3. Оставляем старую логику WPF (на всякий случай)
            control.Focus();
            if (control is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void HandleTabNavigation(Control currentControl, bool isReverse = false)
        {
           var inputControls = GetActiveInputControls(); 
            if (inputControls.Count == 0) return;

            int currentIndex = inputControls.IndexOf(currentControl);
            if (currentIndex == -1) currentIndex = 0;

            int nextIndex = isReverse ?
                (currentIndex - 1 + inputControls.Count) % inputControls.Count :
                (currentIndex + 1) % inputControls.Count;

            FocusAndSelect(inputControls[nextIndex]);
        }

        // Метод для получения всех инпутов в форме редактирования
        private List<Control> GetInputControlsInEditForm()
        {
             var controls = new List<Control>();
            try
            {
                // Проверяем видимость формы редактирования
                if (EditCastleScroll == null || EditCastleScroll.Visibility != Visibility.Visible)
                {
                    Debug.WriteLine("Форма редактирования не видима");
                    return controls;
                }

                // Добавляем элементы в правильном порядке (согласно их TabIndex в XAML)
                // FillingTimeTextBox (TabIndex="1")
                if (FillingTimeTextBox != null && FillingTimeTextBox.IsVisible && FillingTimeTextBox.IsEnabled)
                    controls.Add(FillingTimeTextBox);

                // FillingDateComboBox (TabIndex="2")
                if (FillingDateComboBox != null && FillingDateComboBox.IsVisible && FillingDateComboBox.IsEnabled)
                    controls.Add(FillingDateComboBox);

                // FillingLvlComboBox (TabIndex="3")
                if (FillingLvlComboBox != null && FillingLvlComboBox.IsVisible && FillingLvlComboBox.IsEnabled)
                    controls.Add(FillingLvlComboBox);

                // ClanComboBox (TabIndex="4")
                if (FillingSpheretimeTextBox != null && FillingSpheretimeTextBox.IsVisible && FillingSpheretimeTextBox.IsEnabled)
                    controls.Add(FillingSpheretimeTextBox);
                
                
                // FillingSpheretimeTextBox (TabIndex="5")
                if (ClanComboBox != null && ClanComboBox.IsVisible && ClanComboBox.IsEnabled)
                    controls.Add(ClanComboBox);

                // CommentaryTextBox (TabIndex="6")
                if (CommentaryTextBox != null && CommentaryTextBox.IsVisible && CommentaryTextBox.IsEnabled)
                    controls.Add(CommentaryTextBox);
                    
                // BackFromEditButton (TabIndex="7")
                if (CancelEditButton != null && CancelEditButton.IsVisible && CancelEditButton.IsEnabled)
                    controls.Add(CancelEditButton);

                // SaveCastleButton (TabIndex="8")
                if (SaveCastleButton != null && SaveCastleButton.IsVisible && SaveCastleButton.IsEnabled)
                    controls.Add(SaveCastleButton);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении контролов: {ex.Message}");
            }

            return controls;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Обработка клавиши Tab для ручного переключения фокуса, 
            // необходимого из-за использования WS_EX_NOACTIVATE
            if (e.Key == Key.Tab && EditCastleScroll.Visibility == Visibility.Visible)
            {
                e.Handled = true; // Отключаем стандартное поведение Tab
                
                var controls = GetInputControlsInEditForm();
                if (controls.Count == 0) return;

                // Определяем текущий элемент в фокусе
                var focusedElement = Keyboard.FocusedElement as Control;
                int currentIndex = -1;

                // Находим индекс текущего элемента в нашем списке
                for (int i = 0; i < controls.Count; i++)
                {
                    if (controls[i] == focusedElement)
                    {
                        currentIndex = i;
                        break;
                    }
                }

                // Если фокус на элементе, который не в списке, начинаем с первого/последнего
                if (currentIndex == -1)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        // Shift+Tab: начинаем с последнего
                        Keyboard.Focus(controls.Last());
                    }
                    else
                    {
                        // Tab: начинаем с первого
                        Keyboard.Focus(controls.First());
                    }
                    return;
                }

                int nextIndex;
                bool isShiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                if (isShiftDown) // Shift+Tab (назад)
                {
                    nextIndex = (currentIndex - 1 + controls.Count) % controls.Count;
                }
                else // Tab (вперед)
                {
                    nextIndex = (currentIndex + 1) % controls.Count;
                }

                // Устанавливаем фокус на следующий элемент
                Keyboard.Focus(controls[nextIndex]);
            }
        }

        private void FindControlsRecursive(DependencyObject parent, List<Control> controls)
        {
            if (parent == null) return;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Control control && 
                    (control is TextBox || control is ComboBox) &&
                    control.Visibility == Visibility.Visible &&
                    control.IsEnabled)
                {
                    controls.Add(control);
                }
                
                // Рекурсивно ищем в дочерних элементах
                FindControlsRecursive(child, controls);
            }
        }

        private bool IsNavigationKey(int vkCode)
        {
            int[] navigationKeys = {
                9,   // Tab
                13,  // Enter
                27,  // Escape
                37,  // Left arrow
                38,  // Up arrow
                39,  // Right arrow
                40,  // Down arrow
                33,  // Page Up
                34,  // Page Down
                36,  // Home
                35,  // End
                16,  // Shift
                17,  // Control
                18,  // Alt
                20,  // Caps Lock
                91,  // Left Windows
                92,  // Right Windows
                112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, // F1-F12
            };
            
            return navigationKeys.Contains(vkCode);
        }

        private bool IsTextInputKey(int vkCode)
        {
            // Буквы A-Z
            if (vkCode >= 65 && vkCode <= 90)
                return true;
            
            // Цифры 0-9
            if (vkCode >= 48 && vkCode <= 57)
                return true;
            
            // Специальные клавиши ввода
            if (vkCode == 8 || vkCode == 32) // Backspace, Space
                return true;
            
            return false;
        }

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        private const uint SWP_NOACTIVATE = 0x0010;

        private List<IntPtr> FindAllGameWindows()
        {
            var gameWindows = new List<IntPtr>();
    
            try
            {
                // Ищем по имени процесса
                Process[] processes = Process.GetProcessesByName(TargetProcessName);
                foreach (Process process in processes)
                {
                    if (process != null && !process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        gameWindows.Add(process.MainWindowHandle);
                    }
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки при поиске процессов
            }
            
            return gameWindows;
        }

        // Добавьте эти импорты
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread); // Получение хэндла раскладки для потока

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private bool IsGameWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            
            StringBuilder windowText = new StringBuilder(256);
            GetWindowText(hWnd, windowText, windowText.Capacity);
            
            string title = windowText.ToString();
            return !string.IsNullOrEmpty(title) && 
                (title.Contains("Sphere") || title.Contains("SphereClient"));
        }

        private void HandleKeyPress(Control focusedControl, char character)
        {       
            // --- ЛОГИКА ДЛЯ TEXTBOX ---
            // focusedControl приводится к TextBox, и новая переменная 'textBox' объявляется внутри этого if.
            if (focusedControl is TextBox textBox)
            {
                int caretIndex = textBox.CaretIndex;
                string currentText = textBox.Text;

                if (textBox.SelectionLength > 0)
                {
                    caretIndex = textBox.SelectionStart; 
                    currentText = currentText.Remove(caretIndex, textBox.SelectionLength);
                }

                if (caretIndex >= 0 && caretIndex <= currentText.Length)
                {
                    if (character == ',') character = '.';
                    
                    textBox.Text = currentText.Insert(caretIndex, character.ToString());
                    
                    textBox.CaretIndex = caretIndex + 1;
                }
            }
            // --- ЛОГИКА ДЛЯ PASSWORDBOX ---
            // focusedControl приводится к PasswordBox, и новая переменная 'passwordBox' объявляется здесь.
            else if (focusedControl is PasswordBox passwordBox)
            {
                // У PasswordBox нет CaretIndex, поэтому просто добавляем в конец
                passwordBox.Password += character;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            base.OnClosed(e);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 256)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl); // Добавлен хэндл раскладки

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private LowLevelKeyboardProc _keyboardHook;
        private IntPtr _keyboardHookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private void ToggleBackgroundButton_Click(object sender, RoutedEventArgs e)
        {
            IsLocked = !IsLocked;

            if (IsLocked)
            {
                MainFrame.Opacity = 1.0;
                BackgroundToggleButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/show-background.png"));
            }
            else
            {
                if (!this.IsMouseOver)
                {
                    MainFrame.Opacity = 0.0;
                }

                BackgroundToggleButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/hide-background.png"));
            }

            UpdateButtonIconState();
        }
        
        private void UpdateButtonIconState()
        {
            if (IsLocked)
            {
                BackgroundToggleButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/show-background.png"));
            }
            else
            {
                BackgroundToggleButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/hide-background.png"));
            }

            if (IsContentCollapsed)
            {
                ToggleCollapseButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/reveal.png"));
            } else
            {
                ToggleCollapseButtonImage.Source = new BitmapImage(new Uri("pack://application:,,,/images/hide.png"));
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
                    Height = IsContentCollapsed ? _expandedHeight : this.Height,
                    Width = this.Width,
                    SelectedTableId = _selectedTableId,
                    IsBackgroundOpaque = IsLocked
                };

                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsFileName, json);
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки сохранения, так как это не критично для функционала
            }
        }

        private void SaveColumnOrder()
        {
            try
            {
                // Создаем словарь для хранения порядка: "ИмяСвойства" -> Позиция
                var columnOrder = new Dictionary<string, int>();

                foreach (var column in CastlesDataGrid.Columns)
                {
                    // Используем SortMemberPath как уникальный ключ колонки
                    if (!string.IsNullOrEmpty(column.SortMemberPath))
                    {
                        columnOrder[column.SortMemberPath] = column.DisplayIndex;
                    }
                }

                var json = JsonSerializer.Serialize(columnOrder);
                File.WriteAllText(ColumnOrderFileName, json);
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки, т.к. это не критично для работы
            }
        }

        private void LoadColumnOrder()
        {
            try
            {
                if (!File.Exists(ColumnOrderFileName)) return;

                var json = File.ReadAllText(ColumnOrderFileName);
                var columnOrder = JsonSerializer.Deserialize<Dictionary<string, int>>(json);

                if (columnOrder == null) return;

                foreach (var column in CastlesDataGrid.Columns)
                {
                    // Ищем в сохраненных настройках колонку с таким же ключом (SortMemberPath)
                    if (!string.IsNullOrEmpty(column.SortMemberPath) && 
                        columnOrder.TryGetValue(column.SortMemberPath, out int displayIndex))
                    {
                        column.DisplayIndex = displayIndex;
                    }
                }
            }
            catch (Exception ex)
            {
                // Если файл поврежден или что-то пошло не так, просто используем порядок по умолчанию
            }
        }

        private void ClearFormFields()
        {
            FillingTimeTextBox.Text = string.Empty;
            FillingSpheretimeTextBox.Text = string.Empty;
            CommentaryTextBox.Text = string.Empty;
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
                        IsLocked = settings.IsBackgroundOpaque;
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
            if (IsLocked)
            {
                MainFrame.Opacity = 1.0; // Фон виден
            }
            else
            {
                MainFrame.Opacity = 0.0; // Фон не виден
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
            TablesScrollViewer.Visibility = Visibility.Collapsed;
            EditCastleScroll.Visibility = Visibility.Collapsed;
        }

        private void SwitchToTokenLogin()
        {
            LoginPanel.Visibility = Visibility.Visible;
            EmailLoginPanel.Visibility = Visibility.Collapsed;
            TokenLoginPanel.Visibility = Visibility.Visible;
            TablesPanel.Visibility = Visibility.Collapsed;
            TablesScrollViewer.Visibility = Visibility.Collapsed;
            EditCastleScroll.Visibility = Visibility.Collapsed;
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
            // Если мы не видим таблицу, нет смысла обновлять таймеры
            if (TablesScrollViewer.Visibility != Visibility.Visible) return;

            try
            {
                // 1. Создаем копию списка для итерации, чтобы избежать конфликтов 
                // с обновлением данных из WebSocket (CollectionModified exception)
                List<Castle> castlesSnapshot;
                lock (_castlesLock) // Этот объект нужно объявить (см. ниже)
                {
                    if (originalCastles == null) return;
                    castlesSnapshot = originalCastles.ToList();
                }

                // 2. Обновляем свойства (это вызывает пересчет WhiteTime внутри геттеров)
                foreach (var castle in castlesSnapshot)
                {
                    castle.NotifyPropertyChanged(nameof(castle.WhiteTime));
                    castle.NotifyPropertyChanged(nameof(castle.StatusText));
                    castle.NotifyPropertyChanged(nameof(castle.StatusColor));
                    castle.NotifyPropertyChanged(nameof(castle.RedTime)); // Тоже обновляем, раз есть в UI
                }

                // 3. Применяем сортировку
                ApplyFilterAndSort();
            }
            catch (Exception ex)
            {
                // Логируем ошибку (или просто выводим в Debug), но НЕ роняем таймер
                Debug.WriteLine($"Ошибка в таймере: {ex.Message}");
            }
        }

        private async Task TryLoadTablesWithTokenAsync()
        {
            StatusTextBlock.Text = "Загрузка данных...";

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
                var tablesResponse = await _httpClient.GetFromJsonAsync<AllTablesResponse>(TablesUrl);

                if (tablesResponse?.success == true && tablesResponse.data?.tables != null)
                {
                    _userTables = tablesResponse.data.tables;
                    _staticCastleData = tablesResponse.data.@static;
                    TablesDataGrid.ItemsSource = _userTables;

                    // Проверяем, нужно ли загружать конкретную таблицу
                    if (!string.IsNullOrEmpty(_selectedTableId))
                    {
                        var savedTable = _userTables.FirstOrDefault(t => t.id == _selectedTableId);
                        if (savedTable != null)
                        {
                            // Загружаем данные таблицы, анимация продолжается
                            await HandleTableSelectionAsync(savedTable);
                        }
                    }
                    else
                    {
                        // Просто показываем экран выбора таблиц
                        SwitchToTablesSelection();
                    }
                }
                else
                {
                    _token = null;
                    SaveTokenToFile(null);
                    StatusTextBlock.Text = "Сохраненный токен недействителен. Войдите снова.";
                    SwitchToLoginPanel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}");
                _token = null;
                SaveTokenToFile(null);
                SwitchToLoginPanel();
            }
            finally
            {
                // В любом случае убираем анимацию загрузки после завершения всех операций
                IsSavedLoading = false;
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
                // Используем новую модель AllTablesResponse
                var tablesResponse = await _httpClient.GetFromJsonAsync<AllTablesResponse>(TablesUrl);

                if (tablesResponse?.success == true && tablesResponse.data != null)
                {
                    // Сохраняем оба списка
                    _userTables = tablesResponse.data.tables;
                    _staticCastleData = tablesResponse.data.@static;

                    TablesDataGrid.ItemsSource = _userTables;
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
                var selectedTable = row.Item as UserTableInfo;
                if (selectedTable != null)
                {
                    HandleTableSelectionAsync(selectedTable);

                    TablesPanel.Visibility = Visibility.Collapsed;
                    TablesScrollViewer.Visibility = Visibility.Visible;
                    EditCastleScroll.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async Task HandleTableSelectionAsync(UserTableInfo selectedTable)
        {
            if (selectedTable?.id == null || _staticCastleData == null) return;

            IsLoading = true;
            _selectedTableId = selectedTable.id;

            try
            {
                var specificTableUrl = $"{TablesUrl}/{selectedTable.id}";
                var response = await _httpClient.GetFromJsonAsync<SingleTableResponse>(specificTableUrl);

                if (response?.success == true && response.data?.data?.dynamic?.castles != null)
                {
                    _selectedCastle = null;
                    _clans = response.data.data.clans ?? new List<Clan>();

                    var dynamicCastles = response.data.data.dynamic.castles;
                    var combinedCastles = new List<Castle>();

                    foreach (var staticInfo in _staticCastleData)
                    {
                        var dynamicInfo = dynamicCastles.FirstOrDefault(c => c.id == staticInfo.id);
                        var castle = new Castle
                        {
                            id = staticInfo.id,
                            lvl = staticInfo.lvl,
                            nameRu = staticInfo.nameRu,
                            continentNameRu = staticInfo.continentNameRu,
                            // Данные из динамического запроса (если они есть)
                            fillingDatetime = dynamicInfo?.fillingDatetime,
                            fillingLvl = dynamicInfo?.fillingLvl,
                            fillingSpheretime = dynamicInfo?.fillingSpheretime,
                            ownerClan = dynamicInfo?.ownerClan,
                            OwnerClanName = _clans.FirstOrDefault(c => c.id == dynamicInfo?.ownerClan)?.name ?? dynamicInfo?.ownerClan,
                            commentary = dynamicInfo?.commentary,
                        };
                        combinedCastles.Add(castle);
                    }

                    lock (_castlesLock)
                    {
                        originalCastles = combinedCastles;
                    }

                    LoadButtonSettings();
                    UpdateButtonAppearance();
                    
                    // Сбрасываем сортировку на дефолтную при каждой загрузке таблицы
                    _currentSortColumn = "DefaultSort";
                    _currentSortDirection = ListSortDirection.Ascending;
                    // Убираем старые индикаторы сортировки с колонок
                    foreach (var col in CastlesDataGrid.Columns)
                    {
                        col.SortDirection = null;
                    }

                    ApplyFilterAndSort(); // Применяем фильтр и сортировку по умолчанию
                    _refreshTimer.Start();
                    
                    await ConnectAndJoinTableAsync(selectedTable.id);
                    
                }
            }
            catch (Exception ex)
            {
                IsSavedLoading = false;
                MessageBox.Show($"Не удалось загрузить данные таблицы: {ex.Message}");
            }
            finally
            {
                IsSavedLoading = false;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;

            if (scrollViewer != null)
            {
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
            _selectedCastle = null;
            await DisconnectWebSocket();
            _selectedTableId = null;
            TablesDataGrid.SelectedItem = null;
            SwitchToTablesSelection();
        }

        private async void BackFromEditButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedCastle = null;
            _refreshTimer.Stop();
            ClearFormFields();
            SwitchToMainContent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowError(string message)
        {
            EditErrorTextBlock.Text = message;
            EditErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            EditErrorTextBlock.Text = string.Empty;
            EditErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        private void SwitchToTablesSelection()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            TablesPanel.Visibility = Visibility.Visible;
            TablesScrollViewer.Visibility = Visibility.Collapsed;
            EditCastleScroll.Visibility = Visibility.Collapsed;
        }

        private void SwitchToMainContent()
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            TablesPanel.Visibility = Visibility.Collapsed;
            TablesScrollViewer.Visibility = Visibility.Visible;
            EditCastleScroll.Visibility = Visibility.Collapsed;
            _selectedCastle = null;
        }

        private void SwitchToLoginPanel()
        {
            LoginPanel.Visibility = Visibility.Visible;
            TablesPanel.Visibility = Visibility.Collapsed;
            TablesScrollViewer.Visibility = Visibility.Collapsed;
            EditCastleScroll.Visibility = Visibility.Collapsed;
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

        private void SelectAllLocks_Click(object sender, RoutedEventArgs e)
        {
            // 1. Установить все замки (добавить все уровни в hiddenLevels)
            hiddenLevels.Clear();
            foreach (int level in AllLevels)
            {
                hiddenLevels.Add(level);
            }

            // 2. Обновить внешний вид всех кнопок-уровней
            // Перебираем все элементы внутри StackPanel, чтобы найти кнопки с числовым Tag (уровни)
            foreach (var child in TimeButtonsPanel.Children)
            {
                // Проверяем, что это кнопка с числовым Tag (чтобы не трогать кнопки-иконки)
                if (child is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out _))
                {
                    // Устанавливаем стиль "скрыто"
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.DarkSlateGray;
                }
            }

            // 3. Применить фильтр
            ApplyFilterAndSort();
        }

        private void RemoveAllLocks_Click(object sender, RoutedEventArgs e)
        {
            // 1. Снять все замки (очистить hiddenLevels)
            hiddenLevels.Clear();

            // 2. Обновить внешний вид всех кнопок-уровней
            // Перебираем все элементы внутри StackPanel, чтобы найти кнопки с числовым Tag
            foreach (var child in TimeButtonsPanel.Children)
            {
                // Проверяем, что это кнопка с числовым Tag
                if (child is Button button && button.Tag != null && int.TryParse(button.Tag.ToString(), out _))
                {
                    // Устанавливаем стиль "видимо"
                    button.Background = Brushes.Transparent;
                    button.Foreground = Brushes.White;
                }
            }

            // 3. Применить фильтр
            ApplyFilterAndSort();
        }
        
        private Func<Castle, object> GetKeySelector(string sortColumn)
        {
            switch (sortColumn)
            {
                case "nameRu": // "Замок"
                    return c => c.lvl ?? 0;
                case "WhiteTimeExact": // "Лить" и "Через"
                    return c => c.WhiteTimeExact;
                case "RedTimeExact": // "Спад"
                    return c => c.RedTimeExact;
                case "OwnerClanName": // "Клан"
                    return c => c.OwnerClanName ?? string.Empty;
                default:
                    // По умолчанию, если что-то пошло не так
                    return c => c.id;
            }
        }

        private void CastlesDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;

            string column = e.Column.SortMemberPath;

            // Список колонок, для которых разрешена сортировка
            var sortableColumns = new[] { "nameRu", "WhiteTimeExact", "RedTimeExact", "OwnerClanName" };
            if (!sortableColumns.Contains(column))
            {
                return; // Если кликнули по другой колонке, ничего не делаем
            }

            // Определяем направление и колонку для новой сортировки
            if (_currentSortColumn == column)
            {
                // Если кликнули по той же колонке, меняем направление
                _currentSortDirection = _currentSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                // Если кликнули по новой колонке, устанавливаем ее и сбрасываем направление на "по возрастанию"
                _currentSortColumn = column;
                _currentSortDirection = ListSortDirection.Ascending;
            }

            // Применяем новую сортировку
            ApplyFilterAndSort();

            // Обновляем визуальные индикаторы (стрелочки) на заголовках колонок
            foreach (var col in CastlesDataGrid.Columns)
            {
                if (col.SortMemberPath == column)
                {
                    col.SortDirection = _currentSortDirection;
                }
                else
                {
                    // Убираем стрелочку с других колонок
                    col.SortDirection = null;
                }
            }
        }

        private void ApplyFilterAndSort()
        {
            // 1. Фильтруем оригинальный список по выбранным уровням
            var filteredCastles = originalCastles.Where(c => c.lvl.HasValue && !hiddenLevels.Contains(c.lvl.Value));

            // 2. Разделяем отфильтрованный список на три основные группы
            var castlesWithTime = filteredCastles
                .Where(c => c.WhiteTime != "N/A" && c.WhiteTime != "-")
                .ToList();

            var castlesReadyToFill = filteredCastles
                .Where(c => c.WhiteTime == "-")
                .ToList();

            var castlesNotAvailable = filteredCastles
                .Where(c => c.WhiteTime == "N/A")
                .ToList();

            // 3. Сортируем каждую группу отдельно

            // Обрабатываем специальный случай сортировки по умолчанию
            if (_currentSortColumn == "DefaultSort")
            {
                castlesWithTime = castlesWithTime.OrderBy(c => c.WhiteTimeExact).ToList();
                castlesReadyToFill = castlesReadyToFill.OrderByDescending(c => c.WhiteTimeExact).ToList();
                // Группа N/A не сортируется по умолчанию
            }
            else // Обрабатываем сортировку по клику на колонку
            {
                // Получаем "ключ" для сортировки (по какому полю сортировать)
                Func<Castle, object> keySelector = GetKeySelector(_currentSortColumn);

                if (_currentSortDirection == ListSortDirection.Ascending)
                {
                    castlesWithTime = castlesWithTime.OrderBy(keySelector).ToList();
                    castlesReadyToFill = castlesReadyToFill.OrderBy(keySelector).ToList();
                    castlesNotAvailable = castlesNotAvailable.OrderBy(keySelector).ToList();
                }
                else // ListSortDirection.Descending
                {
                    castlesWithTime = castlesWithTime.OrderByDescending(keySelector).ToList();
                    castlesReadyToFill = castlesReadyToFill.OrderByDescending(keySelector).ToList();
                    castlesNotAvailable = castlesNotAvailable.OrderByDescending(keySelector).ToList();
                }
            }

            // 4. Объединяем группы обратно в один список и обновляем коллекцию для отображения
            var finalSortedList = castlesWithTime.Concat(castlesReadyToFill).Concat(castlesNotAvailable);

            _castlesObservable.Clear();
            foreach (var castle in finalSortedList)
            {
                _castlesObservable.Add(castle);
            }
        }

        private void UpdateDataGrid(WebSocketDataUpdate? update)
        {
            if (update?.data?.castleData == null) return;

            Dispatcher.Invoke(() =>
            {
                lock (_castlesLock)
                {
                    int castleId = update.data.castleData.id; // Получаем ID отсюда

                    var existingCastleInObservable = _castlesObservable.FirstOrDefault(c => c.id == castleId);
                    var existingCastleInOriginal = originalCastles.FirstOrDefault(c => c.id == castleId);

                    // Получаем данные из вложенного объекта
                    var castleData = update.data.castleData;

                    if (existingCastleInObservable != null)
                    {
                        existingCastleInObservable.fillingLvl = castleData.fillingLvl;
                        existingCastleInObservable.fillingSpheretime = castleData.fillingSpheretime;
                        existingCastleInObservable.commentary = castleData.commentary;
                        existingCastleInObservable.ownerClan = castleData.ownerClan;
                        existingCastleInObservable.fillingDatetime = castleData.fillingDatetime;
                    }
                    if (existingCastleInOriginal != null)
                    {
                        existingCastleInOriginal.fillingLvl = castleData.fillingLvl;
                        existingCastleInOriginal.fillingSpheretime = castleData.fillingSpheretime;
                        existingCastleInOriginal.commentary = castleData.commentary;
                        existingCastleInOriginal.ownerClan = castleData.ownerClan;
                        existingCastleInObservable.OwnerClanName = _clans.FirstOrDefault(c => c.id == castleData.ownerClan)?.name ?? castleData.ownerClan;
                        existingCastleInOriginal.fillingDatetime = castleData.fillingDatetime;
                    }

                    ApplyFilterAndSort();
                }
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
            _isUserInitiatedDisconnect = true; // <-- Ставим флаг, что это мы сами отключили
    
            if (_webSocket.State == WebSocketState.Open)
            {
                _cts.Cancel();
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Switching table", CancellationToken.None);
                }
                catch { /* ignore */ }
            }
            _webSocket?.Dispose();
        }

        private async Task ConnectAndJoinTableAsync(string tableId)
        {
            // Сбрасываем флаг, так как начинаем новое подключение
            _isUserInitiatedDisconnect = false; 
            
            // Выносим логику подключения в отдельный метод, чтобы его можно было вызывать повторно
            await ConnectToSocketLoop(tableId); 
        }

        private async Task ConnectToSocketLoop(string tableId)
        {
            // Бесконечный цикл переподключения, пока пользователь не уйдет со страницы
            while (!_isUserInitiatedDisconnect)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                }
                _cts = new CancellationTokenSource();

                try
                {
                    _webSocket = new ClientWebSocket();
                    // Подключаемся
                    await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cts.Token);
                    
                    // Отправляем Auth и Join
                    await SendWebSocketAuthMessage();
                    await SendWebSocketJoinMessage(tableId);

                    // Если это первое успешное подключение - убираем лоадер и показываем контент
                    IsSavedLoading = false;
                    SwitchToMainContent();
                    
                    StatusTextBlock.Text = ""; // Очищаем ошибки если были

                    // Слушаем сообщения (этот метод заблокирует выполнение, пока сокет жив)
                    await ReceiveWebSocketMessagesAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка WS: {ex.Message}");
                }
                finally
                {
                    // Очистка ресурсов перед следующей попыткой
                    try { _webSocket.Dispose(); } catch { }
                }

                // Если мы здесь, значит соединение разорвано или возникла ошибка.
                if (_isUserInitiatedDisconnect) break; // Если юзер нажал "Назад", выходим.

                // Иначе - пробуем подключиться снова через 3 секунды
                Dispatcher.Invoke(() => StatusTextBlock.Text = "Связь потеряна. Реконнект...");
                await Task.Delay(3000); 
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
            
            // Цикл работает пока сокет открыт и не отменено
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
                            // Сервер закрыл соединение корректно
                            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            return; // Выходим из метода, сработает цикл в ConnectToSocketLoop
                        }
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = stringBuilder.ToString();
                        // Десериализация может упасть, если JSON битый
                        try 
                        {
                            var update = JsonSerializer.Deserialize<WebSocketDataUpdate>(message);
                            UpdateDataGrid(update);
                        }
                        catch (JsonException) { /* Игнорируем битый пакет */ }
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // Отмена токена
                }
                catch (Exception)
                {
                    throw; // Пробрасываем ошибку наверх, чтобы сработал reconnect в ConnectToSocketLoop
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveWindowPosition();
            SaveButtonSettings();
            SaveColumnOrder();

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

    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
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