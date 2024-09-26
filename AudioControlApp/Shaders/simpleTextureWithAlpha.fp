#version 330
 
in vec2 TexCoord0;
uniform sampler2D Sampler;

uniform vec4 vColorAdjust;

void main()
{
	gl_FragColor = texture2D(Sampler, TexCoord0);
	gl_FragColor = gl_FragColor * vColorAdjust;
}