Shader "Sprites/GlowOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineSize ("Outline Size", Float) = 0.05
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100

        Pass
        {
            Name "OUTLINE"
            Tags {"LightMode"="Always"}

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.texcoord).a;
                if (alpha < 0.1)
            {
                float2 offsetX = float2(_OutlineSize, 0.0);
                float2 offsetY = float2(0.0, _OutlineSize);
                float surroundingAlpha = 0.0;

                surroundingAlpha += tex2D(_MainTex, i.texcoord + offsetX).a;
                surroundingAlpha += tex2D(_MainTex, i.texcoord - offsetX).a;
                surroundingAlpha += tex2D(_MainTex, i.texcoord + offsetY).a;
                surroundingAlpha += tex2D(_MainTex, i.texcoord - offsetY).a;

                if (surroundingAlpha > 0.0)
                    return _OutlineColor;
                else
                    discard;
            }
                return tex2D(_MainTex, i.texcoord) * _Color;
            }
            ENDCG
        }
    }
}
