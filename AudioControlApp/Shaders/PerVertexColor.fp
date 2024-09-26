#version 330
// varying color comes from vertex shader
in vec4 vVaryingColor;
// or Make geometry solid
uniform vec4 vColorValue;
// Output fragment color
out vec4 out_frag_color;
uniform vec4 fixedColorTest = vec4(1.0, 0.0, 0.0, 1.0);
void main()
{ 
	out_frag_color = vColorValue;
}