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
    float3 vPositionOs : POSITION;
    float2 vTexCoord : TEXCOORD0;
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

    Texture2D _MainTexture <Attribute("VerticalBlurTexture");  >;
	float2 _ScreenSize < Attribute("Size"); >;
	float _GlowSize < Attribute("GlowSize"); >;
    SamplerState Sampler < Filter( Bilinear ); AddressU(Clamp); AddressV(Clamp); >;
    
	RenderState(BlendEnable, false);


	float4 MainPs( PixelInput i ) : SV_Target0
	{
		const float weight[41] = {0.014, 0.015, 0.017, 0.018, 0.019, 0.020, 0.021, 0.022, 0.024, 0.025, 0.026, 0.027, 0.028, 0.028, 0.029, 0.030, 0.030, 0.031, 0.031, 0.031, 0.031, 0.031, 0.031, 0.031, 0.030, 0.030, 0.029, 0.028, 0.028, 0.027, 0.026, 0.025, 0.024, 0.022, 0.021, 0.020, 0.019, 0.018, 0.017, 0.015, 0.014};

		// Gaussian Blur
		float4 gSum = 0;

		for( float x = -_GlowSize; x <= _GlowSize; x++ )
		{
			float2 UV = ( i.vPositionSs.xy + float2(x, 0) ) / g_vViewportSize.xy;
			gSum += _MainTexture.SampleLevel(Sampler, UV, 1 ) * weight[abs(x)];
		}

		return float4( gSum );
	}
}

