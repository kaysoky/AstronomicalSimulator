float4x4 World;
float4x4 ViewXProjection;
//Used by the following Post Process shaders
sampler SceneSampler : register(s0);

//-----"GenerateNoise" Technique-----
//Generates a bunch of noise from

Texture InputTexture;
sampler TextureSampler = sampler_state
{
	Texture = <InputTexture>;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU  = WRAP;
	AddressV  = WRAP; 
};
float NoiseShift;  //Should be between 0.0f and 1.0f
float Sharpness;  //Lower for more sharpness

float4 GenerateNoisePS (float2 TexCoord : TEXCOORD0) : COLOR0
{
	float2 shift = float2(1, 1);
	float4 color = tex2D(TextureSampler, fmod(TexCoord + NoiseShift * shift, shift)) / 2.0f;
    color += tex2D(TextureSampler, fmod(TexCoord * 2 + NoiseShift * shift, shift)) / 4.0f;
    color += tex2D(TextureSampler, fmod(TexCoord * 4 + NoiseShift * shift, shift)) / 8.0f;
    color += tex2D(TextureSampler, fmod(TexCoord * 8 + NoiseShift * shift, shift)) / 16.0f;
    color += tex2D(TextureSampler, fmod(TexCoord * 16 + NoiseShift * shift, shift)) / 32.0f;
    color += tex2D(TextureSampler, fmod(TexCoord * 32 + NoiseShift * shift, shift)) / 32.0f;
    color = pow(color, Sharpness);
	return color;
}

technique GenerateNoise
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 GenerateNoisePS();
	}
}

//-----"SphericalWrap" Technique-----
//Blends the two side edges and inflates the upper and lower edges

float WrapMagnitude; //Must be between 0.0f and 0.5f

float4 SphericalWrapPS (float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 color = tex2D(TextureSampler, float2(0.5f + (TexCoord[0] - 0.5f) *  pow(sin(TexCoord[1] * 3.14f), 1.1f), TexCoord[1]));
	if (TexCoord[0] < WrapMagnitude)
	{
		color = color * TexCoord[0] / WrapMagnitude
			+ tex2D(TextureSampler
				, float2(0.5f + (0.5f + TexCoord[0]) *  pow(sin(TexCoord[1] * 3.14f), 1.1f)
					, TexCoord[1]))
				* (1.0f - TexCoord[0] / WrapMagnitude);
	}
	else if (TexCoord[0] > 1.0f - WrapMagnitude)
	{
		color = color * (1.0f - TexCoord[0]) / WrapMagnitude
			+ tex2D(TextureSampler
				, float2(0.5f + (TexCoord[0] - 0.5f) *  pow(sin(TexCoord[1] * 3.14f), 1.1f)
					, TexCoord[1]))
				* (1.0f - (1.0f - TexCoord[0]) / WrapMagnitude);
	}
	color.b *= pow(sin(TexCoord[1] * 3.14f), 0.2);
	return color;
}

technique SphericalWrap
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 SphericalWrapPS();
	}
}

//-----"SpiralWarp" Technique-----
//Spins the image about the center by moving every pixel along an arc of input length
//Erases the center and corners

float WarpMagnitude; //Should be a reasonable length

float4 SpiralWarpPS (float2 TexCoord : TEXCOORD0) : COLOR0
{
	TexCoord *= 2.0f;
	float distance = pow(pow(TexCoord[0] - 1.0f, 2.0f) + pow(TexCoord[1] - 1.0f, 2.0f), 0.5f);
	float4 color;
	if (distance >= 1.0f)
	{
		color = float4(0.0f, 0.0f, 0.0f, 0.0f);
	}
	else
	{
		float angle = WarpMagnitude * distance;
		color = tex2D(TextureSampler
			, float2(cos(angle) * TexCoord[0] + sin(angle) * TexCoord[1]
				, -sin(angle) * TexCoord[0] + cos(angle) * TexCoord[1]));
		color.b *= pow(cos(distance * 1.57f), 0.5);
	}
	return color;
}

technique SpiralWarp
{
	pass Pass1
	{
		PixelShader = compile ps_2_0 SpiralWarpPS();
	}
}