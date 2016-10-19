Shader "World Political Map/Unlit Single Color Order 2" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
    Color [_Color]
    Tags {
        "Queue"="Geometry+260"
        "RenderType"="Opaque"
    }
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
    }
   
}
 
}
