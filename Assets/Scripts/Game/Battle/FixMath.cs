/*--------------------------------------------------------------
 * File: FixMath.cs
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/18 16:00:00
 *--------------------------------------------------------------
 */

using System;

/// <summary>
/// Q16.16 定点数（帧同步确定性数学）：
/// - 64 位整数存储：高 48 位整数部分 + 低 16 位小数部分
/// - 精度 1/65536 ≈ 0.000015，范围 ±32767，塔防地图/速度/数值足够
/// - 纯整数运算，跨平台（x86/ARM、Mono/IL2CPP）结果完全一致；
///   float 在不同平台/编译器下可能因舍入差异导致双端画面不一致，帧同步逻辑层禁用
/// - 乘法安全范围：两操作数绝对值之和不超过约 2^31（塔防数值远小于此）
/// </summary>
public readonly struct Fix : IEquatable<Fix>
{
    public const int FractionBits = 16;
    public const long OneRaw = 1L << FractionBits;

    public readonly long Raw;

    private Fix(long raw)
    {
        Raw = raw;
    }

    public static readonly Fix Zero = new Fix(0);
    public static readonly Fix Half = new Fix(OneRaw / 2);
    public static readonly Fix One = new Fix(OneRaw);

    /// <summary>从整数构造</summary>
    public static Fix FromInt(int v) => new Fix((long)v << FractionBits);

    /// <summary>从原始定点值构造（仅序列化/调试用）</summary>
    public static Fix FromRaw(long raw) => new Fix(raw);

    /// <summary>截断取整数部分</summary>
    public int Int => (int)(Raw >> FractionBits);

    /// <summary>转 float（仅表现层使用，逻辑层禁用）</summary>
    public float AsFloat => Raw / (float)OneRaw;

    public static Fix operator +(Fix a, Fix b) => new Fix(a.Raw + b.Raw);
    public static Fix operator -(Fix a, Fix b) => new Fix(a.Raw - b.Raw);
    public static Fix operator -(Fix a) => new Fix(-a.Raw);

    public static Fix operator *(Fix a, Fix b) => new Fix((a.Raw * b.Raw) >> FractionBits);

    public static Fix operator /(Fix a, Fix b) => new Fix((a.Raw << FractionBits) / b.Raw);

    public static bool operator ==(Fix a, Fix b) => a.Raw == b.Raw;
    public static bool operator !=(Fix a, Fix b) => a.Raw != b.Raw;
    public static bool operator <(Fix a, Fix b) => a.Raw < b.Raw;
    public static bool operator >(Fix a, Fix b) => a.Raw > b.Raw;
    public static bool operator <=(Fix a, Fix b) => a.Raw <= b.Raw;
    public static bool operator >=(Fix a, Fix b) => a.Raw >= b.Raw;

    public bool Equals(Fix other) => Raw == other.Raw;
    public override bool Equals(object obj) => obj is Fix f && Raw == f.Raw;
    public override int GetHashCode() => Raw.GetHashCode();
    public override string ToString() => (Raw / (double)OneRaw).ToString("F4");

    /// <summary>
    /// 平方根（整数位扫描法，纯整数运算确定性）。
    /// 输入范围：a >= 0 且 a.Raw ≤ 2.8e14（内部左移 16 位不溢出 ulong；
    /// 本工程距离平方最大约 2e12，余量 100 倍以上）
    /// </summary>
    public static Fix Sqrt(Fix a)
    {
        if (a.Raw <= 0) return Zero;

        // sqrt(a) 的定点原始值 = isqrt(a.Raw << 16)
        var num = (ulong)a.Raw << FractionBits;
        ulong res = 0;
        var bit = 1UL << 62; // 从最高可能的平方位开始
        while (bit > num) bit >>= 2;
        while (bit != 0)
        {
            if (num >= res + bit)
            {
                num -= res + bit;
                res = (res >> 1) + bit;
            }
            else
            {
                res >>= 1;
            }
            bit >>= 2;
        }
        return new Fix((long)res);
    }
}

/// <summary>
/// 确定性随机数（xorshift64*）：
/// 纯 64 位整数运算，同种子在任意平台产生完全一致的序列。
/// 种子由战斗开始消息统一下发（阶段 5 暂用常量种子），双端各自演算结果相同。
/// 禁止在战斗逻辑中使用 UnityEngine.Random / System.Random
/// </summary>
public class XRng
{
    private ulong _state;

    public XRng(ulong seed)
    {
        _state = seed;
        if (_state == 0) _state = 0x9E3779B97F4A7C15UL; // 全零状态会导致序列退化
    }

    /// <summary>下一个 64 位随机数</summary>
    public ulong NextULong()
    {
        _state ^= _state >> 12;
        _state ^= _state << 25;
        _state ^= _state >> 27;
        return _state * 2685821657736338717UL;
    }

    /// <summary>[min, max) 均匀随机整数</summary>
    public int NextInt(int min, int max) => min + (int)(NextULong() % (ulong)(max - min));
}
