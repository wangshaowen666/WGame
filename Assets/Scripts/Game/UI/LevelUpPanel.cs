/*--------------------------------------------------------------
 * File: LevelUpPanel.cs
 * Author: Wsw
 * Time: 2026/09/04
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>选牌卡片展示数据（VampireView 组装，面板纯渲染）：标题=名字+等级信息，描述=效果说明</summary>
public class VsLevelUpCard
{
    public string Title;
    public string Desc;
}

/// <summary>
/// 升级选牌面板（2-8）：渲染强化选项卡，点击卡片即确认。
/// 数据流与战斗侧解耦（互不持有引用，走 GameEvent）：
///   首开：PanelOn 的 userData 携带 List&lt;VsLevelUpCard&gt;；连升多组换题：VsLevelUpChoicesChanged 事件携带新列表；
///   确认：点击卡片发 VsLevelUpChosen（携带下标），战斗侧应用选择并决定换题/关面板恢复战斗。
/// UI 结构由代码构建（prefab 只需空物体 + CanvasGroup + 本脚本 + 字体引用）。
/// </summary>
public class LevelUpPanel : UIPanelBase
{
    [SerializeField] private TMP_FontAsset _font; // MiSans-Bold SDF（prefab 上拖引用，随面板依赖打包）

    private const int CardCount = 3;
    private const float CardWidth = 280f;
    private const float CardHeight = 400f;
    private const float CardGap = 60f;

    private readonly List<Button> _cardButtons = new();
    private readonly List<TMP_Text> _titleTexts = new();
    private readonly List<TMP_Text> _descTexts = new();

    public override void OnInit(DUIPanel cfg)
    {
        base.OnInit(cfg);
        BuildUI();
        // 面板对象池化常驻（OnInit 仅首次加载调用一次），事件注册随实例常驻；隐藏期间收到刷新仅更新数据，无害
        GameMgr.Event.Register<List<VsLevelUpCard>>(GameEvent.VsLevelUpChoicesChanged, OnChoicesChanged);
    }

    public override void OnOpen(object userData = null)
    {
        base.OnOpen(userData);
        if (userData is List<VsLevelUpCard> cards)
            Refresh(cards);
    }

    /// <summary>连升多组换题：战斗侧应用选择后推送新选项列表</summary>
    private void OnChoicesChanged(List<VsLevelUpCard> cards)
    {
        Refresh(cards);
    }

    /// <summary>刷新选项卡（下标即 VsLevelUpChosen 事件携带值）</summary>
    private void Refresh(List<VsLevelUpCard> cards)
    {
        for (int i = 0; i < _cardButtons.Count; i++)
        {
            var has = cards != null && i < cards.Count;
            _cardButtons[i].gameObject.SetActive(has);
            if (!has) continue;
            _titleTexts[i].text = cards[i].Title;
            _descTexts[i].text = cards[i].Desc;
        }
    }

    // ---- UI 构建（一次构建，池化复用）----

    private void BuildUI()
    {
        var root = (RectTransform)transform;
        Stretch(root);

        // 半透明遮罩底（挡点击 + 聚焦弹窗）
        var bg = CreateImage(root, "Mask", new Color(0f, 0f, 0f, 0.7f));
        Stretch(bg.rectTransform);

        // 标题：锚面板中心，放在三张卡片上方（卡片顶 y=200，标题高 90，留 30 间距）
        var title = CreateText(root, "Title", "升级！选择一项强化", 52, Color.white);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(900f, 90f);
        titleRt.anchoredPosition = new Vector2(0f, 275f);

        // 卡片横排（居中）
        var totalWidth = CardCount * CardWidth + (CardCount - 1) * CardGap;
        for (int i = 0; i < CardCount; i++)
        {
            var index = i; // 闭包捕获（点击回调用）
            var cardRt = CreateImage(root, $"Card{i}", new Color(0.16f, 0.16f, 0.2f, 1f)).rectTransform;
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(CardWidth, CardHeight);
            cardRt.anchoredPosition = new Vector2(-totalWidth / 2f + CardWidth / 2f + i * (CardWidth + CardGap), 0f);

            var button = cardRt.gameObject.AddComponent<Button>();
            _cardButtons.Add(button);
            button.onClick.AddListener(() => GameMgr.Event.Send(GameEvent.VsLevelUpChosen, index));

            var cardText = CreateText(cardRt, "CardTitle", string.Empty, 34, Color.white);
            var cardTextRt = cardText.rectTransform;
            cardTextRt.anchorMin = new Vector2(0.5f, 1f);
            cardTextRt.anchorMax = new Vector2(0.5f, 1f);
            cardTextRt.sizeDelta = new Vector2(CardWidth - 30f, 140f);
            cardTextRt.anchoredPosition = new Vector2(0f, -85f);
            cardText.alignment = TextAlignmentOptions.Center;
            _titleTexts.Add(cardText);

            var desc = CreateText(cardRt, "CardDesc", string.Empty, 26, new Color(0.8f, 0.8f, 0.8f, 1f));
            var descRt = desc.rectTransform;
            descRt.anchorMin = new Vector2(0.5f, 0f);
            descRt.anchorMax = new Vector2(0.5f, 0f);
            descRt.sizeDelta = new Vector2(CardWidth - 30f, 220f);
            descRt.anchoredPosition = new Vector2(0f, 120f);
            desc.alignment = TextAlignmentOptions.Top;
            _descTexts.Add(desc);
        }
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
