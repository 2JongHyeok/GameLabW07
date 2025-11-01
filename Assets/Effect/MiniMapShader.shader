Shader "Custom/MinimapSilhouette"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1) // 인스펙터에서 색상을 바꿀 수 있게 합니다.
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" } // (중요!)
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color; // Properties에서 선언한 _Color

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 모든 픽셀을 _Color로 칠합니다.
                return _Color;
            }
            ENDCG
        }
    }
}