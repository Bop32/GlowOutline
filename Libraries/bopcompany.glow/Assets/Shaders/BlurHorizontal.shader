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
	
	float _GlowSize < Attribute("GlowSize"); >;
    int _MipsLevel <Attribute("MipsLevel"); >;

	SamplerState Sampler < Filter( Bilinear ); AddressU(Clamp); AddressV(Clamp); >;
    

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		const float weight[41] = {0.014, 0.015, 0.017, 0.018, 0.019, 0.020, 0.021, 0.022, 0.024, 0.025, 0.026, 0.027, 0.028, 0.028, 0.029, 0.030, 0.030, 0.031, 0.031, 0.031, 0.031, 0.031, 0.031, 0.031, 0.030, 0.030, 0.029, 0.028, 0.028, 0.027, 0.026, 0.025, 0.024, 0.022, 0.021, 0.020, 0.019, 0.018, 0.017, 0.015, 0.014};

		float4 gSum = 0;
		float total = 0;
		for( float x = -_GlowSize; x <= _GlowSize; x++ )
		{
			float2 offsetUV = i.vTexCoord + float2(x / g_vViewportSize.x, 0);
			gSum += _MainTexture.SampleLevel(Sampler, offsetUV, _MipsLevel ) * weight[abs(x)];
			total += weight[abs(x)];
		}

		return float4( gSum / total);
	}
}

