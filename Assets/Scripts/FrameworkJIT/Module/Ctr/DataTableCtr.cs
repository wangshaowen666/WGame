/*--------------------------------------------------------------
 * File: DataTableCtr.cs
 * Author: Wang ShaoWen
 * Time: 2026/01/07 13:27:28 
 *--------------------------------------------------------------
 */

using cfg;
using Luban;
using UnityEngine;

public class DataTableCtr : Singleton<DataTableCtr>
{
    private bool _isLoaded = false;
    // _tables下面会自动补全Tables中包含的配置表;
    private cfg.Tables _tables;
    public TbUIPanel TbUIPanel => _tables.TbUIPanel;
    
    private DataTableCtr()
    {
    }

    public void LoadTable()
    {
        if (_isLoaded)
        {
            Log.Warning("数据表已经加载，请勿重复加载");
            return;
        }
        
        _tables = new cfg.Tables(LoadByteBuf);
        _isLoaded = true;
        
        Log.Info(_tables.TbUIPanel.Get(DPnlId.MainPanel));
        Log.Info(_tables.TbUIPanel[DPnlId.SettingPanel]);
    }
    
    /// <summary>
    /// 重新加载配置表，热重载
    /// </summary>
    public void ReloadTable()
    {
        _tables = null;
        _isLoaded = false;
        LoadTable();
    }
    
    protected override void OnDispose()
    {
        _tables = null;
        _isLoaded = false;
    }
    
    private static ByteBuf LoadByteBuf(string file)
    {
        var cfg = ResMgr.Instance.LoadSync<TextAsset>($"Bin/{file}.bytes");
        return new ByteBuf(cfg.bytes);
    }
}