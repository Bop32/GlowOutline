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

    PixelInput MainVs(VertexInput i)
    {
        PixelInput o = ProcessVertex(i);
        return FinalizeVertex(o);
    }
}

PS
{
#include "common/pixel.hlsl"

    RenderState(ColorWriteEnable0, false);
    RenderState(StencilEnable, true);
    RenderState(StencilRef, 1);

    RenderState(StencilFunc, ALWAYS);

    RenderState(StencilPassOp, REPLACE);
    RenderState(StencilFailOp, REPLACE);
    RenderState(StencilDepthFailOp, REPLACE);
    RenderState(BackStencilFunc, ALWAYS);
    RenderState(StencilWriteMask, 1);

    float4 MainPs(PixelInput i) : SV_Target0
    {
        return float4(0, 0, 0, 0);
    }
}
