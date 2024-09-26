#version 330
 
in vec2 TexCoord0;
uniform sampler2D Sampler;

uniform vec4 vColorAdjust;

uniform int iApplyTextureF;

// local variables:
vec4 tempColor;

void main()
{
	if (iApplyTextureF == 1)
	{
		gl_FragColor = texture2D(Sampler, TexCoord0); // output color is texture color...
		gl_FragColor = gl_FragColor * vColorAdjust; // multiplied by incoming color adjusment
	}
	else
	{
		gl_FragColor = vColorAdjust; // flat shader - no texture, just use color provided
	}
}