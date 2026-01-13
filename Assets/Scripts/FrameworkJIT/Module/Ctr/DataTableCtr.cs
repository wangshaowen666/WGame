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
    private cfg.Tables _tables;
    private bool _isLoaded = false;

    public TbUIPanel TbUIPanel => _tables.TbUIPanel;
    
    private DataTableCtr()
    {
    }
    
    /// <summary>
    /// 初始化方法，在单例实例化时调用
    /// </summary>
    protected override void OnInit()
    {
        // 可以在这里添加初始化逻辑
    }
    
    /// <summary>
    /// 释放资源方法，在单例被释放时调用
    /// </summary>
    protected override void OnDispose()
    {
        // 释放资源
        _tables = null;
        _isLoaded = false;
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
        var cfg = ResMgr.Instance.LoadSync<TextAsset>($"Bin/{file}.bytes");
        return new ByteBuf(cfg.bytes);
    }
    
    /// <summary>
    /// 检查数据表是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;
}