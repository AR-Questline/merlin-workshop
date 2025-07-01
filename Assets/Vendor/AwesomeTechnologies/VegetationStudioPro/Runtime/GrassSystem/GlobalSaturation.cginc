#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/AwakenLitData.hlsl"

void GlobalSaturation_float(out float globalSaturation)
{
	globalSaturation = GlobalSaturation();
}