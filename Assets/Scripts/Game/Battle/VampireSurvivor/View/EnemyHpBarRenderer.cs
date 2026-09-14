/*--------------------------------------------------------------
 * File: EnemyHpBarRenderer.cs
 * Author: Wsw
 * Time: 2026/09/07
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人头顶血条渲染器（2-12）：全场景合并为**单个动态 Mesh**（每个敌人 = 背景 quad + 前景 quad），
/// 每渲染帧由 VampireView 喂入已插值位置的 EnemyView 列表并重建顶点。
/// 坐标系：挂 EntityRoot（旋转 90°X）下，顶点用其局部坐标——
/// 局部 XY = 精灵艺术面（局部 +y = 画面上方），血条与精灵同层（局部 z 一致，同平面无视差），
/// 顶边取 EnemyView.HeadTopLocalY（经 world bounds 换算，不依赖预制体尺寸/轴点假设）。
/// - 满血/死亡（HasHpBar=false）本帧不写顶点即隐藏（本帧 0 条时禁用渲染器）
/// - 零 GC：顶点/颜色/索引数组复用（按峰值扩容），Mesh.Clear(keepVertexLayout) 保留布局复用 buffer
/// </summary>
public class EnemyHpBarRenderer : MonoBehaviour
{
    [SerializeField] private float _barWidth = 0.8f;   // 血条宽（逻辑单位，与敌人体型同级）
    [SerializeField] private float _barHeight = 0.09f; // 血条高
    [SerializeField] private float _headPad = 0.08f;   // 与精灵头顶的间距

    private static readonly Color32 BgColor = new Color32(20, 20, 20, 255);  // 背景
    private static readonly Color32 FgColor = new Color32(90, 230, 80, 255); // 前景（当前血量）

    private Mesh _mesh;
    private MeshRenderer _renderer;

    // 复用的顶点/颜色/索引数组（容量随敌人峰值增长，一次性扩容，每帧零分配）
    private int _capacity;
    private Vector3[] _verts;
    private Color32[] _colors;
    private int[] _tris;

    private void Awake()
    {
        _renderer = gameObject.AddComponent<MeshRenderer>();
        var filter = gameObject.AddComponent<MeshFilter>();
        _mesh = new Mesh { name = "EnemyHpBars" };
        _mesh.MarkDynamic(); // 每帧改顶点，标记动态提升 CPU 上传效率
        filter.sharedMesh = _mesh;
        // 材质必须双面 + 读顶点色：血条 quad 平铺在艺术面上（法线朝地面），
        // URP Lit 会背面剔除（编辑器复现"血条看不见"）且 Lit/Unlit 不读顶点色（背景/前景无法分色）；
        // Sprites/Default 双面 + 顶点色，编辑器/真机一致（真机 shader 不进包，ViewMats 兜底链实际也落到它）
        _renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // UI 性质元素，不投影不受影
        _renderer.receiveShadows = false;
        _renderer.enabled = false;
        EnsureCapacity(16);
    }

    /// <summary>每渲染帧刷新（views 为已插值位置的 EnemyView 列表；满血/死亡不写顶点即隐藏）</summary>
    public void Refresh(IList<EnemyView> views)
    {
        if (_mesh == null) return;

        // 统计本帧需显示的血条数（每只怪一条，与顶点数区分开）
        int count = 0;
        for (int i = 0; i < views.Count; i++)
        {
            if (views[i].HasHpBar) count++;
        }

        if (count == 0)
        {
            _renderer.enabled = false; // 一个都不用画：隐藏渲染器
            return;
        }

        EnsureCapacity(count);
        int bars = WriteBars(views);
        if (bars == 0)
        {
            _renderer.enabled = false;
            return;
        }

        // Clear(false) 保留顶点布局，配合复用数组零 GC；顶点数/索引数按实际写入
        _mesh.Clear(false);
        _mesh.SetVertices(_verts, 0, bars * 8);
        _mesh.SetColors(_colors, 0, bars * 8);
        _mesh.SetTriangles(_tris, 0, bars * 12, 0);
        _renderer.enabled = true;
    }

    /// <summary>
    /// 写入血条（每条 = 背景 4 顶点 + 前景 4 顶点，前景左对齐按血量比例收窄），返回实际写入条数。
    /// 注意：条数（bar）与顶点下标（bar*8）是两个量纲，循环条件只按条数判断。
    /// </summary>
    private int WriteBars(IList<EnemyView> views)
    {
        int bar = 0;
        for (int i = 0; i < views.Count && bar < _capacity; i++)
        {
            var v = views[i];
            if (!v.HasHpBar) continue;

            var p = v.transform.localPosition;  // (x, y)=艺术面坐标，z=层级（与精灵同层）
            float y0 = v.HeadTopLocalY + _headPad; // 头顶上方（局部 +y = 画面上方）
            float y1 = y0 + _barHeight;
            var w = _barWidth * v.ScaleFactor;  // 精英放大血条宽度（3-5，与体型同步）
            float left = p.x - w * 0.5f;
            float right = left + w;
            float z = p.z;

            WriteQuad(bar * 8, left, right, y0, y1, z, BgColor);
            WriteQuad(bar * 8 + 4, left, left + w * Mathf.Clamp01(v.HpRatio), y0, y1, z, FgColor);
            bar++;
        }
        return bar;
    }

    private void WriteQuad(int i, float x0, float x1, float y0, float y1, float z, Color32 c)
    {
        _verts[i] = new Vector3(x0, y0, z);
        _verts[i + 1] = new Vector3(x1, y0, z);
        _verts[i + 2] = new Vector3(x0, y1, z);
        _verts[i + 3] = new Vector3(x1, y1, z);
        _colors[i] = c;
        _colors[i + 1] = c;
        _colors[i + 2] = c;
        _colors[i + 3] = c;
    }

    /// <summary>扩容到 count 个血条容量（索引为固定模式，扩容时一次性填充）</summary>
    private void EnsureCapacity(int count)
    {
        if (count <= _capacity) return;
        _capacity = count;

        _verts = new Vector3[count * 8];
        _colors = new Color32[count * 8];
        _tris = new int[count * 12];
        for (int i = 0; i < count; i++)
        {
            int b = i * 8;
            // quad0（背景）: 0,1,2 / 2,1,3；quad1（前景）: 4,5,6 / 6,5,7（绕序与精灵面一致，可被相机看到）
            _tris[i * 12 + 0] = b;
            _tris[i * 12 + 1] = b + 1;
            _tris[i * 12 + 2] = b + 2;
            _tris[i * 12 + 3] = b + 2;
            _tris[i * 12 + 4] = b + 1;
            _tris[i * 12 + 5] = b + 3;
            _tris[i * 12 + 6] = b + 4;
            _tris[i * 12 + 7] = b + 5;
            _tris[i * 12 + 8] = b + 6;
            _tris[i * 12 + 9] = b + 6;
            _tris[i * 12 + 10] = b + 5;
            _tris[i * 12 + 11] = b + 7;
        }
    }
}
