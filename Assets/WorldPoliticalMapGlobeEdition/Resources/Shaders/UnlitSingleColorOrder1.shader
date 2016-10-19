Shader "World Political Map/Unlit Single Color Order 1" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
    Color [_Color]
        Tags {
        "Queue"="Geometry+1"
        "RenderType"="Opaque"
    	}
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
    }
    
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
			v.vertex.x+= (v.vertex.w + 0.5) / _ScreenParams.x;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
			
		ENDCG
    }    

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
			v.vertex.x-= (v.vertex.w - 0.5) / _ScreenParams.x;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
			
		ENDCG
    }   
    
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
			v.vertex.y+= (v.vertex.w + 0.5) / _ScreenParams.y;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
			
		ENDCG
    }    
    
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
			v.vertex.y-= (v.vertex.w - 0.5) / _ScreenParams.y;
		}
		
		fixed4 frag(AppData i) : COLOR {
			return _Color;					
		}
			
		ENDCG
    }    
}
 
}
