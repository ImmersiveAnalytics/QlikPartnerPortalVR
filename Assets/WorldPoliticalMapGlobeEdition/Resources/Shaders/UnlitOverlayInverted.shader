 
Shader "World Political Map/Unlit Overlay Inverted" {
 
Properties
    {
       _MainTex ("Texture", 2D) = ""
    }
 
SubShader
    {
        Tags {
         "Queue" = "Transparent" 
         "RenderType"="Transparent"
        }
        Cull Front
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            SetTexture[_MainTex] { Combine texture, texture * primary}
        }
    }
}