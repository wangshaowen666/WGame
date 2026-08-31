/*--------------------------------------------------------------
 * File: ViewMats.cs
 * Author: Wsw
 * Time: 2026/08/31
 *--------------------------------------------------------------
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// URP 材质工厂（表现层 TD/VS 共用）：CreatePrimitive 的默认材质在 URP 真机渲染为粉色，
/// 需显式创建 URP 材质并按颜色缓存（sharedMaterial 避免实例化泄漏）。
/// shader 兜底链：URP Lit/Unlit -> 管线默认材质 -> Sprites/Default（真机 shader 只进 Addressables bundle）
/// </summary>
public static class ViewMats
{
    private static readonly Dictionary<Color, Material> s_matCache = new();

    public static Material Get(Color c)
    {
        if (s_matCache.TryGetValue(c, out var m)) return m;

        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            m = new Material(shader);
        }
        else
        {
            // 真机 Shader.Find 查不到（shader 只进 bundle）：克隆管线默认材质，否则 UGUI 兜底
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultMaterial != null)
            {
                m = new Material(rp.defaultMaterial);
            }
            else
            {
                m = new Material(Shader.Find("Sprites/Default"));
                Log.Warning("[战斗] 使用 Sprites/Default 兜底材质");
            }
        }

        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c); // Built-in 系 shader 用 _Color，不存在的属性写入无效不报错
        s_matCache[c] = m;
        return m;
    }

    public static void Set(Renderer r, Color c)
    {
        r.sharedMaterial = Get(c);
    }
}
