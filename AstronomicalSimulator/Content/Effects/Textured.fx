float4x4 World;
float4x4 ViewXProjection;
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
Texture InputTexture2;
sampler TextureSampler2 = sampler_state
{
	Texture = <InputTexture2>;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU  = WRAP;
	AddressV  = WRAP; 
};
float InputTextureInterpolation;  //Determines how much of each InputTexture to use

Texture ColorMapTexture;  //Texture containing the colors to draw or interpolate
sampler ColorMapSampler = sampler_state
{
	Texture = <ColorMapTexture>;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU  = WRAP;
	AddressV  = WRAP; 
};
float TextureAlphaThreshold;  //Determines the alpha cut-off

//-----"Textured" Technique-----
//Applies a texture to a model based on given UV coordinates
//Uses 'InputTexture' as a group of X, Y, and alpha values

struct VSOut
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
};

VSOut TexturedVS(
	float4 Position : POSITION0
	, float2 TexCoord : TEXCOORD0)
{
	VSOut output = (VSOut) 0;
	output.Position = mul (mul (Position, World), ViewXProjection);
	output.TexCoord = TexCoord;
	
	return output;
}

float4 TexturedPS(VSOut input) : COLOR0
{
	float4 data = tex2D(TextureSampler, input.TexCoord);
	if (InputTextureInterpolation > 0.0f)
	{
		float4 data2 = tex2D(TextureSampler2, input.TexCoord);
		data = data * (1.0f - InputTextureInterpolation) + data2 * InputTextureInterpolation;
	}
	float4 color = tex2D(ColorMapSampler, float2(data.r, data.g));
	if (TextureAlphaThreshold != 0)
	{
		color.a = (data.b - TextureAlphaThreshold)/(1.0f - TextureAlphaThreshold);
	}
	
	return color;
}

technique Textured
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 TexturedVS();
		PixelShader = compile ps_2_0 TexturedPS();
	}
}

//-----"TexturedCloud" Technique-----
//Applies a texture to a model based on given UV coordinates
//Uses 'InputTexture' as a group of X, Y, and alpha values

struct VSCloudOut
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
	float Depth : TEXCOORD1;
};

VSCloudOut TexturedCloudVS(
	float4 Position : POSITION0
	, float2 TexCoord : TEXCOORD0)
{
	VSCloudOut output = (VSCloudOut) 0;
	output.Position = mul (mul (Position, World), ViewXProjection);
	output.TexCoord = TexCoord;
	output.Depth = output.Position[2] / 100.0f;
	if (output.Depth > 1.0f)
	{
		output.Depth = 1.0f;
	}
	
	return output;
}

float4 TexturedCloudPS(VSCloudOut input) : COLOR0
{
	float4 data = tex2D(TextureSampler, input.TexCoord);
	float4 color = tex2D(ColorMapSampler, float2(data.r, data.g));
	if (TextureAlphaThreshold != 0)
	{
		color.a = (data.b - TextureAlphaThreshold)/(1.0f - TextureAlphaThreshold);
	}
	color.a *= pow(input.Depth, 0.5f);
	
	return color;
}

technique TexturedCloud
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 TexturedCloudVS();
		PixelShader = compile ps_2_0 TexturedCloudPS();
	}
}

//-----"TextureDiffuse" Technique-----
//Applies a texture to a model based on given UV coordinates + Diffuse Lighting
//Normal = Position

float3 LightDirection;  //Must be normalized outside the shader
float AmbientIntensity;  //Must be between 0.0f and 1.0f
float3 ViewDirection;  //Must be normalized outside the shader
float SpecularIntensity;  //Must be between 0.0f and 1.0f

struct VSDiffuseOut
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float2 Lighting : TEXCOORD1;
};

VSDiffuseOut TextureDiffuseVS(
	float4 Position : POSITION0
	, float2 TexCoord : TEXCOORD0)
{
	VSDiffuseOut output = (VSDiffuseOut) 0;
	
	output.Position = mul (mul (Position, World), ViewXProjection);
	
	output.TexCoord = TexCoord;
	
	float3 Normal = Position;
	float LightFactor = saturate(dot(Position, LightDirection));
	float SpecularFactor = saturate(SpecularIntensity * pow(dot(normalize(-reflect(LightDirection, Normal)), ViewDirection), 15) * LightFactor);
	output.Lighting[0] = AmbientIntensity + (1 - AmbientIntensity) * LightFactor;
	output.Lighting[1] = SpecularFactor;
	
    
    return output;
}

float4 TextureDiffusePS(VSDiffuseOut input) : COLOR0
{
	float4 color = tex2D(TextureSampler, input.TexCoord);
	float4 outputColor = tex2D(ColorMapSampler, float2(color.r, color.g));
	outputColor.rgb *= input.Lighting[0];
	outputColor.rgb += float3(1.0f, 1.0f, 1.0f) * input.Lighting[1];
	if (TextureAlphaThreshold != 0)
	{
		outputColor.a = (color.b - TextureAlphaThreshold)/(1.0f - TextureAlphaThreshold);
	}
	
	return outputColor;
}

technique TextureDiffuse
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 TextureDiffuseVS();
		PixelShader = compile ps_2_0 TextureDiffusePS();
	}
}