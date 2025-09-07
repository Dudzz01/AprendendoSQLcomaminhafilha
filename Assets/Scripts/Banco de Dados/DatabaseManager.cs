using UnityEngine;
using SQLite4Unity3d;
using System.Collections.Generic;
using System.IO;

public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection _connection;

    void Awake()
    {
        var dst = Path.Combine(Application.persistentDataPath, "gameData.db");
        if (File.Exists(dst)) File.Delete(dst);

        string dbFile = "gameData.db";
        string dbPath = DatabaseUtils.GetDatabasePath(dbFile);
        _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        Debug.Log("DB path: " + dbPath);

        var tables = _connection.Query<TableInfo>(
    "SELECT name FROM sqlite_master WHERE type='table';"
);
        foreach (var t in tables)
            Debug.Log("Tabela: " + t.name);
    }

    public List<T> ExecuteQuery<T>(string sql) where T : new()
    {
        return _connection.Query<T>(sql);
    }

    void OnDestroy()
    {
        _connection.Close();
    }
}

