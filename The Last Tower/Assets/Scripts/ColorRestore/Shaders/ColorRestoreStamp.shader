// ColorRestoreArea 1個分のマスク画像を、マスク用テクスチャに描き込むシェーダー。
// マスク画像の白い所ほど早く色が戻る（グラデーションで「広がる順番」を描ける）。
// 境目にはワールド座標のノイズを掛けて、筆で塗ったようなギザギザにする。
Shader "Hidden/LastTower/ColorRestoreStamp"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        BlendOp Max
        Blend One One

        Pass
        {
            Name "ColorRestoreStamp"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4x4 _CR_MatrixVP;   // ColorRestoreFeature がカメラごとに設定

            float4 _MainTex_ST;
            float  _Progress;        // 0=まだ白黒, 1=すべて色が戻った
            float  _Softness;        // 境目のなめらかさ
            float  _NoiseScale;      // ノイズの細かさ（ワールド単位）
            float  _NoiseStrength;   // ノイズの強さ（筆のギザギザ）
            float  _StrokeAngle;     // 筆の向き（ラジアン）
            float  _StrokeStretch;   // 筆の向きにノイズを伸ばす量
            float  _Strength;        // このエリアの最大の色の戻り具合
            float  _Seed;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 positionWS : TEXCOORD1; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = mul(_CR_MatrixVP, float4(ws, 1.0));
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.positionWS = ws.xy;
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21) + _Seed);
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0, amp = 0.5;
                for (int k = 0; k < 4; k++) { v += ValueNoise(p) * amp; p *= 2.07; amp *= 0.5; }
                return v; // 0..~0.94
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float m = t.r * t.a;
                if (m <= 0.002) return 0;

                // 筆の向きに回転して、その方向に伸ばしたノイズ → 毛羽立った筆跡のような境目
                float s, c; sincos(_StrokeAngle, s, c);
                float2 p = float2(c * i.positionWS.x + s * i.positionWS.y, -s * i.positionWS.x + c * i.positionWS.y);
                p.x /= max(_StrokeStretch, 0.01);
                float n = (Fbm(p / max(_NoiseScale, 0.001)) - 0.47) * _NoiseStrength;

                float soft = max(_Softness, 0.001);
                float threshold = (1.0 - m) + n;                      // 白い所ほど早く戻る
                float lo = -0.5 * _NoiseStrength - 0.001;
                float hi = 1.0 + 0.5 * _NoiseStrength + soft;
                float p01 = lerp(lo, hi, saturate(_Progress));
                float reveal = saturate((p01 - threshold) / soft);

                float cover = saturate(m * 25.0);   // マスクの外周はなめらかに消す
                reveal *= cover;

                // G: 広がっている最中の「先端」だけ光らせる（戻りきったら消える）
                float front = saturate(1.0 - abs(p01 - threshold - soft * 0.5) / (soft * 0.7 + 0.01));
                front *= cover * step(_Progress, 0.999);

                return half4(reveal * _Strength, front * _Strength, 0, 0);
            }
            ENDHLSL
        }
    }
}
