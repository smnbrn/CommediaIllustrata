// Mare del Purgatorio: colore dei vertici (chiaro vicino alla riva, profondo al largo), onde che scintillano
// e riflesso dell'orizzonte a distanza radente. Leggero per WebGL.
Shader "Esplora/Acqua"
{
    Properties { _Orizzonte ("Orizzonte", Color) = (0.86, 0.76, 0.82, 1) _Brillio ("Brillio", Color) = (1, 0.95, 0.85, 1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Orizzonte, _Brillio;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float3 wp : TEXCOORD0; UNITY_FOG_COORDS(1) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float w = sin(i.wp.x * 0.31 + t * 0.9) + sin(i.wp.z * 0.27 - t * 0.7) + sin((i.wp.x + i.wp.z) * 0.13 + t * 0.35)
                        + sin(i.wp.x * 0.83 - i.wp.z * 0.61 + t * 1.7) * 0.6;
                fixed3 c = i.color.rgb * (0.94 + 0.06 * w * 0.33);
                float vista = normalize(_WorldSpaceCameraPos - i.wp).y;
                float f = pow(1.0 - saturate(vista * 3.0), 4.0);
                c = lerp(c, _Orizzonte.rgb, f * 0.8);
                c += _Brillio.rgb * saturate(w - 1.9) * 0.28 * (1.0 - f);
                fixed4 col = fixed4(c, 1);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
