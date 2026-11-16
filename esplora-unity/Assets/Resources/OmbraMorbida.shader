// Ombra finta sotto le figure: disco scuro con bordo sfumato, sovrapposto con la trasparenza. Leggero per WebGL.
Shader "Esplora/OmbraMorbida"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} _Forza ("Forza", Range(0,1)) = 0.6 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed _Forza;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag (v2f i) : SV_Target { return fixed4(0, 0, 0, tex2D(_MainTex, i.uv).a * _Forza); }
            ENDCG
        }
    }
}
