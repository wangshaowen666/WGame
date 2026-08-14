/*--------------------------------------------------------------
 * File: PlayerDataMgr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/13 15:00:00 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// 养成数据服务：获取/保存玩家养成数据（需登录，token 来自 AccountMgr）
/// </summary>
public class PlayerDataMgr : ManagerBase
{
    // 养成数据字段
    public int Gold { get; private set; }
    public int StageProgress { get; private set; }
    public Dictionary<string, object> TowerLevels { get; private set; } = new();

    /// <summary>是否已从服务器加载过养成数据</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// 获取养成数据（GET /data），成功后更新本地缓存
    /// </summary>
    public async UniTask<HttpResult> Load()
    {
        var headers = AuthHeaders();
        var result = await CoreMgr.Http.Get(GameConfig.LoginServerUrl + "/data", headers);

        if (result.IsSuccess && result.Data != null)
        {
            Gold = ParseInt(result.Data, "gold");
            StageProgress = ParseInt(result.Data, "stageProgress");
            TowerLevels = result.Data["towerLevels"] as Dictionary<string, object> ?? new Dictionary<string, object>();
            IsLoaded = true;
            Log.Info("养成数据加载成功: 金币", Gold, "关卡", StageProgress);
        }
        else
        {
            Log.Error("养成数据加载失败:", result.RawData);
        }
        return result;
    }

    /// <summary>
    /// 保存养成数据（POST /data）
    /// </summary>
    public async UniTask<HttpResult> Save()
    {
        var body = new Dictionary<string, object>
        {
            { "gold", Gold },
            { "stageProgress", StageProgress },
            { "towerLevels", ParseUtil.ToJson(TowerLevels) },
        };
        var headers = AuthHeaders();
        var result = await CoreMgr.Http.Post(GameConfig.LoginServerUrl + "/data", body, headers);

        if (result.IsSuccess)
            Log.Info("养成数据保存成功");
        else
            Log.Error("养成数据保存失败:", result.RawData);
        return result;
    }

    public void AddGold(int amount) => Gold += amount;

    /// <summary>构建带 token 的请求头</summary>
    private static Dictionary<string, string> AuthHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Authorization", "Bearer " + GameMgr.Account.Token }
        };
    }

    private static int ParseInt(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out var r) ? r : 0;
    }
}
