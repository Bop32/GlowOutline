// Ideally you wouldn't need half these includes for an unlit shader
// But it's stupiod

FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Forward();
    // Depth();
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
        
		return FinalizeVertex( o );
	}
}

PS
{
    #include "common/pixel.hlsl"

    float4 color < Attribute("GlowColor"); >;

	RenderState(DepthEnable, false);
	RenderState(DepthWriteEnable, true);

    float4 MainPs(PixelInput i) : SV_Target0
    {
        /*
        float depthVal = Depth::Get(i.vPositionSs.xy - g_vViewportOffset);
        depthVal = RemapValClamped(depthVal, g_flViewportMinZ, g_flViewportMaxZ, 0.0, 1.0);

        float objectDepth = i.vPositionSs.z;

        float diff = (depthVal - objectDepth);

        float distanceToCamera = length(i.vPositionWithOffsetWs - g_vCameraPositionWs); 

        // If we are close to the person just return color. (Won't work properly in some cases)
        if(distanceToCamera <= 150) return float4(SrgbGammaToLinear(color.rgb), color.a);

        // Hacky fix to make it only render when visible. Still has artifacts and won't work in some cases.
        float maxDistance = 600.0; 
        float distanceFactor = saturate(distanceToCamera / maxDistance);

        float nonLinearFactor = pow(distanceFactor, 2.0); Higher power = faster drop-off

        float maxThreshold = 0.01f; 
        float minThreshold = 0.001f; 

        float threshold = lerp(maxThreshold, minThreshold, nonLinearFactor);

        if (diff > threshold) discard;
        */

        // Above code is only needed if you want to render glow in certain cases like when the object is only behind a wall or only when visible.
        // I am pretty positive we need to use stencils instead of depth but can't figure out a way to do it without it being all hacky and a mess.
        return float4(SrgbGammaToLinear(color.rgb), color.a);
    }
}
