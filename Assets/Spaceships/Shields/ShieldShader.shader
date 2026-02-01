Shader "Unlit/SpaceshipShieldShader"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _ImpactRadius ("Impact Radius", Float) = 1.0
        _Edge("Impact Edge", Float) = 0.5
        _LifeTime("Time an impact is visible", Float) = 0.25
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define MAX_IMPACTS 32

            float4 _ImpactPoints[MAX_IMPACTS];
            int _ImpactCount;
            float _ImpactRadius;
            float _Edge;
            float _LifeTime;

            sampler2D _MainTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (_ImpactCount == 0)
                    return float4(0,0,0,0);

                float3 finalColor = 0;
                float finalAlpha = 0;
                float t = _Time.y;

                for (int k = 0; k < _ImpactCount; k++)
                {
                    float3 impactPos = _ImpactPoints[k].xyz;
                    float impactTime = _ImpactPoints[k].w;

                    float age = t - impactTime;

                    // -1 means not actually an impact but an "null" element
                    if (impactTime == -1) {
                        continue;
                    }

                    // -2 means that no time is yet set so continue to show the contact point
                    if (impactTime == -2) {
                        age = 0;
                    }

                    // --- time fade (0.25s) ---
                    float timeFade = 1.0 - smoothstep(
                        _LifeTime,
                        _LifeTime + 0.25,
                        age
                    );

                    if (timeFade <= 0)
                        continue;

                    float dist = distance(i.worldPos, impactPos);

                    // --- rim-only alpha ---
                    float a;
                    float fadeStart = _ImpactRadius * (1.0 - _Edge);

                    if (dist < fadeStart)
                        a = 1.0;
                    else if (dist < _ImpactRadius)
                        a = (_ImpactRadius - dist) / (_ImpactRadius * _Edge);
                    else
                        a = 0;

                    a *= timeFade;

                    if (a > 0)
                    {
                        float2 offset = i.worldPos.xz - impactPos.xz;
                        float2 uv = (offset / _ImpactRadius) * 0.5 + 0.5;
                        uv.y = 1.0 - uv.y;

                        float4 texCol = tex2D(_MainTex, uv);

                        finalAlpha = max(finalAlpha, texCol.a * a);
                        finalColor += texCol.rgb * texCol.a * a;
                    }
                }

                finalColor = saturate(finalColor);

                return float4(finalColor, finalAlpha);
            }

            ENDCG
        }
    }
}