Shader "World Political Map/Unlit Single Texture"{

Properties { _MainTex ("Texture", 2D) = "" }
SubShader {
	Tags {
        "Queue"="Geometry"
        "RenderType"="Opaque"
    }
	Pass {
	Offset 5,2
	SetTexture[_MainTex]
	} 
	}
}