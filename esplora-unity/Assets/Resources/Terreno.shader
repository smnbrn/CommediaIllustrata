// Shader del terreno: colore dei vertici (il colore del cerchio) modulato da una texture che si ripete.
// La texture ha tre canali: R = macchie larghe, G = grana fine, B = crepe/celle. Ogni cerchio sceglie quanto usarne
// (uv1 = peso macchie, peso grana, peso crepe, contrasto). Senza luci, visibile da due lati: leggero per WebGL e telefoni.
Shader "Esplora/Terreno"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Color ("Colore", Color) = (1,1,1,1) }
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

            sampler2D _MainTex; fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 stile : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 stile : TEXCOORD1; UNITY_FOG_COORDS(2) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                o.stile = v.stile;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 t = tex2D(_MainTex, i.uv).rgb;
                // un secondo campionamento, più grande e ruotato, rompe la ripetizione
                fixed3 t2 = tex2D(_MainTex, i.uv * 0.23 + float2(0.37, 0.61)).rgb;
                float macchie = (t.r + t2.r) * 0.5 - 0.5;
                float grana = t.g - 0.5;
                float crepe = 1.0 - t.b;
                float f = 1.0 + i.stile.w * (macchie * i.stile.x * 2.0 + grana * i.stile.y);
                f *= 1.0 - i.stile.z * crepe * 0.55;
                fixed4 c = i.color;
                c.rgb *= f;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
