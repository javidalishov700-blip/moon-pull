// Shared by MoonPullWater.shader (and usable as a Shader Graph Custom Function). Mirrors MoonPull.Water.WaveMath exactly.
// Globals are pushed every frame by WaterSurface.cs.
#ifndef MOONPULL_WATER_INCLUDED
#define MOONPULL_WATER_INCLUDED

float  _MP_WaterLevel;
float  _MP_WaveTime;
float4 _MP_WaveAmp;
float4 _MP_WaveK;
float4 _MP_WaveOmega;
float4 _MP_WaveDirX;
float4 _MP_WaveDirZ;
float4 _MP_WavePhase;
float4 _MP_Pulse;      // x = centre X, y = current amplitude, z = 1 / width
float  _MP_FullMoon;
float  _MP_ScrollZ;    // distance sailed: the sea scrolls under a boat that stays near z = 0

void MoonPullWaterLevel_float(out float Level, out float FullMoon)
{
    Level = _MP_WaterLevel;
    FullMoon = _MP_FullMoon;
}

// WorldPos: absolute world position of the undisplaced vertex.
// Height: world Y of the displaced surface. Normal: world-space surface normal.
void MoonPullWave_float(float3 WorldPos, out float Height, out float3 Normal)
{
    float4 arg = _MP_WaveK * (_MP_WaveDirX * WorldPos.x + _MP_WaveDirZ * (WorldPos.z + _MP_ScrollZ)) - _MP_WaveOmega * _MP_WaveTime + _MP_WavePhase;
    float4 s = sin(arg);
    float4 c = cos(arg);

    float h = dot(_MP_WaveAmp.xyz, s.xyz);
    float dhdx = dot((_MP_WaveAmp * _MP_WaveK * _MP_WaveDirX).xyz, c.xyz);
    float dhdz = dot((_MP_WaveAmp * _MP_WaveK * _MP_WaveDirZ).xyz, c.xyz);

    float d = (WorldPos.x - _MP_Pulse.x) * _MP_Pulse.z;
    float pulse = _MP_Pulse.y * exp(-d * d);
    h += pulse;
    dhdx += pulse * (-2.0 * d * _MP_Pulse.z);

    Height = _MP_WaterLevel + h;
    Normal = normalize(float3(-dhdx, 1.0, -dhdz));
}

void MoonPullWave_half(float3 WorldPos, out float Height, out float3 Normal)
{
    MoonPullWave_float(WorldPos, Height, Normal);
}

void MoonPullWaterLevel_half(out float Level, out float FullMoon)
{
    MoonPullWaterLevel_float(Level, FullMoon);
}

#endif
