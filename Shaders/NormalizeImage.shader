Shader "Deep Learning Image Preprocessor/NormalizeImage"
{
    Properties
    {
        // The input image texture
        _MainTex("Texture", 2D) = "white" {}
        // A vector representing the mean of the color channels (r, g, b, a).
        _Mean("Mean", Vector) = (0, 0, 0, 0)
        // A vector representing the standard deviation of the color channels (r, g, b, a).
        _Std("Std", Vector) = (1, 1, 1, 1)
        // A float range to control the scaling of the output color values.
        _Scale("Scale", Range(0, 10)) = 1
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

            // Uniform variables to hold the mean and standard deviation values for each color channel (r, g, b)
            float4 _Mean;
            float4 _Std;
            float _Scale;

            // Contains the vertex position and texture coordinates
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // Contains the transformed vertex position and texture coordinates
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                // Transform the input vertex position to clip space
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Copy the input texture coordinates to the output structure
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            // Fragment shader function
            float4 frag(v2f i) : SV_Target
            {
                // Sample the input image
                float4 col = tex2D(_MainTex, i.uv);
                // Normalize each color channel (r, g, b) and scale
                col.rgb = ((col.rgb - _Mean.rgb) / _Std.rgb) * _Scale;
                // Return the normalized color values
                return col;
            }
            ENDCG
        }
    }
}
