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
    // clang-format off
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
    // clang-format off
    Texture2D colorBuffer < Attribute( "SceneTexture" ); SrgbRead( true ); >;
    Texture2D _DownScaledTexture < Attribute("DownScaledTexture"); SrgbRead(true); >;
    float _GlowIntensity < Attribute("GlowIntensity"); >;
    int _MipsLevel < Attribute("GlowMips"); >;

    // clang-format on
    RenderState(DepthEnable, false);
    RenderState(StencilEnable, true);

    RenderState(StencilRef, 1);
    RenderState(StencilReadMask, 1);

    RenderState(StencilFunc, NOT_EQUAL);

    RenderState(StencilPassOp, KEEP);
    RenderState(StencilFailOp, KEEP);
    RenderState(StencilDepthFailOp, KEEP);

    float4 MainPs(PixelInput i) : SV_Target0
    {
        float4 scene = colorBuffer.Sample(g_sBilinearClamp, i.vTexCoord);
        float4 glow = _DownScaledTexture.Sample(g_sBilinearClamp, i.vTexCoord);

        float3 result = scene.rgb + glow.rgb * _GlowIntensity;

        return float4(result, 1);
    }
}
