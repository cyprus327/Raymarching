#version 410

uniform vec2 uViewport;

layout (location = 0) in vec2 aPosition;

out vec2 iResolution;

void main() {
	iResolution = uViewport;
	float nx = aPosition.x / uViewport.x * 2.0 - 1.0;
	float ny = aPosition.y / uViewport.y * 2.0 - 1.0;
	gl_Position = vec4(nx, ny, 0.0, 1.0);
}