Shader "World Political Map/Unlit Earth Glow" {

	Properties {
		_GlowColor("Glow Color", Color) = (0.4, 0.3, 0.9, 1)
		_GlowIntensity("Glow Intensity", Range(0, 5)) = 2
		_GlowGrow("Glow Grow", Range(0, 2)) = 1
		_GlowStep("Glow Brightness Step", Range(0,1)) = 1
		_GlowFallOff("Glow FallOff", Range(0, 1)) = 1
		_GlowDistanceFallOff("Distance Fall Off", Range(0, 1)) = 0.5
		_SunLightDirection("Sun Light Direction", Vector) = (0,0,1)		
	}
	
	Subshader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Lighting Off
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Front
		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				
				#include "UnityCG.cginc"
				
				fixed3 _GlowColor;
				fixed _GlowGrow, _GlowStep, _GlowIntensity, _GlowDistanceFallOff, _GlowFallOff;
				float3 _SunLightDirection;
				
				struct v2f {
					float4 pos  : SV_POSITION;
					fixed4 color: COLOR0;
				};
        
				v2f vert (appdata_tan v) {
					TANGENT_SPACE_ROTATION;
					v2f o;
					
					const float epsilon = 0.00001;
					v.vertex 		   *= _GlowGrow + epsilon;
					o.pos 				= mul (UNITY_MATRIX_MVP, v.vertex);
					
					float3 n  			= mul( _Object2World, float4( v.normal, 0.0 ) ).xyz;
					float atten         = saturate (dot(normalize(_SunLightDirection), n));
					
					float3 worldPos		= mul( _Object2World, float4(0,0,0,1)).xyz;
					float d2 			= distance(_WorldSpaceCameraPos, worldPos);
					float dist			= 1+_GlowDistanceFallOff/d2;

					float3 viewDir 		= normalize(mul(rotation, ObjSpaceViewDir(v.vertex*atten)));
					float3 normal 		= normalize(mul(rotation, -v.normal));
					float d1 			= 1-dot(viewDir, normal) / (_GlowFallOff * dist);
					d1				   /= dist;
					
					float glowStrength  = _GlowStep -saturate(d1*_GlowStep);
					glowStrength 	   /= d2;
					fixed3 color 		= lerp(_GlowColor, 1, _GlowStep - d1);
					o.color			    = saturate (fixed4(color, glowStrength) * (_GlowIntensity + glowStrength));
					return o;
				 }
				
				fixed4 frag (v2f i) : COLOR {
					return i.color;
				}
			
			ENDCG
		}
	}
}