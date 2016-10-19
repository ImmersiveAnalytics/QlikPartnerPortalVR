Shader "World Political Map/Unlit Tickers Background" {
 
Properties {
    _Color ("Color", Color) = (1,1,1,0.5)
}
 
SubShader {
	Tags {
//        "Queue"="Geometry+5"
        "Queue"="Transparent"
        "RenderType"="Transparent"
    }
    Color [_Color]
   	Blend SrcAlpha OneMinusSrcAlpha
   	ZWrite On
    Pass {
 
    }
}
 
}
