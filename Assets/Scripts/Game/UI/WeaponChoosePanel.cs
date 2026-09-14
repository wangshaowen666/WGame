/*--------------------------------------------------------------
 * File: WeaponChoosePanel.cs
 * Author: Wsw
 * Time: 2026/09/12
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using cfg;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>开局武器卡片展示数据（战斗侧查表组装，经 PanelOn userData 传入）</summary>
public class VsWeaponCard
{
    public int WeaponId; // 武器表 id（确认事件携带值）
    public string Title; // 武器名
    public string Desc;  // 效果说明
}

/// <summary>
/// 开局武器选择面板（3-1 扩展）：进入战斗即暂停逻辑，展示武器表全部武器，点选一把开局。
/// 数据流与战斗侧解耦（对齐 LevelUpPanel 范式）：首开 PanelOn 的 userData 携带 List&lt;VsWeaponCard&gt;；
/// 确认：点卡片发 VsStartWeaponChosen（携带武器 id），战斗侧 EquipStartWeapon 后关面板恢复战斗。
/// UI 结构由代码构建（prefab 只需空物体 + CanvasGroup + 本脚本 + 字体引用），卡片数量=武器表行数（动态）。
/// </summary>
public class WeaponChoosePanel : UIPanelBase
{
    [SerializeField] private TMP_FontAsset _font; // MiSans-Bold SDF（prefab 上拖引用，随面板依赖打包）

    private const int Columns = 3;        // 每行卡片数（竖屏布局）
    private const float CardWidth = 200f;
    private const float CardHeight = 300f;
    private const float CardGapX = 30f;
    private const float CardGapY = 40f;

    private readonly List<Button> _cardButtons = new();
    private readonly List<TMP_Text> _titleTexts = new();
    private readonly List<TMP_Text> _descTexts = new();
    private readonly List<RectTransform> _cardRoots = new();
    private readonly List<int> _cardIds = new(); // 与卡片下标对齐的武器表 id（Refresh 时填充）

    public override void OnInit(DUIPanel cfg)
    {
        base.OnInit(cfg);
        // 卡片在 OnOpen 按武器表行数动态构建（表内容运行期不变，一次构建随面板池化常驻）
    }

    public override void OnOpen(object userData = null)
    {
        base.OnOpen(userData);
        if (userData is List<VsWeaponCard> cards)
            Refresh(cards);
    }

    /// <summary>按卡片数据重建卡片（数量=武器表行数；已构建则仅刷新文本与显隐）</summary>
    private void Refresh(List<VsWeaponCard> cards)
    {
        if (_cardRoots.Count == 0 && cards != null && cards.Count > 0)
            BuildCards(cards.Count);

        _cardIds.Clear();
        if (cards != null)
            for (int i = 0; i < cards.Count; i++)
                _cardIds.Add(cards[i].WeaponId);

        // 网格布局（每行 Columns 个，整体居中）：列 x = 行内序 × (宽+间距X)，行 y = -行序 × (高+间距Y)
        var count = cards?.Count ?? 0;
        var rows = (count + Columns - 1) / Columns;
        var gridW = System.Math.Min(count, Columns) * CardWidth + System.Math.Max(0, System.Math.Min(count, Columns) - 1) * CardGapX;
        var gridH = rows * CardHeight + System.Math.Max(0, rows - 1) * CardGapY;
        for (int i = 0; i < _cardRoots.Count; i++)
        {
            var has = cards != null && i < count;
            _cardRoots[i].gameObject.SetActive(has);
            if (!has) continue;

            var col = i % Columns;
            var row = i / Columns;
            var x = -gridW / 2f + CardWidth / 2f + col * (CardWidth + CardGapX);
            var y = gridH / 2f - CardHeight / 2f - row * (CardHeight + CardGapY);
            _cardRoots[i].anchoredPosition = new Vector2(x, y);
            _titleTexts[i].text = cards[i].Title;
            _descTexts[i].text = cards[i].Desc;
        }
    }

    // ---- UI 构建（一次构建，池化复用）----

    private void BuildCards(int count)
    {
        var root = (RectTransform)transform;
        Stretch(root);

        // 半透明遮罩底（挡点击 + 聚焦弹窗）
        var bg = CreateImage(root, "Mask", new Color(0f, 0f, 0f, 0.7f));
        Stretch(bg.rectTransform);

        // 标题
        var title = CreateText(root, "Title", "选择开局武器", 52, Color.white);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(900f, 90f);
        titleRt.anchoredPosition = new Vector2(0f, -120f);

        // 卡片横排（居中）
        for (int i = 0; i < count; i++)
        {
            var index = i; // 闭包捕获（点击回调用）
            var cardRt = CreateImage(root, $"Card{i}", new Color(0.16f, 0.16f, 0.2f, 1f)).rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(CardWidth, CardHeight);

            var button = cardRt.gameObject.AddComponent<Button>();
            _cardButtons.Add(button);
            button.onClick.AddListener(() => OnCardClick(index));

            var cardText = CreateText(cardRt, "CardTitle", string.Empty, 34, Color.white);
            var cardTextRt = cardText.rectTransform;
            cardTextRt.anchorMin = new Vector2(0.5f, 1f);
            cardTextRt.anchorMax = new Vector2(0.5f, 1f);
            cardTextRt.sizeDelta = new Vector2(CardWidth - 30f, 90f);
            cardTextRt.anchoredPosition = new Vector2(0f, -60f);
            cardText.alignment = TextAlignmentOptions.Center;
            _titleTexts.Add(cardText);

            var desc = CreateText(cardRt, "CardDesc", string.Empty, 24, new Color(0.8f, 0.8f, 0.8f, 1f));
            var descRt = desc.rectTransform;
            descRt.anchorMin = new Vector2(0.5f, 0f);
            descRt.anchorMax = new Vector2(0.5f, 0f);
            descRt.sizeDelta = new Vector2(CardWidth - 30f, 200f);
            descRt.anchoredPosition = new Vector2(0f, 80f);
            desc.alignment = TextAlignmentOptions.Top;
            _descTexts.Add(desc);

            _cardRoots.Add(cardRt);
        }
    }

    private void OnCardClick(int index)
    {
        if (index < 0 || index >= _cardIds.Count)
            return;
        GameMgr.Event.Send(GameEvent.VsStartWeaponChosen, _cardIds[index]);
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
