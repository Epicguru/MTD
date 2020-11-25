static const int SAMPLES = 25;

sampler s0; // Light texture
texture sceneTex; // Scene tex, only used in final (vertical) pass.
sampler sceneTexSampler = sampler_state
{
  Texture = <sceneTex>;
  MagFilter = Point;
  MinFilter = Point;
};

float radius; // Measured in pixels.
float resolution; // The screen resolution in the current axis.
float weights[SAMPLES]; // Pre-calculated kernels, in the current axis.
float3 axis; // Will be (1, 0, 0) for horizontal and (0, 1, 1) for vertical. The Z component is for scene multi.

float4 Blur(float2 uv : TEXCOORD0) : COLOR0
{
  float4 color = float4(0, 0, 0, 0);
  float mag = radius / resolution;
  for(int i = 0; i < SAMPLES; i++)
  {
    float p = -1.0 + ((float)i / (SAMPLES - 1.0)) * 2.0;
    float weight = weights[i];

    float2 ruv = uv + float2(mag * p * axis.x, mag * p * axis.y);

    color += tex2D(s0, ruv) * weight;
  }

  float4 scene = tex2D(sceneTexSampler, uv);

  // Scene has 0 alpha where cleared to sky.
  // Lights should not render in sky, it gives a 'volumetric' look that isn't good.
  return lerp(color, scene * color, axis.z);
}

technique Technique1
{
  pass Blur
  {
    PixelShader = compile ps_3_0 Blur();
  }
}