#version 330

in vec2 TexCoords;

uniform sampler2D albedo_texture;

out vec4 FragColor;

void main()
{
    FragColor = texture(albedo_texture, TexCoords);
}