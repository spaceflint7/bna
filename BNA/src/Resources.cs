
using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public class Resources
    {

        //
        // SpriteEffect
        //

        public static byte[] SpriteEffect => System.Text.Encoding.ASCII.GetBytes(@"

#technique SpriteBatch

--- vertex ---

layout (location = 0) in vec3 pos;
layout (location = 1) in vec4 color;
layout (location = 2) in vec2 uv;
out vec4 f_color;
out vec2 f_uv;
uniform mat4 MatrixTransform;
uniform float MultiplierY;

void main()
{
    gl_Position = MatrixTransform * vec4(pos, 1.0);
    gl_Position.y *= MultiplierY;
    f_color = color;
    f_uv = uv;
}

--- fragment ---

in vec4 f_color;
in vec2 f_uv;
out vec4 o_color;
uniform sampler2D image;

void main()
{
    o_color = texture(image, f_uv) * f_color;
}

--- end ---
");

        //
        // BasicEffect
        //

        public static byte[] BasicEffect => System.Text.Encoding.ASCII.GetBytes(@"

#technique BasicEffect

--- vertex ---

layout (location = 0) in vec3 pos;
layout (location = 1) in vec3 norm;
layout (location = 2) in vec2 uv;

uniform int ShaderIndex;
uniform float MultiplierY;

uniform mat4 WorldViewProj;
uniform mat4 World;
uniform mat3 WorldInverseTranspose;

uniform vec4 DiffuseColor;
uniform vec4 FogVector;

out vec3 f_Position;
out vec3 f_Normal;
out vec2 f_TexCoord;
out float f_FogFactor;
out vec2 f_uv;

void main()
{
    vec4 pos4 = vec4(pos, 1.0);

    f_FogFactor = clamp(dot(pos4, FogVector), 0.0, 1.0);

    f_Position = vec3(World * pos4);
    f_Normal = normalize(WorldInverseTranspose * norm);

    gl_Position = WorldViewProj * pos4;
    gl_Position.y *= MultiplierY;
    f_TexCoord = uv;
}

--- fragment ---

in vec3  f_Position;
in vec3  f_Normal;
in vec2  f_TexCoord;
in float f_FogFactor;

uniform vec3 DirLight0Direction;
uniform vec3 DirLight0DiffuseColor;
uniform vec3 DirLight0SpecularColor;

uniform vec3 DirLight1Direction;
uniform vec3 DirLight1DiffuseColor;
uniform vec3 DirLight1SpecularColor;

uniform vec3 DirLight2Direction;
uniform vec3 DirLight2DiffuseColor;
uniform vec3 DirLight2SpecularColor;

uniform vec4  DiffuseColor;
uniform vec3  EmissiveColor;
uniform vec3  SpecularColor;
uniform float SpecularPower;
uniform vec3  FogColor;

uniform vec3 EyePosition;

uniform sampler2D Texture;
uniform int ShaderIndex;

out vec4 o_color;

void ComputeOneLight(vec3 eyeVector, vec3 worldNormal, out vec3 o_diffuse, out vec3 o_specular)
{
    float dotL = dot(worldNormal, -DirLight0Direction);
    float dotH = dot(worldNormal, normalize(eyeVector - DirLight0Direction));

    float zeroL = step(0.0, dotL);

    float diffuse  = zeroL * dotL;
    float specular = pow(max(dotH, 0.0) * zeroL, SpecularPower);

    o_diffuse  = (DirLight0DiffuseColor  * diffuse)  * DiffuseColor.rgb + EmissiveColor;
    o_specular = (DirLight0SpecularColor * specular) * SpecularColor;
}

void ComputeThreeLights(vec3 eyeVector, vec3 worldNormal, out vec3 o_diffuse, out vec3 o_specular)
{
    mat3 lightDirections = mat3(DirLight0Direction,     DirLight1Direction,     DirLight2Direction);
    mat3 lightDiffuse    = mat3(DirLight0DiffuseColor,  DirLight1DiffuseColor,  DirLight2DiffuseColor);
    mat3 lightSpecular   = mat3(DirLight0SpecularColor, DirLight1SpecularColor, DirLight2SpecularColor);
    mat3 halfVectors     = mat3(normalize(eyeVector - DirLight0Direction),
                                normalize(eyeVector - DirLight1Direction),
                                normalize(eyeVector - DirLight2Direction));

    vec3 dotL = worldNormal * -lightDirections;
    vec3 dotH = worldNormal * halfVectors;

    vec3 zeroL = step(vec3(0.0), dotL);

    vec3 diffuse  = zeroL * dotL;
    vec3 specular = pow(max(dotH, vec3(0.0)) * zeroL, vec3(SpecularPower));

    o_diffuse  = (lightDiffuse  * diffuse)  * DiffuseColor.rgb + EmissiveColor;
    o_specular = (lightSpecular * specular) * SpecularColor;
}

void main()
{
    vec3 eyeVector = normalize(EyePosition - f_Position);
    vec3 worldNormal = normalize(f_Normal);
    vec3 diffuse, specular;
    if ((ShaderIndex & 24) == 24)
        ComputeThreeLights(eyeVector, worldNormal, diffuse, specular);
    else if ((ShaderIndex & 24) != 0)
        ComputeOneLight(eyeVector, worldNormal, diffuse, specular);
    else
    {
        diffuse = DiffuseColor.rgb;
        specular = vec3(0.0);
    }

    vec4 color = mix(vec4(1.0), texture(Texture, f_TexCoord), bvec4(ShaderIndex & 4));
    color.rgb *= diffuse;
    color.rgb += specular * color.a;
    color.rgb = mix(color.rgb, FogColor * color.a, f_FogFactor);
    color.a *= DiffuseColor.a;
    o_color = color;
}

--- end ---
");

    }
}
