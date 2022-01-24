Shader "Unlit/Molten"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_NoiseTex("Noise Texture", 2D) = "white" {}
		_Resolution("Resolution", float) = 256
		_Color("Color", Color) = (1, 0.9, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
				
            struct v2f
            {
				float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
			sampler2D _NoiseTex;
			float4 _MainTex_ST;
			float _Resolution;
			float4 _Color;

			float noise(float3 x)
			{
				float3 p = floor(x);
				float3 f = frac(x);
				f = smoothstep(0.0, 1.0, f);

				float2 uv = (p.xy + float2(37.0, 17.0) * p.z) + f.xy;
				float2 rg = tex2D(_NoiseTex, (uv + 0.5) / 256.0).yx;

				return lerp(rg.x, rg.y, f.z) * 2.0 - 1.0;
			}

			float2 swirl(float2 p)
			{
				return float2(noise(float3(p.xy, 0.33)), noise(float3(p.yx, 0.66)));
			}

            v2f vert (
				float4 vertex : POSITION,
				float2 uv : TEXCOORD0,
				out float4 outpos : SV_POSITION
			)
            {
                v2f o;
				o.uv = uv;
				outpos = UnityObjectToClipPos(vertex);
                return o;
            }

            fixed4 frag (v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : SV_Target
            {
				float2 coord = screenPos.xy / _Resolution * float2(2, -2);
				float4 col = _Color;
				col += tex2D(_MainTex, col + float2(0.01, 0.01) * swirl(i.uv * 6.66 + float2(1, 1) * _Time[3] * 0.33));
				col += tex2D(_MainTex, col + float2(0.01, 0.01) * swirl(i.uv * 6.66 + float2(1, 1) * _Time[3] * 0.33));
				col += tex2D(_MainTex, col + float2(0.01, 0.01) * swirl(i.uv * 6.66 + float2(1, 1) * _Time[3] * 0.33));

                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
