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
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DollOverlay
{

    // ---------------------------
// ResizableFrameControl (fixed)
// ---------------------------
public class ResizableFrameControl : FrameworkElement
{
    private BitmapImage? _source;
    private const int Corner = 32;
    private const int Border = 16;

    public ResizableFrameControl()
    {
        this.SnapsToDevicePixels = true;
        this.UseLayoutRounding = true;
        this.SizeChanged += (s, e) => InvalidateVisual();
    }
protected override void OnRender(DrawingContext dc)
{
    base.OnRender(dc);

    if (_source == null)
    {
        if (!TryLoadSource())
        {
            return;
        }
    }

    if (_source == null) return;

    double w = this.ActualWidth;
    double h = this.ActualHeight;

    if (!IsFinite(w) || !IsFinite(h) || w <= 0 || h <= 0)
    {
        Debug.WriteLine($"[FRAME] Skip render: bad control size w={w} h={h}");
        return;
    }

    double scale = 1;

    // === Верхняя горизонтальная секция ===
    Int32Rect TL = new(0, 0, 12, 31);
    Int32Rect TLB = new(13, 0, 54, 31);
    Int32Rect T = new(68, 0, 127, 31);
    Int32Rect TRB = new(196, 0, 46, 31);
    Int32Rect TR = new(243, 0, 12, 31);

    // === Левая вертикальная секция ===
    Int32Rect LT = new(0, 32, 12, 31);
    Int32Rect L = new(0, 65, 12, 19);
    Int32Rect LB = new(0, 307, 12, 62);

    // === Правая вертикальная секция ===
    Int32Rect RT = new(243, 32, 12, 31);
    Int32Rect R = new(243, 65, 12, 19);
    Int32Rect RB = new(243, 307, 12, 62);
    
    Int32Rect BL = new(13, 356, 51, 12);    // XY1 = 13,356; XY2 = 64,368 → width=51, height=12
    Int32Rect B = new(65, 356, 131, 12);    // XY1 = 65,356; XY2 = 196,368 → width=131, height=12
    Int32Rect BR = new(197, 356, 46, 12);   // XY1 = 197,356; XY2 = 243,368 → width=46, height=12
    
    Int32Rect C = new(13, 32, 228, 321);

    // Высота верхней полосы
    double topMaxSrcH = Math.Max(Math.Max(TL.Height, T.Height), Math.Max(TLB.Height, Math.Max(TRB.Height, TR.Height)));
    double topHeight = topMaxSrcH * scale;
    if (!IsFinite(topHeight) || topHeight <= 0)
    {
        Debug.WriteLine($"[FRAME] Skip render: invalid topHeight={topHeight}");
        return;
    }

    // ФИКСИРОВАННЫЕ размеры горизонтальных частей
    double fixedTLWidth = TL.Width * scale;
    double fixedTRWidth = TR.Width * scale;
    double fixedTWidth = T.Width * scale;
    double fixedTLBWidth = TLB.Width * scale;
    double fixedTRBWidth = TRB.Width * scale;

    // ФИКСИРОВАННЫЕ размеры вертикальных частей
    double fixedLTHeight = LT.Height * scale;
    double fixedLHeight = L.Height * scale;
    double fixedLBHeight = LB.Height * scale;
    double fixedLWidth = L.Width * scale;

    double fixedRTHeight = RT.Height * scale;
    double fixedRHeight = R.Height * scale;
    double fixedRBHeight = RB.Height * scale;
    double fixedRWidth = R.Width * scale;
    
    double fixedBLWidth = BL.Width * scale;
    double fixedBWidth = B.Width * scale;
    double fixedBRWidth = BR.Width * scale;
    double fixedBHeight = B.Height * scale;
    
    double fixedCWidth = C.Width * scale;
    double fixedCHeight = C.Height * scale;

    // --- Центральная часть (повторяется по обеим осям) ---
    double centerStartX = fixedLWidth; // Начинаем после левой рамки
    double centerEndX = w - fixedRWidth; // Заканчиваем перед правой рамкой
    double centerStartY = topHeight; // Начинаем ниже верхней рамки
    double centerEndY = h - fixedBHeight; // Заканчиваем перед нижней рамкой

    // Тайлинг по горизонтали и вертикали
    double currentCenterY = centerStartY;
    while (currentCenterY < centerEndY)
    {
        double segmentHeight = Math.Min(fixedCHeight, centerEndY - currentCenterY);
        
        double currentCenterX = centerStartX;
        while (currentCenterX < centerEndX)
        {
            double segmentWidth = Math.Min(fixedCWidth, centerEndX - currentCenterX);
            SafeDraw(dc, C, currentCenterX, currentCenterY, segmentWidth, segmentHeight);
            currentCenterX += fixedCWidth;
        }
        
        currentCenterY += fixedCHeight;
    }
    

    // --- Нижняя горизонтальная рамка ---
    double bottomY = h - fixedBHeight;

    // Левая часть нижней рамки (BL) - повторяется
    double blStartX = fixedLWidth; // Начинаем после левой рамки
    double blEndX = (w - fixedBWidth) / 2; // До начала центральной части
    double currentBlX = blStartX;

    while (currentBlX < blEndX)
    {
        double segmentWidth = Math.Min(fixedBLWidth, blEndX - currentBlX);
        SafeDraw(dc, BL, currentBlX, bottomY, segmentWidth, fixedBHeight);
        currentBlX += segmentWidth;
    }

    // Центральная часть нижней рамки (B) - фиксированная, по центру
    double centerBottomX = (w - fixedBWidth) / 2;
    SafeDraw(dc, B, centerBottomX, bottomY, fixedBWidth, fixedBHeight);

    // Правая часть нижней рамки (BR) - повторяется
    double brStartX = centerBottomX + fixedBWidth;
    double brEndX = w - fixedRWidth; // До правой рамки
    double currentBrX = brStartX;
    

    while (currentBrX < brEndX)
    {
        double segmentWidth = Math.Min(fixedBRWidth, brEndX - currentBrX);
        SafeDraw(dc, BR, currentBrX, bottomY, segmentWidth, fixedBHeight);
        currentBrX += segmentWidth;
    }

    // --- Верхние углы (фиксированные) ---
    SafeDraw(dc, TL, 0, 0, fixedTLWidth, topHeight);
    SafeDraw(dc, TR, w - fixedTRWidth, 0, fixedTRWidth, topHeight);

    // --- Центральная часть (фиксированная, по центру) ---
    double centerX = (w - fixedTWidth) / 2;
    SafeDraw(dc, T, centerX, 0, fixedTWidth, topHeight);

    // --- Левая переходная часть (TLB) ---
    double tlbStartX = fixedTLWidth;
    double tlbEndX = centerX;
    double currentTlbX = tlbStartX;
    
    while (currentTlbX < tlbEndX)
    {
        double segmentWidth = Math.Min(fixedTLBWidth, tlbEndX - currentTlbX);
        SafeDraw(dc, TLB, currentTlbX, 0, segmentWidth, topHeight);
        currentTlbX += segmentWidth;
    }

    // --- Правая переходная часть (TRB) ---
    double trbStartX = centerX + fixedTWidth;
    double trbEndX = w - fixedTRWidth;
    double currentTrbX = trbStartX;
    
    while (currentTrbX < trbEndX)
    {
        double segmentWidth = Math.Min(fixedTRBWidth, trbEndX - currentTrbX);
        SafeDraw(dc, TRB, currentTrbX, 0, segmentWidth, topHeight);
        currentTrbX += segmentWidth;
    }

    // --- Левая вертикальная рамка ---
    double leftY = topHeight; // Начинаем ниже верхней рамки
    
    // Верхняя часть левой рамки (LT)
    SafeDraw(dc, LT, 0, leftY, fixedLWidth, fixedLTHeight);
    leftY += fixedLTHeight;

    // Средняя часть левой рамки (L) - повторяется
    double lEndY = h - fixedLBHeight; // До начала нижней части
    double currentLY = leftY;
    
    while (currentLY < lEndY)
    {
        double segmentHeight = Math.Min(fixedLHeight, lEndY - currentLY);
        SafeDraw(dc, L, 0, currentLY, fixedLWidth, segmentHeight);
        currentLY += segmentHeight;
    }

    // Нижняя часть левой рамки (LB)
    SafeDraw(dc, LB, 0, h - fixedLBHeight, fixedLWidth, fixedLBHeight);

    // --- Правая вертикальная рамка ---
    double rightY = topHeight; // Начинаем ниже верхней рамки
    double rightX = w - fixedRWidth; // Правый край окна минус ширина правой рамки
    
    // Верхняя часть правой рамки (RT)
    SafeDraw(dc, RT, rightX, rightY, fixedRWidth, fixedRTHeight);
    rightY += fixedRTHeight;

    // Средняя часть правой рамки (R) - повторяется
    double rEndY = h - fixedRBHeight; // До начала нижней части
    double currentRY = rightY;
    
    while (currentRY < rEndY)
    {
        double segmentHeight = Math.Min(fixedRHeight, rEndY - currentRY);
        SafeDraw(dc, R, rightX, currentRY, fixedRWidth, segmentHeight);
        currentRY += segmentHeight;
    }

    // Нижняя часть правой рамки (RB)
    SafeDraw(dc, RB, rightX, h - fixedRBHeight, fixedRWidth, fixedRBHeight);

    // Проверяем, есть ли место для всех частей
    double requiredMinWidth = fixedTLWidth + fixedTLBWidth + fixedTWidth + fixedTRBWidth + fixedTRWidth;
    if (w < requiredMinWidth)
    {
        Debug.WriteLine($"[FRAME] Window too narrow: {w} < {requiredMinWidth}");
    }
}
    // Пытаемся синхронно загрузить изображение из папки images (рядом с exe)
    private bool TryLoadSource()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = System.IO.Path.Combine(baseDir, "images", "i_pup1.png");

            if (!File.Exists(path))
            {
                Debug.WriteLine($"[FRAME] IMAGE NOT FOUND at: {path}");
                return false;
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad; // ВАЖНО: загрузить синхронно
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            _source = bmp;

            Debug.WriteLine($"[FRAME] Loaded OK: {_source.PixelWidth}x{_source.PixelHeight} from {path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FRAME] ERROR loading image: {ex.Message}");
            _source = null;
            return false;
        }
    }

    private static bool IsFinite(double v) => !(double.IsNaN(v) || double.IsInfinity(v));

    /// <summary>
    /// Безопасная отрисовка: проверяет корректность размеров и src-координат.
    /// Не бросает исключений при некорректных входных данных, а логирует их.
    /// </summary>
    private void SafeDraw(DrawingContext dc, Int32Rect src, double destX, double destY, double destW, double destH)
    {
        // Проверка назначения размеров (учитываем NaN/Infinity)
        if (!IsFinite(destW) || !IsFinite(destH) || !IsFinite(destX) || !IsFinite(destY))
        {
            Debug.WriteLine($"[FRAME] SafeDraw SKIP: dest value not finite (X={destX},Y={destY},W={destW},H={destH}) for src ({src.X},{src.Y},{src.Width},{src.Height})");
            return;
        }
        if (destW <= 0 || destH <= 0)
        {
            Debug.WriteLine($"[FRAME] SafeDraw SKIP: dest size <= 0 (W={destW}, H={destH}) for src ({src.X},{src.Y},{src.Width},{src.Height})");
            return;
        }

        // Проверяем исходный прямоугольник на валидность
        if (src.Width <= 0 || src.Height <= 0)
        {
            Debug.WriteLine($"[FRAME] SafeDraw SKIP: src size <= 0 for src ({src.X},{src.Y},{src.Width},{src.Height})");
            return;
        }

        // Проверяем, не выходит ли src за пределы изображения
        if (_source == null)
        {
            Debug.WriteLine($"[FRAME] SafeDraw SKIP: _source == null");
            return;
        }

        if (src.X < 0 || src.Y < 0 || src.X + src.Width > _source.PixelWidth || src.Y + src.Height > _source.PixelHeight)
        {
            Debug.WriteLine($"[FRAME] SafeDraw SKIP: src rect out of bounds for src ({src.X},{src.Y},{src.Width},{src.Height}) imageSize({_source.PixelWidth}x{_source.PixelHeight})");
            return;
        }

        try
        {
            var part = new CroppedBitmap(_source, src);
            dc.DrawImage(part, new Rect(destX, destY, destW, destH));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FRAME] SafeDraw EXCEPTION: {ex.Message} src({src.X},{src.Y},{src.Width},{src.Height}) dest({destX},{destY},{destW},{destH})");
        }
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

    //---------------------------------------------------------

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string? _token;
        private string? _selectedTableId;
        private string _currentSortColumn = "DefaultSort";
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


        private Storyboard? _loadingStoryboard;
        private bool _isLoading;
        private bool _isSavedLoading;
        private double _expandedHeight; // Для хранения высоты до сворачивания
        private bool _isContentCollapsed;
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
            // Пытаемся найти окно игры, если его хэндл еще не известен
            if (_gameWindowHandle == IntPtr.Zero)
            {
                _gameWindowHandle = FindGameWindow();
            }

            // Если окно игры не найдено (например, игра не запущена), выходим
            if (_gameWindowHandle == IntPtr.Zero)
            {
                // Можно периодически пытаться найти его снова, если игра может быть запущена позже
                _gameWindowHandle = FindGameWindow();
                if (_gameWindowHandle == IntPtr.Zero) return;
            }

            // Получаем хэндл текущего активного окна в системе
            IntPtr activeWindowHandle = GetForegroundWindow();

            // Сравниваем хэндл активного окна с хэндлом окна игры
            if (activeWindowHandle == _gameWindowHandle)
            {
                // Если активно окно игры, делаем наш оверлей поверх него
                IntPtr myWindowHandle = new WindowInteropHelper(this).Handle;
                SetWindowPos(myWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        // Новая вспомогательная функция для поиска окна игры
        private IntPtr FindGameWindow()
        {
            // Ищем процесс по имени (самый надежный способ)
            Process[] processes = Process.GetProcessesByName(TargetProcessName);
            if (processes.Length > 0)
            {
                // Возвращаем хэндл главного окна найденного процесса
                return processes[0].MainWindowHandle;
            }

            // Если по имени процесса не нашли, можно попробовать найти по заголовку окна (менее надежно)
            return FindWindow(null, "Sphere");
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
            // Приводим отправителя события к типу TextBox
            if (sender is TextBox textBox)
            {
                // Стираем содержимое
                textBox.SelectAll();
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
                        FillingTimeTextBox.Focus();
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
            if (IsContentCollapsed) return;

            if (!isBackgroundOpaque)
            {
                this.Background = new SolidColorBrush(Colors.White) { Opacity = 0.01 };
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

            DataContext = this;
            _loadingStoryboard = this.FindResource("LoadingAnimation") as Storyboard;

            // Настраиваем и запускаем таймер для поддержания окна Topmost
            // ========= ИЗМЕНЕНИЕ ИНТЕРВАЛА ТАЙМЕРА =========
            _topmostTimer.Interval = TimeSpan.FromMilliseconds(500); // Было 3 секунды, стало 0.5 секунды для быстрой реакции
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
                    Height = IsContentCollapsed ? _expandedHeight : this.Height,
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
            if (TablesScrollViewer.Visibility == Visibility.Visible)
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

                    originalCastles = combinedCastles;

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
                SendWebSocketAuthMessage();
                await SendWebSocketJoinMessage(tableId);
                SwitchToMainContent();
                IsSavedLoading = false;
                await ReceiveWebSocketMessagesAsync();
            }
            catch (OperationCanceledException)
            {
                IsSavedLoading = false;
            }
            catch (Exception ex)
            {
                IsSavedLoading = false;
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