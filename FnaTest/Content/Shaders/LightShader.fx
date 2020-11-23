static const int DIRS = 32;
static const int QUALITY = 5;

sampler s0; // The light texture.
float2 resolution; // The screen resolution.
texture inputTex;
sampler inputTexSampler = sampler_state
{
  Texture = <inputTex>;
  AddressU = Clamp;
  AddressV = Clamp;
  MagFilter = Linear;
  MinFilter = Linear;
};
float radius; // Measured in pixels.
float2 offsets[DIRS]; // Pre-calculated sins and cos's

float4 Blur(float2 uv : TEXCOORD0) : COLOR0
{
  float2 radius2 = radius / resolution;
  float4 color = tex2D(inputTexSampler, uv);

  for(int d = 0; d < DIRS; d++)
  {
    float2 offset = offsets[d];
    for(int r = 0; r < QUALITY; r++)
    {
      float radius = (r + 1) / (float)QUALITY;
      color += tex2D(inputTexSampler, uv + offset * radius2 * radius);
    }
  }

  color /= (DIRS * QUALITY) + 1;

  float4 scene = tex2D(s0, uv);

  return scene * color;
}

technique Technique1
{
  pass Blur
  {
    PixelShader = compile ps_3_0 Blur();
  }
}