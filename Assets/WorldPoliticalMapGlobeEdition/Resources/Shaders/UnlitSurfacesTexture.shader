Shader "World Political Map/Unlit Surface Texture" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
    _MainTex ("Texture", 2D) = "white"
}
 
SubShader {
    Tags {
        "Queue"="Geometry+1"
        "RenderType"="Opaque"
    }
    	
    Lighting On
    Blend SrcAlpha OneMinusSrcAlpha
    Material {
        Emission [_Color]
    }
   	ZTest Always
   	ZWrite Off
    Pass {
    	SetTexture [_MainTex] {
            Combine Texture * Primary, Texture * Primary
        }
    }
}
 
}
