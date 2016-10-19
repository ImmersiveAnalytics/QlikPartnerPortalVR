Shader "World Political Map/Unlit Earth Single Color" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white"
    }
    Category {
       Lighting On
       ZWrite On
       Cull Back
       Offset 5,2
       Tags {
       	"Queue"="Geometry"
       }
       SubShader {
            Material {
               Emission [_Color]
            }
            Pass {
               SetTexture [_MainTex] {
                      Combine Texture * Primary, Texture * Primary
                }
            }
        } 
    }
}