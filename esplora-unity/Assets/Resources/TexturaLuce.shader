// Shader per i modelli 3D con texture: colore della texture con una luce finta (direzione fissa),
// un po' di luce di fondo e un leggero alone sui bordi, così le figure si leggono anche nel buio.
// Due lati visibili e ritaglio dell'alfa (barba, capelli). Leggero per WebGL.
Shader "Esplora/TexturaLuce"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tinta", Color) = (1,1,1,1)
        _Cutoff ("Soglia alfa", Range(0,1)) = 0.5
        _Luce ("Direzione luce", Vector) = (0.4, 0.8, -0.35, 0)
        _Fondo ("Luce di fondo", Range(0,1)) = 0.5
        _Alone ("Alone sui bordi", Color) = (0.55, 0.25, 0.15, 1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Tint, _Alone; fixed _Cutoff, _Fondo; float4 _Luce;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 n : TEXCOORD1; float3 vista : TEXCOORD2; UNITY_FOG_COORDS(3) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.vista = UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex).xyz);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Tint;
                clip(c.a - _Cutoff);
                float3 n = normalize(i.n) * (facing > 0 ? 1 : -1);
                float nl = saturate(dot(n, normalize(_Luce.xyz)));
                float rim = pow(1 - saturate(dot(n, normalize(i.vista))), 3);
                c.rgb = c.rgb * (_Fondo + (1 - _Fondo) * nl) + _Alone.rgb * rim * 0.35;
                c.a = 1;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
