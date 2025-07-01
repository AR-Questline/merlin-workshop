
#ifndef VECTOR_ENCODING_DECODING_INCLUDED
#define VECTOR_ENCODING_DECODING_INCLUDED

#define V_PI		3.14159265359f
#define V_TWO_PI	6.28318530718f
#define V_HALF_PI   1.57079632679f

float2 DecodeFloat1ToFloat2(float f1)
{
    float2 f2;

    f1 *= 1024.0;
    f2.x = floor(f1 / 32.0) / 31.5;
    f2.y = (f1 - (floor(f1 / 32.0) * 32.0)) / 31.5;

    return f2;
}

float3 DecodeFloat2ToFloat3(float2 f2)
{
    float3 f3;

    f2 *= 4.0;
    f2 -= 2.0;

    float f2dot = dot(f2, f2);

    // Ensure the value inside sqrt is non-negative
    float sqrtTerm = sqrt(max(1.0 - (f2dot / 4.0), 0.0));

    f3.x = sqrtTerm * f2.x;
    f3.y = sqrtTerm * f2.y;
    f3.z = 1.0 - (f2dot / 2.0);

    // Normalize to maintain unit length
    f3 = normalize(f3);

    return f3;
}

void DecodeFloat1ToFloat3(float f1, out float3 decoded)
{
    float2 f2 = DecodeFloat1ToFloat2(f1);
    decoded = DecodeFloat2ToFloat3(f2);
}

#endif
