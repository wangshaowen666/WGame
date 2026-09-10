/*--------------------------------------------------------------
 * File: SettlePanel.cs
 * Author: Wsw
 * Time: 2026/09/09
 *--------------------------------------------------------------
 */

using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>结算展示数据（VampireView 死亡时组装快照，经 PanelOn userData 传入）</summary>
public class VsSettleData
{
    public int SurviveSec; // 存活时长（秒）
    public long KillCount; // 击杀数
    public int Level;      // 到达等级
}

/// <summary>
/// 死亡结算面板（2-11）：渲染本局统计（存活/击杀/等级）+ 返回主界面按钮。
/// 数据流与战斗侧解耦（对齐 LevelUpPanel）：首开 PanelOn 的 userData 携带 VsSettleData；
/// 返回按钮发 ProcedureExitBattle 事件，ProcedureBattle 收到后切 ProcedureChangeScene 回主界面。
/// UI 结构由代码构建（prefab 只需空物体 + CanvasGroup + 本脚本 + 字体引用）。
/// </summary>
public class SettlePanel : UIPanelBase
{
    [SerializeField] private TMP_FontAsset _font; // MiSans-Bold SDF（prefab 上拖引用，随面板依赖打包）

    private TMP_Text _statsText;

    public override void OnInit(DUIPanel cfg)
    {
        base.OnInit(cfg);
        BuildUI(); // 一次构建，池化复用
    }

    public override void OnOpen(object userData = null)
    {
        base.OnOpen(userData);
        if (userData is VsSettleData data)
            _statsText.text = $"存活 {data.SurviveSec / 60:00}:{data.SurviveSec % 60:00}\n击杀 {data.KillCount}\n等级 Lv.{data.Level}";
    }

    // ---- UI 构建（一次构建，池化复用）----

    private void BuildUI()
    {
        var root = (RectTransform)transform;
        Stretch(root);

        // 半透明遮罩底（挡点击 + 聚焦弹窗）
        var bg = CreateImage(root, "Mask", new Color(0f, 0f, 0f, 0.7f));
        Stretch(bg.rectTransform);

        // 标题
        var title = CreateText(root, "Title", "游戏结束", 52, Color.white);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(600f, 90f);
        titleRt.anchoredPosition = new Vector2(0f, -160f);

        // 统计（存活/击杀/等级，多行居中）
        _statsText = CreateText(root, "Stats", string.Empty, 36, Color.white);
        var statsRt = _statsText.rectTransform;
        statsRt.anchorMin = statsRt.anchorMax = new Vector2(0.5f, 0.5f);
        statsRt.sizeDelta = new Vector2(500f, 300f);
        statsRt.anchoredPosition = new Vector2(0f, 60f);
        _statsText.alignment = TextAlignmentOptions.Center;
        _statsText.lineSpacing = 1.6f;

        // 返回主界面按钮
        var btnRt = CreateImage(root, "BackBtn", new Color(0.16f, 0.16f, 0.2f, 1f)).rectTransform;
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(280f, 84f);
        btnRt.anchoredPosition = new Vector2(0f, -180f);
        var button = btnRt.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => GameMgr.Event.Send(GameEvent.ProcedureExitBattle));

        var btnText = CreateText(btnRt, "Text", "返回主界面", 30, Color.white);
        Stretch(btnText.rectTransform);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image CreateImage(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TMP_Text CreateText(RectTransform parent, string name, string content, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = _font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
