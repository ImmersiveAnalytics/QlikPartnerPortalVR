Shader "World Political Map/Unlit Earth Scenic Scatter" 
{
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}
		_BumpAmount ("Bump Amount", Range(0, 1)) = 0.5
		_CloudMap ("Cloud Map", 2D) = "black" {}
		_CloudSpeed ("Cloud Speed", Range(-1, 1)) = -0.04
		_CloudAlpha ("Cloud Alpha", Range(0, 1)) = 1
		_CloudShadowStrength ("Cloud Shadow Strength", Range(0, 1)) = 0.2
		_CloudElevation ("Cloud Elevation", Range(0.001, 0.1)) = 0.003
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
    	Pass 
    	{
    		
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			sampler2D _NormalMap;
			sampler2D _CloudMap;
		
			uniform float3 v3Translate;		// The objects world pos
			uniform float3 _SunLightDirection;		// The direction vector to the light source
			uniform float3 v3InvWavelength; // 1 / pow(wavelength, 4) for the red, green, and blue channels
			uniform float fOuterRadius;		// The outer (atmosphere) radius
			uniform float fOuterRadius2;	// fOuterRadius^2
			uniform float fInnerRadius;		// The inner (planetary) radius
			uniform float fInnerRadius2;	// fInnerRadius^2
			uniform float fKrESun;			// Kr * ESun
			uniform float fKmESun;			// Km * ESun
			uniform float fKr4PI;			// Kr * 4 * PI
			uniform float fKm4PI;			// Km * 4 * PI
			uniform float fScale;			// 1 / (fOuterRadius - fInnerRadius)
			uniform float fScaleDepth;		// The scale depth (i.e. the altitude at which the atmosphere's average density is found)
			uniform float fScaleOverScaleDepth;	// fScale / fScaleDepth
			uniform float fHdrExposure;		// HDR exposure
			uniform float _BumpAmount;		// Normal/Bump effect amount (0..1)
			uniform	float _CloudSpeed;
			uniform float _CloudAlpha;
			uniform float _CloudShadowStrength;
			uniform float _CloudElevation;
			
			struct v2f 
			{
    			float4 pos : SV_POSITION;
    			float2 uv : TEXCOORD0;
    			float3 c0 : COLOR0;
    			float3 c1 : COLOR1;
    			float3 viewDir: TEXCOORD1;
			};
			
			float scale(float fCos)
			{
				float x = 1.0 - fCos;
				return fScaleDepth * exp(-0.00287 + x*(0.459 + x*(3.83 + x*(-6.80 + x*5.25))));
			}

			v2f vert(appdata_tan v)
			{
			    float3 v3CameraPos = _WorldSpaceCameraPos - v3Translate;	// The camera's current position
				float fCameraHeight = length(v3CameraPos);					// The camera's current height
				float fCameraHeight2 = fCameraHeight*fCameraHeight;			// fCameraHeight^2
				
				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
				float3 v3Pos = mul(_Object2World, v.vertex).xyz - v3Translate;
				float3 v3Ray = v3Pos - v3CameraPos;
				float fFar = length(v3Ray);
				v3Ray /= fFar;
				
				// Calculate the closest intersection of the ray with the outer atmosphere (which is the near point of the ray passing through the atmosphere)
				float B = 2.0 * dot(v3CameraPos, v3Ray);
				float C = fCameraHeight2 - fOuterRadius2;
				float fDet = max(0.0, B*B - 4.0 * C);
				float fNear = 0.5 * (-B - sqrt(fDet));
				
				// Calculate the ray's starting position, then calculate its scattering offset
				float3 v3Start = v3CameraPos + v3Ray * fNear;
				fFar -= fNear;
				float fDepth = exp((fInnerRadius - fOuterRadius) / fScaleDepth);
				float fCameraAngle = dot(-v3Ray, v3Pos) / length(v3Pos);
				float fLightAngle = dot(_SunLightDirection, v3Pos) / length(v3Pos);
				float fCameraScale = scale(fCameraAngle);
				float fLightScale = scale(fLightAngle);
				float fCameraOffset = fDepth*fCameraScale;
				float fTemp = (fLightScale + fCameraScale);
				
				const float fSamples = 2.0;
				
				// Initialize the scattering loop variables
				float fSampleLength = fFar / fSamples;
				float fScaledLength = fSampleLength * fScale;
				float3 v3SampleRay = v3Ray * fSampleLength;
				float3 v3SamplePoint = v3Start + v3SampleRay * 0.5;
				
				// Now loop through the sample rays
				float3 v3FrontColor = float3(0.0, 0.0, 0.0);
				float3 v3Attenuate;
				for(int i=0; i<int(fSamples); i++)
				{
					float fHeight = length(v3SamplePoint);
					float fDepth = exp(fScaleOverScaleDepth * (fInnerRadius - fHeight));
					float fScatter = fDepth*fTemp - fCameraOffset;
					v3Attenuate = exp(-fScatter * (v3InvWavelength * fKr4PI + fKm4PI));
					v3FrontColor += v3Attenuate * (fDepth * fScaledLength);
					v3SamplePoint += v3SampleRay;
				}
				
    			v2f OUT;
    			OUT.pos = mul(UNITY_MATRIX_MVP, v.vertex);
    			OUT.uv = v.texcoord.xy;
    			OUT.c0 = v3FrontColor * (v3InvWavelength * fKrESun + fKmESun);
    			OUT.c1 = v3Attenuate;
    			
				TANGENT_SPACE_ROTATION;
				OUT.viewDir = normalize(mul(rotation, ObjSpaceViewDir(v.vertex)));
    			return OUT;
			}
			
			
			half4 frag(v2f IN) : COLOR
			{
				// get Earth texture texel
				half3 color = tex2D(_MainTex, IN.uv).rgb;
				
				// apply bump mapping
				float3 elevation = UnpackNormal(tex2D(_NormalMap, IN.uv));
				float d = 1.0 - 0.5 * saturate(dot(elevation, IN.viewDir) * _BumpAmount );
				half3 earth = color * d;
				
				// compute cloud and shadows
				float2 t = fixed2(_Time[0] * _CloudSpeed, 0);
				float2 disp = -IN.viewDir * _CloudElevation;
				fixed4 cloud = tex2D (_CloudMap, IN.uv + t - disp);
				const float2 c = fixed2(0.998,0);
				fixed4 shadows = tex2D (_CloudMap, IN.uv + t + c + disp) * _CloudShadowStrength;
				shadows *=  dot(elevation, IN.viewDir);
					
				// apply clouds
				earth += (cloud - clamp(shadows, shadows, 1-cloud)) * _CloudAlpha ;
				
				// apply atmosphere scattering
				float3 col = IN.c0 + 0.25 * IN.c1;
				
				// adjust color from HDR
				col = 1.0 - exp(col * -fHdrExposure);
				earth *= col.b;
				return half4(earth*1.25+col*0.75,1.0);
			}
			
			ENDCG

    	}
	}
}