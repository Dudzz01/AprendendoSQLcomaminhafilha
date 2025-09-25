using UnityEngine;
using SQLite4Unity3d;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System;

public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection _conn;

    // ====== Tipos auxiliares (PRAGMA) ======
    // PRAGMA table_info('tbl')
    public class TableCol
    {
        public int cid { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int notnull { get; set; }
        public string dflt_value { get; set; }
        public int pk { get; set; }
    }

    // PRAGMA foreign_key_list('tbl')
    public class FkInfo
    {
        public int id { get; set; }
        public int seq { get; set; }
        public string table { get; set; }     // tabela pai
        public string from { get; set; }      // coluna local
        public string to { get; set; }        // coluna pai
        public string on_update { get; set; }
        public string on_delete { get; set; }
        public string match { get; set; }
    }

    // PRAGMA index_list('tbl') + PRAGMA index_info('idx')
    public class IdxList
    {
        public int seq { get; set; }
        public string name { get; set; }
        public int unique { get; set; }       // 1 = único
        public string origin { get; set; }
        public int partial { get; set; }
    }
    public class IdxCol
    {
        public int seqno { get; set; }
        public int cid { get; set; }
        public string name { get; set; }
    }
    public class UniqueIndexInfo
    {
        public string name;
        public List<string> columns;
    }

    void Awake()
    {
        // Recria DB sempre que abrir o jogo (como você fazia):
        var dst = Path.Combine(Application.persistentDataPath, "gameData.db");
        

        string dbFile = "gameData.db";
        string dbPath = DatabaseUtils.GetDatabasePath(dbFile);
        _conn = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        _conn.BusyTimeout = TimeSpan.FromSeconds(3);

        // MUITO IMPORTANTE: FKs ativas
        _conn.Execute("PRAGMA foreign_keys = ON;");

        Debug.Log("DB path: " + dbPath);

      
    }

    // ====== Execuções ======

    // SELECT tipado (compat/uso interno de debug)
    public List<T> ExecuteQuery<T>(string sql) where T : new()
    {
        return _conn.Query<T>(sql);
    }

    // DDL/DML — usado pelo SQLConsoleUI (retorna linhas afetadas)
    public int ExecuteNonQuery(string sql)
    {
        _conn.Execute(sql);
        // garante contagem real de mudanças (UPDATE/DELETE/INSERT). Para DDL pode retornar 0.
        var cmd = _conn.CreateCommand("SELECT changes();");
        return cmd.ExecuteScalar<int>();
    }

    // Escalar long — para validadores (contagens etc.)
    public long ScalarLong(string sql)
    {
        var cmd = _conn.CreateCommand(sql);
        return cmd.ExecuteScalar<long>();
    }

    // ====== PRAGMA helpers (para validadores dos Setups) ======

    public List<TableCol> GetTableInfo(string table)
    {
        string tbl = SafeIdent(table);
        return _conn.Query<TableCol>($"PRAGMA table_info('{tbl}')");
    }

    public List<FkInfo> GetForeignKeys(string table)
    {
        string tbl = SafeIdent(table);
        return _conn.Query<FkInfo>($"PRAGMA foreign_key_list('{tbl}')");
    }

    public List<UniqueIndexInfo> GetUniqueIndexes(string table)
    {
        string tbl = SafeIdent(table);
        var list = new List<UniqueIndexInfo>();
        var idxList = _conn.Query<IdxList>($"PRAGMA index_list('{tbl}')");
        foreach (var idx in idxList)
        {
            if (idx.unique == 1)
            {
                var cols = _conn.Query<IdxCol>($"PRAGMA index_info('{idx.name}')");
                var colsNames = new List<string>();
                foreach (var c in cols) colsNames.Add(c.name);
                list.Add(new UniqueIndexInfo { name = idx.name, columns = colsNames });
            }
        }
        return list;
    }

    // ====== Transações (se seus Setups precisarem) ======
    public void BeginTransaction() => _conn.BeginTransaction();
    public void Commit() => _conn.Commit();
    public void Rollback() => _conn.Rollback();

    // ====== Util ======
    private static string SafeIdent(string ident)
    {
        if (string.IsNullOrEmpty(ident) || !Regex.IsMatch(ident, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            throw new System.ArgumentException("Identificador inválido: " + ident);
        return ident;
    }

    void OnDestroy()
    {
        try { _conn?.Close(); } catch { }
        try { _conn?.Dispose(); } catch { }
        _conn = null;
    }
}
