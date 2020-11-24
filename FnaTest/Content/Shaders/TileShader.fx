sampler s0; // The light texture.
float4 masks[5]; // The uv's of all masks within the atlas.

float4x4 _viewProjectionMatrix; // camera view-proj matrix

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float4 Color : COLOR0;
	float4 Tint : COLOR1;
};

VertexShaderOutput Vert(VertexShaderInput input)
{
	VertexShaderOutput output;

  output.Tint = float4(input.Position.z, input.Position.z, input.Position.z, 1);
  input.Position.z = 0; // Force depth to zero.
	output.Position = mul(input.Position, _viewProjectionMatrix);
	output.TexCoord = input.TexCoord;
  output.Color = input.Color;
	return output;
}

// Samples a mask, provided that mask's index and a normalized UV.
inline float4 SampleMask(float2 nuv, int maskIndex)
{
  float4 mask = masks[maskIndex];
  float2 uv = float2(mask.x + mask.z * nuv.x, mask.y + mask.w * nuv.y);
  return tex2D(s0, uv);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
  float2 uv = float2(input.Color.x, input.Color.y);
  float4 baseColor = tex2D(s0, input.TexCoord);

  // Skip sampling the mask if the mask index is 0 (0 is full mask)
  if(input.Color.w == 0){
    return baseColor * input.Tint;
  }

  float4 maskCol = SampleMask(uv, (int)(input.Color.w * 255));
  baseColor.rgba *= maskCol.r;
  return baseColor * input.Tint;
}

technique Technique1
{
  pass Pass1
  {
    VertexShader = compile vs_3_0 Vert();
    PixelShader = compile ps_3_0 PixelShaderFunction();
  }
}