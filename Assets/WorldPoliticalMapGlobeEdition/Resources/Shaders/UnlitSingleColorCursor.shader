Shader "World Political Map/Unlit Single Color Cursor" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
    Color [_Color]
    Tags {
        "Queue"="Transparent"
        "RenderType"="Transparent"
    }
 	Pass {
	    ZWrite Off
 	}
}
}