Shader "UI/CRT_Scanlines"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(CRT Curvature)]
        _CurvatureX ("Curvature X", Range(0.0, 0.25)) = 0.08
        _CurvatureY ("Curvature Y", Range(0.0, 0.25)) = 0.06

        [Header(Scanlines)]
        _ScanlineCount ("Scanline Count", Float) = 540.0
        _ScanlineIntensity ("Scanline Intensity", Range(0.0, 1.0)) = 0.18

        [Header(Rolling Scan Beam)]
        _ScanBeamSpeed ("Scan Beam Speed", Float) = 0.035
        _ScanBeamWidth ("Scan Beam Width", Range(0.005, 0.1)) = 0.018
        _ScanBeamIntensity ("Scan Beam Intensity", Range(0.0, 1.0)) = 0.12

        [Header(Vignette and Flicker)]
        _VignetteStrength ("Vignette Strength", Range(0.0, 1.0)) = 0.05
        _FlickerIntensity ("Flicker Intensity", Range(0.0, 0.05)) = 0.003

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent+500"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;

            float _CurvatureX;
            float _CurvatureY;

            float _ScanlineCount;
            float _ScanlineIntensity;

            float _ScanBeamSpeed;
            float _ScanBeamWidth;
            float _ScanBeamIntensity;

            float _VignetteStrength;
            float _FlickerIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Normalized coordinates centered at (0, 0), ranging [-1, 1]
                float2 p = (IN.texcoord - 0.5) * 2.0;

                // Curvature distortion: curves scanlines and rolling beam across the screen
                // without clipping or drawing any black borders or bezel frames.
                float2 curvedP = p;
                curvedP.x += p.x * (p.y * p.y) * _CurvatureX;
                curvedP.y += p.y * (p.x * p.x) * _CurvatureY;

                // Convert curved coordinates back to [0, 1]
                float2 curvedUV = (curvedP + 1.0) * 0.5;

                // Horizontal scanline pattern that curves gracefully toward edges and corners
                float scanline = sin(curvedUV.y * _ScanlineCount * 3.14159265 * 2.0);
                scanline = (scanline + 1.0) * 0.5;
                float scanFactor = scanline * _ScanlineIntensity;

                // Slow, thin upward rolling scan beam (bell curve)
                float beamPos = frac(curvedUV.y - _Time.y * _ScanBeamSpeed);
                float beamDist = abs(beamPos - 0.5);
                float beam = smoothstep(_ScanBeamWidth, 0.0, beamDist);
                float beamFactor = beam * _ScanBeamIntensity;

                // Extremely subtle vignette to preserve edge brightness - NO BLACK BORDERS!
                float2 vigUV = IN.texcoord * (1.0 - IN.texcoord);
                float vig = clamp(vigUV.x * vigUV.y * 16.0, 0.0001, 1.0);
                vig = clamp(pow(vig, _VignetteStrength), 0.0, 1.0);
                float vigDarkness = (1.0 - vig) * 0.05;

                // Total darkening (translucent scanlines, never solid black)
                float totalDarkness = scanFactor + vigDarkness;
                totalDarkness = clamp(totalDarkness, 0.0, 0.45);

                fixed4 col;
                if (beamFactor > totalDarkness)
                {
                    // Translucent beam glow
                    col.rgb = float3(0.92, 0.96, 1.0);
                    col.a = (beamFactor - totalDarkness) * 0.25;
                }
                else
                {
                    // CRT dark scanlines (translucent overlay)
                    col.rgb = float3(0.0, 0.0, 0.0);
                    col.a = clamp(totalDarkness - beamFactor, 0.0, 0.35);
                }

                // Subtle refresh flicker
                float flicker = sin(_Time.y * 25.0) * _FlickerIntensity;
                col.a = clamp(col.a + flicker, 0.0, 0.40);

                col.a *= IN.color.a;
                return col;
            }
            ENDCG
        }
    }
}
