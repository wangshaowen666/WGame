using Dapper;
using LoginServer.Models;
using Microsoft.Data.Sqlite;

namespace LoginServer.Data;

/// <summary>玩家养成数据访问层</summary>
public class PlayerProfileRepository
{
    private readonly string _connStr;

    public PlayerProfileRepository(IConfiguration config)
    {
        _connStr = config.GetConnectionString("Default") ?? "Data Source=login.db";
    }

    /// <summary>建表（幂等）</summary>
    public void InitDatabase()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS PlayerProfile (
                PlayerId INTEGER PRIMARY KEY,
                Gold INTEGER DEFAULT 0,
                StageProgress INTEGER DEFAULT 0,
                TowerLevels TEXT DEFAULT '{}',
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (PlayerId) REFERENCES Player(Id)
            );");
    }

    /// <summary>按玩家 Id 查养成数据</summary>
    public async Task<PlayerProfile?> GetByPlayerId(int playerId)
    {
        using var conn = new SqliteConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<PlayerProfile>(
            "SELECT * FROM PlayerProfile WHERE PlayerId = @id", new { id = playerId });
    }

    /// <summary>创建默认养成数据（注册时调用）</summary>
    public async Task CreateDefault(int playerId)
    {
        using var conn = new SqliteConnection(_connStr);
        await conn.ExecuteAsync(
            "INSERT INTO PlayerProfile (PlayerId) VALUES (@id)", new { id = playerId });
    }

    /// <summary>更新养成数据（整段覆盖保存）</summary>
    public async Task Update(int playerId, int gold, int stageProgress, string towerLevels)
    {
        using var conn = new SqliteConnection(_connStr);
        await conn.ExecuteAsync(@"
            UPDATE PlayerProfile
            SET Gold = @gold, StageProgress = @stage, TowerLevels = @towers,
                UpdatedAt = CURRENT_TIMESTAMP
            WHERE PlayerId = @id",
            new { id = playerId, gold, stage = stageProgress, towers = towerLevels });
    }
}
