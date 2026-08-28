/*--------------------------------------------------------------
 * File: FrameAnimGpu.shader
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/27 11:10:00
 *--------------------------------------------------------------
 */

// GPU 序列帧动画 shader（URP 手写 HLSL）-- 支持不规则尺寸帧的大图（紧致裁切、任意 rect/pivot）：
// - 原理：帧矩形数据纹理 _FrameData 记录每帧"四边形上的区域"与"图集里的区域"（均为图集 uv），
//   shader 内按 _Time 算出当前帧号，点采样取回该帧两个矩形，把四边形 uv 重映射到图集后采样；
//   CPU 每帧零动画成本，同一材质的实例相互兼容 SRP Batcher（所有标量属性都在 UnityPerMaterial 内）
// - 四边形 = 全部帧的联合包围盒（按各自 pivot 对齐，由 FrameAnimGpuUtil.CreateClip 构建），
//   小于包围盒的帧在盒内以透明边距呈现，不拉伸不变形
// - 均匀网格大图是本方案的特例（各帧矩形恰好排成网格），无需区分处理
// - 与 CPU 方案（FrameAnimPlayer 逐帧换 Sprite 引用）对比：用 GPU 每像素几次 ALU + 两次数据纹理点采样，
//   换掉 C# 逐帧时间推进 + 换帧
//
// _FrameData 格式（RGBAFloat，宽 = 总帧数，高 = 2，必须 Point 过滤 + Clamp 寻址，运行时由工具生成）：
//   行 0（v=0.25）：该帧在四边形上的区域 (x, y, w, h) -- 图集 uv 空间
//   行 1（v=0.75）：该帧在图集里的区域 (x, y, w, h) -- 图集 uv 空间
//
// 注意：
// 1. GPU 时间驱动无法暂停 / 逐帧步进 / 播完回调（CPU 感知不到进度）；需要精确控制时关闭"GPU 时间驱动"，
//    由 CPU 设置"手动帧"（逐实例建议走 MaterialPropertyBlock，注意会破坏 SRP Batcher）
// 2. 真机构建需 shader 进包：Project Settings/Graphics -> Always Included Shaders，
//    或由打进 bundle 的材质引用本 shader（Shader.Find 按名字查找）
// 3. 双线性/多级渐远处采样会在帧矩形边缘轻微串到相邻帧：美术可在每帧矩形内留 1~2px 透明边距或关闭 mipmap
// 4. RGBAFloat 数据纹理需 GLES3 / Metal / Vulkan / DX11+（项目移动端 ETC2 已是 GLES3 起），点采样即可
// 5. 逐实例差异（相位/闪白/变速）用"每实例材质克隆"承载：SRP Batcher 按 shader 合批，材质属性值不同不破批
//    （实测 4 材质 SetPass 恒 15）；破批的只有逐实例 MaterialPropertyBlock

Shader "Game/FrameAnimGpu"
{
    Properties
    {
        [NoScaleOffset] [MainTexture] _BaseMap ("序列帧大图(紧致排布)", 2D) = "white" {}
        [NoScaleOffset] _FrameData ("帧矩形数据(运行时生成, Point/Clamp)", 2D) = "black" {}
        [MainColor] _BaseColor ("染色", Color) = (1, 1, 1, 1)

        _FrameCount ("总帧数", Float) = 8

        [Header(Play)]
        _Fps ("帧率", Float) = 10
        [Toggle] _Loop ("循环", Float) = 1
        _PlaySpeed ("播放速度", Float) = 1
        _StartFrame ("起始帧", Float) = 0
        _FrameOffset ("相位偏移(帧)", Float) = 0
        _FlashAmount ("闪白强度0~1(逐材质)", Float) = 0

        [Header(Drive)]
        [Toggle] _TimeDriven ("GPU时间驱动", Float) = 1
        _FrameIndex ("手动帧(GPU时间驱动关闭时生效)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FrameAnimUnlit"
            // SRPDefaultUnlit：与无 LightMode 标签的 pass 等价，URP 默认 DrawObjects 会绘制（Unlit 同路径）
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR; // SpriteRenderer.color 顶点色
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_FrameData);
            SAMPLER(sampler_FrameData);

            // 所有材质标量属性进 UnityPerMaterial -> SRP Batcher 兼容
            // （_BaseMap_ST / _FrameData_ST 声明仅为保持 CBUFFER 布局统一，tiling/offset 不参与采样）
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _FrameData_ST;
                half4  _BaseColor;
                float  _FrameCount;
                float  _Fps;
                float  _Loop;
                float  _PlaySpeed;
                float  _StartFrame;
                float  _FrameOffset;
                float  _FlashAmount;
                float  _TimeDriven;
                float  _FrameIndex;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float count = max(_FrameCount, 1.0);

                // 1) 当前帧号：GPU 时间驱动（默认）或 CPU 手动帧
                float frame = _TimeDriven > 0.5
                    ? floor(_Time.y * max(_Fps, 0.0) * _PlaySpeed + _FrameOffset + _StartFrame)
                    : floor(_FrameIndex);

                // 2) 循环：非负取模（相位偏移可能为负）；非循环：钳到最后一帧
                if (_Loop > 0.5)
                {
                    frame = frame - floor(frame / count) * count;
                }
                else
                {
                    frame = clamp(frame, 0.0, count - 1.0);
                }

                // 3) 点采样取回该帧两个矩形（纹素中心定位：x=(帧+0.5)/帧数, y=0.25/0.75；数据纹理无 mipmap，lod 固定 0）
                float2 dataUv = float2((frame + 0.5) / count, 0.25);
                float4 quadRect  = SAMPLE_TEXTURE2D_LOD(_FrameData, sampler_FrameData, dataUv, 0);
                float4 atlasRect = SAMPLE_TEXTURE2D_LOD(_FrameData, sampler_FrameData, dataUv + float2(0.0, 0.5), 0);

                // 4) 四边形 uv -> 帧内局部 uv -> 图集 uv（四边形 Sprite 的 rect 在图集原点，故两者同处图集 uv 空间）
                float2 local = (IN.uv - quadRect.xy) / quadRect.zw;
                if (any(local < 0.0) || any(local > 1.0))
                {
                    return half4(0.0, 0.0, 0.0, 0.0); // 联合包围盒内超出该帧区域 -> 透明
                }

                float2 atlasUv = atlasRect.xy + local * atlasRect.zw;
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, atlasUv);

                // 5) 闪白：RGB 向白收拢（保 alpha，避免闪白时透明度变化）；逐实例差异走"每实例材质克隆"设本值
                c.rgb = lerp(c.rgb, half3(1.0, 1.0, 1.0), saturate(_FlashAmount));
                return c * _BaseColor * IN.color;
            }
            ENDHLSL
        }
    }
}
