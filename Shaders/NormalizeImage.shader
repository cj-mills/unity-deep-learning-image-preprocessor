Shader "Deep Learning Image Preprocessor/NormalizeImage"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Uniform arrays to hold the mean and standard deviation values for each color channel (r, g, b)
            uniform float _Mean[3];
            uniform float _Std[3];
            float _Scale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            // Fragment shader function
            float4 frag(v2f i) : SV_Target
            {
                // Sample the input image
                float4 col = tex2D(_MainTex, i.uv);
                // Normalize each color channel (r, g, b)
                col.r = ((col.r - _Mean[0]) / _Std[0])*_Scale;
                col.g = ((col.g - _Mean[1]) / _Std[1])*_Scale;
                col.b = ((col.b - _Mean[2]) / _Std[2])*_Scale;
                // Return the normalized color values
                return col;
            }
            ENDCG
        }
    }
}
