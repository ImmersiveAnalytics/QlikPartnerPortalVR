Shader "World Political Map/Unlit Outline" {
 
Properties {
    _Color ("Color", Color) = (1,1,1,1)
}
 
SubShader {
    Tags {
       "Queue"="Geometry+260"
       "RenderType"="Opaque"
  	}
  	ZWrite Off
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
		
		void vert(inout AppData v) {
			v.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			v.vertex.z-=0.001;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
			
		ENDCG
    }
    
   // SECOND STROKE ***********
 
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};

		void vert(inout AppData v) {
			float4x4 projectionMatrix = UNITY_MATRIX_P;
			float d = projectionMatrix[1][1];
 			float distanceFromCameraToVertex = mul( UNITY_MATRIX_MV, v.vertex ).z;
 			//The check here is for wether the camera is orthographic or perspective
 			float frustumHeight = projectionMatrix[3][3] == 1 ? 2/d : 2.0*-distanceFromCameraToVertex*(1/d);
 			float metersPerPixel = frustumHeight/_ScreenParams.y;
 			
			v.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
 			v.vertex.x += metersPerPixel;
			v.vertex.z-=0.001;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;						//Output RGBA color
		}
			
		ENDCG
    }
    
      // THIRD STROKE ***********
 
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;
		
		struct AppData {
			float4 vertex : POSITION;
		};		
		
		void vert(inout AppData v) {
			float4x4 projectionMatrix = UNITY_MATRIX_P;
			float d = projectionMatrix[1][1];
 			float distanceFromCameraToVertex = mul( UNITY_MATRIX_MV, v.vertex ).z;
 			//The check here is for wether the camera is orthographic or perspective
 			float frustumHeight = projectionMatrix[3][3] == 1 ? 2/d : 2.0*-distanceFromCameraToVertex*(1/d);
 			float metersPerPixel = frustumHeight/_ScreenParams.y;
 			 			
			v.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
 			v.vertex.y += metersPerPixel;
			v.vertex.z-=0.001;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;						//Output RGBA color
		}
			
		ENDCG
    }
    
       
      // FOURTH STROKE ***********
 
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};		
		
		void vert(inout AppData v) {
			float4x4 projectionMatrix = UNITY_MATRIX_P;
			float d = projectionMatrix[1][1];
 			float distanceFromCameraToVertex = mul( UNITY_MATRIX_MV, v.vertex ).z;
 			//The check here is for wether the camera is orthographic or perspective
 			float frustumHeight = projectionMatrix[3][3] == 1 ? 2/d : 2.0*-distanceFromCameraToVertex*(1/d);
 			float metersPerPixel = frustumHeight/_ScreenParams.y;
 			 
			v.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
 			v.vertex.x-= metersPerPixel;
			v.vertex.z-=0.001;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;						//Output RGBA color
		}
			
		ENDCG
    }
    
    // FIFTH STROKE ***********
 
    Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag				

		fixed4 _Color;

		struct AppData {
			float4 vertex : POSITION;
		};
				
		void vert(inout AppData v) {
			float4x4 projectionMatrix = UNITY_MATRIX_P;
			float d = projectionMatrix[1][1];
 			float distanceFromCameraToVertex = mul( UNITY_MATRIX_MV, v.vertex ).z;
 			//The check here is for wether the camera is orthographic or perspective
 			float frustumHeight = projectionMatrix[3][3] == 1 ? 2/d : 2.0*-distanceFromCameraToVertex*(1/d);
 			float metersPerPixel = frustumHeight/_ScreenParams.y;
 			 			
			v.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
 			v.vertex.y-= metersPerPixel;
			v.vertex.z-=0.001;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;						//Output RGBA color
		}
			
		ENDCG
    }
}
}
