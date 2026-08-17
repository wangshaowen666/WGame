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
    /// 获取养成数据（GET /data，proto），成功后更新本地缓存
    /// </summary>
    public async UniTask<NetMsg.GetDataResp> Load()
    {
        var headers = AuthHeaders();
        var resp = await CoreMgr.Http.GetProto<NetMsg.GetDataResp>(GameConfig.LoginServerUrl + "/data", headers);

        if (resp.ErrorCode == NetMsg.ErrorCode.ErrorNone)
        {
            Gold = resp.Gold;
            StageProgress = resp.StageProgress;
            TowerLevels = ParseUtil.DeJson(resp.TowerLevels) as Dictionary<string, object> ?? new Dictionary<string, object>();
            IsLoaded = true;
            Log.Info("养成数据加载成功: 金币", Gold, "关卡", StageProgress);
        }
        else
        {
            Log.Error("养成数据加载失败, 错误码:", resp.ErrorCode);
        }
        return resp;
    }

    /// <summary>
    /// 保存养成数据（POST /data，proto）
    /// </summary>
    public async UniTask<NetMsg.SaveDataResp> Save()
    {
        var req = new NetMsg.SaveDataReq
        {
            Gold = Gold,
            StageProgress = StageProgress,
            TowerLevels = ParseUtil.ToJson(TowerLevels),
        };
        var headers = AuthHeaders();
        var resp = await CoreMgr.Http.PostProto<NetMsg.SaveDataReq, NetMsg.SaveDataResp>(
            GameConfig.LoginServerUrl + "/data", req, headers);

        if (resp.ErrorCode == NetMsg.ErrorCode.ErrorNone)
            Log.Info("养成数据保存成功");
        else
            Log.Error("养成数据保存失败, 错误码:", resp.ErrorCode);
        return resp;
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
}
