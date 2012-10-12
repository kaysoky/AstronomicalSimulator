float4x4 World;
float4x4 ViewXProjection;
float OrdinaryTransparency;  //Used by OrdinaryAlpha, OrdinaryTint, PointSpriteAuto, and Formation
float4 OrdinaryColor;  //Used by OrdinaryTint, BruteForceColor, and Formation

//The following are used by PointSprite and PointSpriteAuto and Glow
float3 CameraPosition;  //Helps determine the point of view vector
float3 CameraUpVector;  //Helps determine the side vector
float PointSpriteSize;  //Multiplies the texture coordinate
float TextureFraction;  //Helps centers the point sprite
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
Texture CircleBorder;  //Used by PointSpriteSmoke
sampler BorderSampler = sampler_state
{
	Texture = <CircleBorder>;
	MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU  = WRAP;
	AddressV  = WRAP; 
};

//-----"Ordinary" Technique-----
//Displays things as they are
struct VSOut
{
    float4 Position : POSITION0;
    float4 Color	: COLOR0;
};

VSOut OrdinaryVS(
	float4 Position : POSITION0
	, float4 Color : COLOR0)
{
	VSOut output = (VSOut) 0;
    output.Position = mul (mul (Position, World), ViewXProjection);
    output.Color = Color;
    
    return output;
}

float4 OrdinaryPS(VSOut input) : COLOR0
{
    return input.Color;
}

technique Ordinary
{
    pass Pass1
    {
        VertexShader = compile vs_1_1 OrdinaryVS();
        PixelShader = compile ps_1_1 OrdinaryPS();
    }
}

//-----"PointSprite" Technique-----
//Draws a texture at a point in space

struct VSTextureOut
{
    float4 Position : POSITION0;
    float2 TexCoord	: TEXCOORD0;
    float4 Color	: COLOR0;
};
VSTextureOut PointSpriteVS(float4 Position : POSITION0
	, float2 TexCoord : TEXCOORD0
	, float4 Color : COLOR0)
{
    VSTextureOut output = (VSTextureOut)0;

	//Get the vectors pointing to the sides of the view
	float3 tempPosition = mul(Position, World);
    float3 eyeVector = tempPosition - CameraPosition;
    float3 sideVector = normalize(cross(eyeVector, CameraUpVector));
    float3 upVector = normalize(cross(sideVector, eyeVector));

	//Shift the position to match the texture coordinates
	float coordX = fmod(TexCoord.x, TextureFraction);
	if (coordX > 0)
	{
		coordX = 1.0f;
		TexCoord.x += TextureFraction / 100;
	}
	float coordY = fmod(TexCoord.y, TextureFraction);
	if (coordY > 0)
	{
		coordY = 1.0f;
		TexCoord.y += TextureFraction / 100;
	}
    tempPosition += (coordX - 0.5f) * sideVector * PointSpriteSize;
    tempPosition += (0.5f - coordY) * upVector * PointSpriteSize;
    output.Position = mul(float4(tempPosition, 1), ViewXProjection);

    output.TexCoord = TexCoord;
    output.Color = Color;

    return output;
}

float4 PointSpritePS(VSTextureOut input) : COLOR0
{
    float4 color = tex2D(TextureSampler, input.TexCoord);
    color.r *= input.Color.r;
    color.g *= input.Color.g;
    color.b *= input.Color.b;
    color.a *= input.Color.a * OrdinaryTransparency;
    return color;
}

technique PointSprite
{
	pass Pass0
	{   
		VertexShader = compile vs_2_0 PointSpriteVS();
		PixelShader  = compile ps_2_0 PointSpritePS();
	}
}

//-----"Glow" Technique-----
//Draws the Glow of a Star

//The following are used by Glow
float3 PositionValue;  //Basis of Vertex Shader manipulation of location
float Offset;  //Value for use in varying the looks of drawing objects

VSOut GlowVS(float4 Position : POSITION0
	, float4 Color : COLOR0)
{
    VSOut output = (VSOut)0;

	//Get the vectors pointing to the sides of the view
	float3 tempPosition = PositionValue;
    float3 eyeVector = tempPosition - CameraPosition;
    float3 sideVector = normalize(cross(eyeVector, CameraUpVector));
    float3 upVector = normalize(cross(sideVector, eyeVector));

	//Shift the position to match the texture coordinates
    tempPosition += (Position[0] * sideVector * PointSpriteSize + Position[1] * upVector * PointSpriteSize)
		* (0.75f + 0.25f * abs(sin(Position[2] * Offset)));
    output.Position = mul(float4(tempPosition, 1), ViewXProjection);

    output.Color = Color;

    return output;
}

technique Glow
{
	pass Pass0
	{   
		VertexShader = compile vs_2_0 GlowVS();
		PixelShader  = compile ps_2_0 OrdinaryPS();
	}
}