// Ideally you wouldn't need half these includes for an unlit shader
// But it's stupiod

FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Forward();
    Depth( S_MODE_DEPTH);
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
    float2 vTexCoord : TEXCOORD0 < Semantic( LowPrecisionUv ); >;
};

struct PixelInput
{
    #if ( PROGRAM == VFX_PROGRAM_VS )
        float4 vPositionPs : SV_Position;
    #endif

    #if ( ( PROGRAM == VFX_PROGRAM_PS ) )
        float4 vPositionSs : SV_Position;
    #endif

	float2 vTexCoord : TEXCOORD0;
};

VS
{
	PixelInput MainVs( VertexInput i )
	{
		PixelInput o;
		o.vPositionPs = float4( i.vPositionOs.xy, 0.0f, 1.0f );
		o.vTexCoord = i.vTexCoord;
		return o;
	}
}

PS
{
    #include "common/pixel.hlsl"

	Texture2D _SceneTexture < Attribute("SceneTexture"); >;
	Texture2D _SilhouetteTexture < Attribute("SilhouetteTexture"); SrgbRead(true); >;
    Texture2D _BlurredTexture < Attribute("BlurredTexture"); SrgbRead(true); >;

    float _GlowIntensity < Attribute("GlowIntensity"); >;
    SamplerState Sampler < Filter( Point ); AddressU(Clamp); AddressV(Clamp);>;

    RenderState(DepthFunc, ALWAYS);

    float4 MainPs(PixelInput i) : SV_Target0
    {
        float4 sceneColor       = _SceneTexture.Sample(Sampler, i.vTexCoord);
        float4 silhouetteTexture = _SilhouetteTexture.Sample(Sampler, i.vTexCoord);
        float4 blurredTexture    = _BlurredTexture.Sample(Sampler, i.vTexCoord);
        float4 glow = max(0, blurredTexture - silhouetteTexture) * _GlowIntensity * 2;

        //These lines prevent the glow color from bleeding into each other.
        float3 additiveGlow = glow.rgb * glow.a;
        float3 finalColor = sceneColor.rgb + additiveGlow;

        return float4(finalColor, 1.0);
    }
}
