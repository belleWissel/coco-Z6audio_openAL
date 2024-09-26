#version 330
 
in vec2 TexCoord0;
uniform sampler2D Sampler;


void main()
{
	gl_FragColor = texture2D(Sampler, TexCoord0);
}