/*--------------------------------------------------------------
 * File: DataTableCtr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/07 13:27:28 
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using cfg;
using Luban;
using UnityEngine;

public class DataTableMgr : ManagerBase
{
    private bool _isLoaded = false;
    // 导表工具自动补全下列属性
    private cfg.Tables _tables;

    public TbVSWave TbVSWave => _tables.TbVSWave;

    public TbVSStage TbVSStage => _tables.TbVSStage;
    public TbVSEnemy TbVSEnemy => _tables.TbVSEnemy;
    public TbVSWeapon TbVSWeapon => _tables.TbVSWeapon;
    public TbVSWeaponLevel TbVSWeaponLevel => _tables.TbVSWeaponLevel;
    public TbVSPassive TbVSPassive => _tables.TbVSPassive;
    public TbVSCharacter TbVSCharacter => _tables.TbVSCharacter;
    public TbVSPickup TbVSPickup => _tables.TbVSPickup;

    public TbEffect TbEffect => _tables.TbEffect;
    public TbPlane TbPlane => _tables.TbPlane;
    public TbEntity TbEntity => _tables.TbEntity;
    public TbUIPanel TbUIPanel => _tables.TbUIPanel;

    public void LoadTable()
    {
        if (_isLoaded)
        {
            Log.Warning("数据表已经加载，请勿重复加载");
            return;
        }
        
        _tables = new cfg.Tables(LoadByteBuf);
        _isLoaded = true;
    }
    
    /// <summary>
    /// 重新加载数据表
    /// </summary>
    public void ReloadTable()
    {
        _tables = null;
        _isLoaded = false;
        LoadTable();
    }
    
    // 小游戏(WebGL)下 LoadSync 加载远程bundle会死锁，表bytes必须先经 PreloadTableBytesAsync 异步预载
    private readonly Dictionary<string, byte[]> _preloadedTableBytes = new Dictionary<string, byte[]>();

    /// <summary>
    /// 配置表统一标签：在 Addressables Groups 中给所有 Bin/*.bytes 打上此标签，新增表打上同标签即可
    /// </summary>
    public const string TableLabel = "datatable";

    /// <summary>
    /// 异步预载所有配置表bytes（按标签统一加载，无需维护文件列表；键为 TextAsset.name，与 Luban loader 的文件名一致）
    /// </summary>
    public async UniTask PreloadTableBytesAsync()
    {
        var assets = await CoreMgr.Res.PreloadWithLabel<TextAsset>(TableLabel);
        if (assets == null)
            throw new System.Exception($"配置表预载失败: {TableLabel}");

        foreach (var ta in assets)
            _preloadedTableBytes[ta.name] = ta.bytes;
    }

    private ByteBuf LoadByteBuf(string file)
    {
        if (_preloadedTableBytes.TryGetValue(file, out var bytes))
            return new ByteBuf(bytes);

        // 兜底：其他平台走同步加载
        var cfg = CoreMgr.Res.LoadSync<TextAsset>($"Bin/{file}.bytes");
        return new ByteBuf(cfg.bytes);
    }
    
    /// <summary>
    /// 检查数据表是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;
}