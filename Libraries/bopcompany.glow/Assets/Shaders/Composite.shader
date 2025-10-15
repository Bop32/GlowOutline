// Ideally you wouldn't need half these includes for an unlit shader
// But it's stupiod

FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Forward();
    Depth();
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
	Texture2D colorBuffer < Attribute( "SceneTexture" ); SrgbRead( true ); >;
	Texture2D _MaskTexture < Attribute("MaskTexture"); SrgbRead(true); >;
    Texture2D _BlurredTexture < Attribute("BlurredTexture"); SrgbRead(true); >;

    float _GlowIntensity < Attribute("GlowIntensity"); >;
    SamplerState Sampler < Filter( Bilinear ); AddressU(Clamp); AddressV(Clamp); >;
    
    RenderState(DepthEnable, false);

    float4 MainPs(PixelInput i) : SV_Target0
    {
        float4 sceneColor       = colorBuffer.Sample(Sampler, i.vTexCoord);
        float4 maskTexture = _MaskTexture.Sample(Sampler, i.vTexCoord);
        float4 blurredTexture    = _BlurredTexture.Sample(Sampler, i.vTexCoord);

        float glowFactor = saturate(blurredTexture.a - maskTexture.a);
        float3 glowColor = blurredTexture.rgb * glowFactor * _GlowIntensity;

        float3 finalColor = sceneColor.rgb + glowColor;

        return float4(finalColor, 1.0);
    }
}
