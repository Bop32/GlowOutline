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
    Texture2D _DownScaledTexture < Attribute("DownScaledTexture"); SrgbRead(true); >;
    float _GlowIntensity < Attribute("GlowIntensity"); >;
    int _MipsLevel < Attribute("GlowMips"); >;
    
    RenderState(DepthEnable, false);
    float4 MainPs(PixelInput i) : SV_Target0
    {
        float4 sceneColor = colorBuffer.Sample(g_sBilinearClamp, i.vTexCoord);
    
        // Blurred glow (may have spread into visible areas)
        float4 glow = _DownScaledTexture.Sample(g_sBilinearClamp, i.vTexCoord);
    
        // Original mask before blur (sharp edges showing occlusion)
        float4 mask = _MaskTexture.Sample(g_sBilinearClamp, i.vTexCoord);
    
        float3 glowColor = glow.rgb * glow.a * (1 - mask.a) * _GlowIntensity;
        float3 finalColor = sceneColor.rgb + glowColor;
    
        return float4(finalColor, 1.0);
    }
}