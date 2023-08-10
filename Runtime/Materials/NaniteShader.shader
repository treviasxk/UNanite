Shader "Nanite" {
    Properties {
        _EdgeLength ("Edge length", Range(2,500)) = 5
        _Phong ("Phong Strengh", Range(0,1)) = 0.5
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", color) = (1,1,1,0)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 300
        
        CGPROGRAM
        #pragma surface surf Lambert vertex:dispNone tessellate:tessEdge tessphong:_Phong nolightmap
        #include "Tessellation.cginc"
        #pragma multi_compile_instancing
        #pragma instancing_options

        struct appdata {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float2 texcoord : TEXCOORD0;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        void dispNone (inout appdata v) { }

        float _Phong;
        float _EdgeLength;

        float4 tessEdge (appdata v0, appdata v1, appdata v2){
            return UnityEdgeLengthBasedTess (v0.vertex, v1.vertex, v2.vertex, _EdgeLength);
        }

        struct Input {
            float4 color : COLOR;
            float2 uv_MainTex;
        };

        fixed4 _Color;
        sampler2D _MainTex;

        void surf (Input IN, inout SurfaceOutput o) {
            half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            half4 vertexColor = IN.color;
            o.Albedo = vertexColor;
            o.Alpha = c.a;
        }


        ENDCG
    }
    FallBack "Diffuse"
}