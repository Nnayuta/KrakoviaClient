// Shader: OutlineUnlit.shader
Shader "Unlit/OutlineUnlit"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1) // Amarelo por padrão
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
        LOD 100
        Cull Front // IMPORTANTE: Só desenha a parte de trás do modelo!

        Pass
        {
            // --- Stencil Buffer ---
            // Aqui, só desenhamos se o valor no buffer NÃO FOR 1 (Comp NotEqual).
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // Precisamos da normal para expandir o modelo
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert (appdata v)
            {
                v2f o;
                // Expande o vértice na direção da sua normal
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Retorna a cor sólida do outline
                return _OutlineColor;
            }
            ENDCG
        }
    }
}