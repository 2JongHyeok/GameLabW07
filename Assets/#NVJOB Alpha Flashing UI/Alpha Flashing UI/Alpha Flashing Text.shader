// Copyright (c) 2016 Unity Technologies. MIT license - license_unity.txt
// #NVJOB Alpha Flashing UI. MIT license - license_nvjob.txt
// #NVJOB Nicholas Veselov - https://nvjob.github.io
// #NVJOB Alpha Flashing UI v2.3 - https://nvjob.github.io/unity/alpha-flashing-text
// Patrons - https://nvjob.github.io/patrons
//
// ## 수정: Unity UI 마스킹 지원 추가 ##


Shader "#NVJOB/Alpha Flashing UI/ Text (Masking Fixed)" {


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


Properties{
//----------------------------------------------
    
[Header(Color Settings)][Space(5)]
[HDR]_Color("Text Color", Color) = (1,1,1,1)
[PerRendererData]_MainTex("Font Texture", 2D) = "white" {}
[Header(Blink)][Space(5)]
_Speed("Blink rate", float) = 1.0
_VectorValue("Alpha Vector Value", Vector) = (1, 1, 0.8, 0.2)
[Space(5)]
[Toggle(UNSCALED_TIME_ON)]
_UnscaledTime("Unscaled Time On", int) = 0
[Header(Other Settings)][Space(5)]
_Saturation("Saturation", Range(0, 5)) = 1
_Brightness("Brightness", Range(0, 5)) = 1
_Contrast("Contrast", Range(0, 5)) = 1

//----------------------------------------------

// ## 1. 마스킹을 위한 프로퍼티 추가 ##
// 유니티의 RectMask2D 컴포넌트가 이 변수를 자동으로 채워줍니다.
[HideInInspector] _ClipRect ("Clipping Rect", Vector) = (-1,-1,-1,-1)

//----------------------------------------------
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


SubShader{
///////////////////////////////////////////////////////////////////////////////////////////////////////////////

Tags {
"Queue"="Transparent"
"IgnoreProjector"="True"
"RenderType"="Transparent"
"PreviewType"="Plane"
}

Lighting Off Cull Off ZTest Always ZWrite Off
Blend SrcAlpha OneMinusSrcAlpha

///////////////////////////////////////////////////////////////////////////////////////////////////////////////

Pass {
//----------------------------------------------

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
#pragma shader_feature UNSCALED_TIME_ON
#include "UnityCG.cginc"

// ## 2. 유니티 UI 헬퍼 파일 포함 ##
// (UnityGet2DClipping 함수 등을 사용하기 위해)
#include "UnityUI.cginc" 

//----------------------------------------------

sampler2D _MainTex;
uniform float4 _MainTex_ST;
uniform fixed4 _Color;
fixed _Saturation, _Contrast, _Brightness;
half _Speed;
half4 _VectorValue;
float timeCh, _NV_AF_Time;

// ## 3. RectMask2D가 사용할 클리핑 사각형 변수 ##
uniform float4 _ClipRect;

//----------------------------------------------

struct appdata_t {
float4 vertex : POSITION;
fixed4 color : COLOR;
float2 texcoord : TEXCOORD0;
UNITY_VERTEX_INPUT_INSTANCE_ID
};

//----------------------------------------------

struct v2f {
float4 vertex : SV_POSITION;
fixed4 color : COLOR;
float2 texcoord : TEXCOORD0;
// ## 4. 픽셀의 월드 좌표를 프래그먼트 쉐이더로 전달 ##
float4 worldPosition : TEXCOORD1; 
UNITY_VERTEX_OUTPUT_STEREO
};

//----------------------------------------------

v2f vert (appdata_t v){
v2f o;
UNITY_SETUP_INSTANCE_ID(v);
UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
o.vertex = UnityObjectToClipPos(v.vertex);
o.color = v.color * _Color;
o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

// ## 5. UI 오브젝트의 월드 좌표를 저장 ##
o.worldPosition = v.vertex; 

return o;
}

//----------------------------------------------

fixed4 frag (v2f i) : SV_Target{
fixed4 col = i.color;
fixed Lum = dot(col, float3(0.2126, 0.7152, 0.0722));
fixed4 color = lerp(Lum.xxxx, col, _Saturation);
color = color * _Brightness;
color = (color - 0.5) * _Contrast + 0.5;
half2 wpNorm = i.texcoord;
half vectorSum = (sin(wpNorm.x) * _VectorValue.x) + (sin(wpNorm.y) * _VectorValue.y);
#ifdef UNSCALED_TIME_ON
timeCh = _NV_AF_Time;
#else
timeCh = _Time.y;
#endif
half sinTime = sin(vectorSum + (timeCh * _Speed)) * _VectorValue.w;
fixed cola = abs(sinTime) + _VectorValue.z;
color.a *= tex2D(_MainTex, i.texcoord).a * cola;

// ## 6. 마스킹(클리핑) 연산 수행 ##
// 픽셀의 월드 좌표가 _ClipRect(마스크 영역) 밖에 있다면,
// 이 픽셀의 알파(a) 값을 0으로 만들어 숨깁니다.
color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

return color;
}

//----------------------------------------------

ENDCG
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
}
