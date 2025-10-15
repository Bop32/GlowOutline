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
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		// Add your vertex manipulation functions here
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"

	float4 color < Attribute("GlowColor"); >;
    
	RenderState(DepthEnable, true);           // enable depth test to prevent mesh overlap
	RenderState(DepthWriteEnable, false);     // don’t write to depth buffer
	RenderState(DepthFunc, LESS_EQUAL);     // render if closer or equal
	RenderState(CullMode, FRONT);           // cull front faces of expanded mesh
	RenderState(DepthBias, true);             // small offset to prevent z-fighting
	RenderState(BlendEnable, false);          // fully opaque

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float alpha = saturate(color.a); // or 1 if it’s solid geometry
		return float4(SrgbGammaToLinear(color.rgb), alpha);
	}
}
