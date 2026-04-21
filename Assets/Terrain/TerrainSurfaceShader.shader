Shader "Custom/TerrainSurfaceShader"
{
    Properties
    {
        _LightColor ("Light Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog 
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_FOG_COORDS(1)
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1.0));
                //o.pos = UnityObjectToClipPos(v.vertex);

                // transform normal to world space
                o.normal = UnityObjectToWorldNormal(v.normal);

                o.color = v.color;

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // normalize interpolated normal
                float3 normal = normalize(i.normal);

                // main directional light direction
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // Lambert diffuse
                float NdotL = max(0, dot(normal, lightDir));

                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * i.color.rgb;

                // final color = vertex color * light intensity
                fixed3 diffuse = i.color.rgb * _LightColor0.rgb * NdotL;

                fixed4 color = fixed4(ambient + diffuse, 1.0);

                UNITY_APPLY_FOG(i.fogCoord, color);

                return color;
            }
            ENDCG
        }
    }
}