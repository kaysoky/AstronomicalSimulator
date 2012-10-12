float4x4 World;
float4x4 ViewXProjection;
float3 LightDirection;  //Must be normalized outside the shader
float AmbientIntensity;  //Must be between 0.0f and 1.0f
float3 ViewDirection;  //Must be normalized outside the shader
float SpecularIntensity;  //Must be between 0.0f and 1.0f


//-----"Diffuse" Technique-----
//Adds some lighting changes over objects
//Includes specular dots

struct VSOut
{
    float4 Position : POSITION0;
    float4 Color	: COLOR0;
};

VSOut DiffuseVS(
	float4 Position : POSITION0
	, float3 Normal: NORMAL0
	, float4 Color : COLOR0)
{
	VSOut output = (VSOut) 0;
    output.Position = mul (mul (Position, World), ViewXProjection);
    Normal = mul(Normal, World);
    float LightFactor = saturate(dot(Normal, LightDirection));
    float SpecularFactor = saturate(SpecularIntensity * pow(dot(normalize(-reflect(LightDirection, Normal)), ViewDirection), 15) * LightFactor);
    output.Color = Color;
    output.Color.rgb *= AmbientIntensity + (1 - AmbientIntensity) * LightFactor;
    output.Color.rgb += float3(1.0f, 1.0f, 1.0f) * SpecularFactor;
    
    return output;
}

float4 DiffusePS(VSOut input) : COLOR0
{
	return input.Color;
}

technique Diffuse
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 DiffuseVS();
        PixelShader = compile ps_2_0 DiffusePS();
    }
}

//-----"DiffuseFast" Technique-----
//Adds some lighting changes over objects
//Excludes specular dots

VSOut DiffuseFastVS(
	float4 Position : POSITION0
	, float3 Normal: NORMAL0
	, float4 Color : COLOR0)
{
	VSOut output = (VSOut) 0;
    output.Position = mul (mul (Position, World), ViewXProjection);
    float LightFactor = saturate(dot(Normal, LightDirection));
    output.Color = Color;
    output.Color.rgb *= AmbientIntensity + (1 - AmbientIntensity) * LightFactor;
    
    return output;
}

technique DiffuseFast
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 DiffuseFastVS();
        PixelShader = compile ps_2_0 DiffusePS();
    }
}