/*--------------------------------------------------------------
 * File: FrameAnimGpuInst.shader
 * Author: Wsw
 * Feedback: 614270423@qq.com
 * Time: 2026/08/28 10:00:00
 *--------------------------------------------------------------
 */

// GPU 序列帧动画 shader -- GPU Instancing 版（FrameAnimGpu 的逐实例属性变体，任务 6-2）：
// - 与 FrameAnimGpu 同源（帧矩形数据纹理 + 联合包围盒四边形重映射），差异仅在逐实例属性通道：
//   逐实例属性（相位偏移 _FrameOffset / 闪白 _FlashAmount）放 UNITY_INSTANCING_BUFFER，
//   由 Graphics.RenderMeshInstanced 按实例传入 -- 与 MPB 路径的区别：
//   MPB 逐实例覆盖会踢出 SRP 合批（每实例一次提交），instancing 一个 draw call 批量提交全部实例
// - 逐实例属性不能进 UnityPerMaterial CBUFFER（会与 instanced buffer 冲突），
//   故本 shader 天然不兼容 SRP Batcher -- 两条合批路径互斥，用 RenderMeshInstanced 时走本 shader
// - 顶点阶段读出逐实例值经插值器传给片元（四边形仅 4 顶点，逐实例标量在顶点读取等价且更稳）
// - 全局控制（暂停/加减速/整体染色）仍走材质属性：改共享材质的 _PlaySpeed/_BaseColor 一次全实例生效
//
// 用法（配套 FrameAnimGpuTest 的 GpuInstancing 模式）：
// - 材质须 enableInstancing = true（FrameAnimGpuUtil.CreateClip(instanced:true) 已处理）
// - C# 侧每实例一个 MaterialPropertyBlock 设 _FrameOffset/_FlashAmount，
//   随 Graphics.RenderMeshInstanced(params, pass, matrices, mpbs) 提交

Shader "Game/FrameAnimGpuInst"
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

        [Header(Instanced)]
        _FrameOffset ("相位偏移(帧,逐实例)", Float) = 0
        _FlashAmount ("闪白强度0~1(逐实例)", Float) = 0

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
            Name "FrameAnimUnlitInst"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                float  frameOffset : TEXCOORD1; // 逐实例相位（顶点读出下传）
                float  flashAmount : TEXCOORD2; // 逐实例闪白强度
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_FrameData);
            SAMPLER(sampler_FrameData);

            // 逐实例属性（instanced buffer，不进 UnityPerMaterial -- 与 SRP Batcher 互斥的原因）
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _FrameOffset)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlashAmount)
            UNITY_INSTANCING_BUFFER_END(Props)

            // 材质级（全实例共享）属性
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _FrameData_ST;
                half4  _BaseColor;
                float  _FrameCount;
                float  _Fps;
                float  _Loop;
                float  _PlaySpeed;
                float  _StartFrame;
                float  _TimeDriven;
                float  _FrameIndex;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                // 宏展开为 PropsArray[unity_InstanceID]._FrameOffset（第一参是 buffer 名，非输入结构体）
                OUT.frameOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _FrameOffset);
                OUT.flashAmount = UNITY_ACCESS_INSTANCED_PROP(Props, _FlashAmount);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float count = max(_FrameCount, 1.0);

                // 1) 当前帧号：GPU 时间驱动 + 逐实例相位（不同步播放的关键），或 CPU 手动帧
                float frame = _TimeDriven > 0.5
                    ? floor(_Time.y * max(_Fps, 0.0) * _PlaySpeed + IN.frameOffset + _StartFrame)
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

                // 3) 点采样取回该帧两个矩形（纹素中心定位：x=(帧+0.5)/帧数, y=0.25/0.75）
                float2 dataUv = float2((frame + 0.5) / count, 0.25);
                float4 quadRect  = SAMPLE_TEXTURE2D_LOD(_FrameData, sampler_FrameData, dataUv, 0);
                float4 atlasRect = SAMPLE_TEXTURE2D_LOD(_FrameData, sampler_FrameData, dataUv + float2(0.0, 0.5), 0);

                // 4) 四边形 uv -> 帧内局部 uv -> 图集 uv
                float2 local = (IN.uv - quadRect.xy) / quadRect.zw;
                if (any(local < 0.0) || any(local > 1.0))
                {
                    return half4(0.0, 0.0, 0.0, 0.0); // 联合包围盒内超出该帧区域 -> 透明
                }

                float2 atlasUv = atlasRect.xy + local * atlasRect.zw;
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, atlasUv);

                // 5) 逐实例闪白：RGB 向白收拢（保 alpha，避免闪白时透明度变化）
                c.rgb = lerp(c.rgb, half3(1.0, 1.0, 1.0), saturate(IN.flashAmount));
                return c * _BaseColor * IN.color;
            }
            ENDHLSL
        }
    }
}
