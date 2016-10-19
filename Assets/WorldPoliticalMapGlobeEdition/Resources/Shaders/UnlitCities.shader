// Used by cities material

Shader "World Political Map/Unlit Cities" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white"
    }

   	SubShader {
   		
       Tags {
       "Queue"="Geometry+6" // Draw over highlight
       }
       Lighting On
       ZWrite On
       Blend SrcAlpha OneMinusSrcAlpha
       Material {
              Emission [_Color]
       }
       Pass {
	       SetTexture [_MainTex] { combine texture * primary }
       }
   } 
    
}