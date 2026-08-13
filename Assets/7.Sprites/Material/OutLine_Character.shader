Shader "Unlit/OutLine_Character"
{
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _MainColor ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _OnOutline ("Outline (1=True)", Range (0, 1)) = 0
        _OutlineColor ("Outline Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _OutlineThickness ("Outline Thickness", Range(1, 10)) = 2.0 // 아웃라인 두께를 조절하는 변수
    }
    
    CGINCLUDE
    
    #include "UnityCG.cginc"
    
    sampler2D _MainTex;
    uniform float4 _MainTex_TexelSize;
    fixed4 _MainColor;
    fixed4 _MainTex_ST;
    
    fixed _OnOutline;
    fixed4 _OutlineColor;
    float _OutlineThickness;  // 두께 변수
    
    struct v2f {
        fixed4 pos : SV_POSITION;
        fixed2 uv : TEXCOORD0;
        fixed4 vertexColor : COLOR;
    };
    
    v2f vert(appdata_full v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);   
        o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
        o.vertexColor = v.color * _MainColor;
        return o; 
    }
    
    fixed4 frag(v2f i) : COLOR {
        if (_OnOutline == 1) {
            // 아웃라인 두께에 따른 주변 픽셀 샘플링 범위 설정
            float thickness = _OutlineThickness;  // 아웃라인 두께
            
            // 두께에 맞게 주변 픽셀을 샘플링
            fixed s00 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness, -thickness)).a;
            fixed s01 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness / 2.0, -thickness)).a;
            fixed s02 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(0, -thickness)).a;
            fixed s03 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness / 2.0, -thickness)).a;
            fixed s04 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness, -thickness)).a;

            fixed s10 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness, -thickness / 2.0)).a;
            fixed s11 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness / 2.0, -thickness / 2.0)).a;
            fixed s12 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(0, -thickness / 2.0)).a;
            fixed s13 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness / 2.0, -thickness / 2.0)).a;
            fixed s14 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness, -thickness / 2.0)).a;

            fixed s20 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness, 0)).a;
            fixed s21 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness / 2.0, 0)).a;
            fixed s22 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(0, 0)).a;
            fixed s23 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness / 2.0, 0)).a;
            fixed s24 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness, 0)).a;

            fixed s30 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness, thickness / 2.0)).a;
            fixed s31 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness / 2.0, thickness / 2.0)).a;
            fixed s32 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(0, thickness / 2.0)).a;
            fixed s33 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness / 2.0, thickness / 2.0)).a;
            fixed s34 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness, thickness / 2.0)).a;

            fixed s40 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness, thickness)).a;
            fixed s41 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(-thickness / 2.0, thickness)).a;
            fixed s42 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(0, thickness)).a;
            fixed s43 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness / 2.0, thickness)).a;
            fixed s44 = tex2D(_MainTex, i.uv + _MainTex_TexelSize * float2(thickness, thickness)).a;

            // Sobel 필터 적용 (X, Y 방향)
            fixed sobelX = s00 + 2 * s10 + 3 * s20 + 2 * s30 + s40
                         - (s04 + 2 * s14 + 3 * s24 + 2 * s34 + s44);
            fixed sobelY = s00 + 2 * s01 + 3 * s02 + 2 * s03 + s04
                         - (s40 + 2 * s41 + 3 * s42 + 2 * s43 + s44);

            fixed edgeSqr = (sobelX * sobelX + sobelY * sobelY);
            _OutlineColor.a = edgeSqr;

            // 아웃라인을 그릴 때, 경계선과 함께 투명하지 않은 부분을 강제로 아웃라인 처리
            if (tex2D(_MainTex, i.uv.xy).a < 0.5 || edgeSqr > 0.1)  // 경계선 외에도 일정 강도의 투명도를 가진 부분도 처리
                return _OutlineColor;
        }

        return tex2D(_MainTex, i.uv.xy) * i.vertexColor;
    }

    ENDCG

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            ENDCG
        }
    }
    FallBack Off
}