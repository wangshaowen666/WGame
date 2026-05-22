/*--------------------------------------------------------------
 * File: DataTableCtr.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/01/07 13:27:28 
 *--------------------------------------------------------------
 */

using cfg;
using Luban;
using UnityEngine;

public class DataTableMgr : ManagerBase
{
    private bool _isLoaded = false;
    // 导表工具自动补全下列属性
    private cfg.Tables _tables;

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
    
    private static ByteBuf LoadByteBuf(string file)
    {
        var cfg = FrameworkMgr.Res.LoadSync<TextAsset>($"Bin/{file}.bytes");
        return new ByteBuf(cfg.bytes);
    }
    
    /// <summary>
    /// 检查数据表是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;
}