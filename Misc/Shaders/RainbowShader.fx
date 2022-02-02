sampler uImage0 : register(s0); // The contents of the screen.
sampler uImage1 : register(s1); // Up to three extra textures you can use for various purposes (for instance as an overlay).
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition; // The position of the camera.
float2 uTargetPosition; // The position of the cursor, updated in SystemHooks' PostUpdateInput method.
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect; // Doesn't seem to be used, but included for parity.
float2 uZoom;

static const float PI = 3.14159265f;
static const float rt2 = sqrt(2.0);

static const float minOpacity = 0.2; // The rainbow opacity on the entirety of the screen.
static const float maxOpacity = 0.7; // The rainbow opacity in the vicinity of the cursor.
static const float minDistance = 1.15; // Pixels that are at least this far away from the closest edge (1 - distance from cursor) get coloured. Can be at most rt2 (1.41) at which point nothing will be coloured.
static const int edgeFade = 10; // The lower this value, the smoother the rainbow fades into the original colours. 

float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * normalize(lerp(K.xxx, saturate(p - K.xxx), c.y));
}
    
float4 RainbowScreen(float2 coords : TEXCOORD0) : COLOR0
{
    float4 c = tex2D(uImage0, coords);
    if (uProgress == 0)
        return c;

	float ratio = uScreenResolution.x / uScreenResolution.y;
	float distance = sqrt(pow((coords.x - uTargetPosition.x) * ratio, 2) + pow(coords.y - uTargetPosition.y, 2));
	float hue = distance * 2 + (1.0 - frac(uTime)) * 2;
    float3 cNew = hsv2rgb(float3(frac(hue), 0.75, 0.85));
    
    float edgeDistance = rt2 - distance; // x and y can be 1.0 at most sqrt(1.0^2 + 1.0^2) = sqrt(2);
    float rainbowMult = clamp((edgeDistance - minDistance) * edgeFade, minOpacity, maxOpacity);

    c.r = c.r * (1.0 - rainbowMult) + cNew.r * rainbowMult;
    c.g = c.g * (1.0 - rainbowMult) + cNew.g * rainbowMult;
    c.b = c.b * (1.0 - rainbowMult) + cNew.b * rainbowMult;
    
    return c;
}

technique Rainbow
{    pass RainbowScreen
    {
        PixelShader = compile ps_2_0 RainbowScreen();
    }
}