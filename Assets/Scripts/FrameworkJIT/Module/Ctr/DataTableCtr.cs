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

    public TbUIPanel TbUIPanel => _tables.TbUIPanel;
    
    private DataTableCtr()
    {
    }

    public void LoadTable()
    {
        _tables = new cfg.Tables(LoadByteBuf);
        
        Log.Info(_tables.TbUIPanel.Get(DPnlId.MainPanel));
        Log.Info(_tables.TbUIPanel[DPnlId.SettingPanel]);
    }
    
    private static ByteBuf LoadByteBuf(string file)
    {
        var cfg = ResMgr.Instance.LoadSync<TextAsset>($"Bin/{file}.bytes");
        return new ByteBuf(cfg.bytes);
    }
}