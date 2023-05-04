Shader "Deep Learning Image Preprocessor/CropImage" {
    Properties {
        // The input texture to crop
        _MainTex ("Texture", 2D) = "white" {}
        // A vector representing the x and y offsets for the cropping area
        _Offset ("Offset", Vector) = (0, 0, 0, 0)
        // A vector representing the width and height of the cropping area
        _Size ("Size", Vector) = (0, 0, 0, 0)
    }
    
    SubShader {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Uniform variables for the offset and size of the cropping area
            float2 _Offset;
            float2 _Size;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            // Fragment shader function
            fixed4 frag (v2f i) : SV_Target {
                // Calculate the input position based on the offset and size
                float2 inputPos = i.uv * _Size + _Offset;
                // Sample the input image and return the cropped color values
                return tex2D(_MainTex, inputPos);
            }
            ENDCG
        }
    }
}
