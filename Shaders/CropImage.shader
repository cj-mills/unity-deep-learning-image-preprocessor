Shader "Deep Learning Image Preprocessor/CropImage"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Offset("Offset", Vector) = (0, 0, 0, 0)
        _Size("Size", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert
        #pragma target 3.0

        sampler2D _MainTex;
        float2 _Offset;
        float2 _Size;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 inputPos = IN.uv_MainTex * _Size + _Offset;
            o.Albedo = tex2D(_MainTex, inputPos).rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
