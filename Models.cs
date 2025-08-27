using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    public List<TableInfo>? tables { get; set; }
}

public class TableInfo
{
    [JsonPropertyName("id")] // Или то имя, которое приходит от сервера, например "tableId"
    public string id { get; set; }

    [JsonPropertyName("tableName")] // Или то имя, которое приходит от сервера, например "tableName"
    public string tableName { get; set; }
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

// Модели для данных таблицы (GET-запрос)
public class TableContentResponse
{
    public bool success { get; set; }
    public TableContentData? data { get; set; }
}

public class TableContentData
{
    public TableDetails? dynamic { get; set; }
}

public class TableDetails
{
    public string? id { get; set; }
    public List<Castle>? castles { get; set; }
}

public class Castle
{
    public int id { get; set; }
    public int lvl { get; set; }
    public string? nameRu { get; set; }
    public string? continentNameRu { get; set; }
    public string? fillingSpheretime { get; set; }
    public int? fillingLvl { get; set; }
    public string? ownerClan { get; set; }
    public string? commentary { get; set; }
}