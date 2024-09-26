#version 130

// Make geometry solid
uniform vec4 vColorValue;

// Output fragment color
out vec4 out_frag_color;

void main(void)
	{ 
		out_frag_color = vColorValue;
	}