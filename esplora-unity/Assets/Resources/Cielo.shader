// Cielo del Purgatorio: cupola con sfumatura dall'orizzonte allo zenit e un sole all'alba.
// Si disegna dietro a tutto, senza nebbia e senza luci. Leggero per WebGL.
Shader "Esplora/Cielo"
{
    Properties
    {
        _Orizzonte ("Orizzonte", Color) = (0.86, 0.76, 0.82, 1)
        _Zenit ("Zenit", Color) = (0.42, 0.56, 0.84, 1)
        _Sole ("Direzione del sole", Vector) = (0.6, 0.16, 0.78, 0)
        _ColoreSole ("Colore del sole", Color) = (1, 0.86, 0.62, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" }
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Orizzonte, _Zenit, _ColoreSole; float4 _Sole;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (float4 v : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v);
                o.dir = v.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = saturate(d.y);
                fixed3 c = lerp(_Orizzonte.rgb, _Zenit.rgb, pow(h, 0.5));
                float s = saturate(dot(d, normalize(_Sole.xyz)));
                c += _ColoreSole.rgb * (pow(s, 600.0) * 3.0 + pow(s, 24.0) * 0.45 + pow(s, 5.0) * 0.12) * step(-0.02, d.y);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
