#version 330 core

// Input vertex data, different for all executions of this shader.
layout(location = 0) in vec3 vertexPosition_modelspace;
layout(location = 1) in vec3 vertexNormal;
layout(location = 2) in vec2 vertexUV;
layout(location = 3) in vec4 vertexRGBA;
layout(location = 4) in float vertexTerrainLight;

struct Light {
	vec4 color1;
	vec4 direction1;
	vec4 color2;
	vec4 direction2;
};

// Allocate as many as can appear in any level
#define ALLOCATED_LIGHTS 20
layout(std140) uniform lights{
	Light light[ALLOCATED_LIGHTS];
};

// Output data ; will be interpolated for each fragment.
out vec2 UV;
out vec3 lightColor;
out float fogBlend;

// Values that stay constant for the whole mesh.
uniform mat4 worldToView;
uniform mat4 modelToWorld;
uniform int levelObjectType;
uniform int lightIndex;
uniform int useFog;
uniform vec4 fogParams;
uniform vec4 staticColor;
uniform int useLighting;
uniform int useMetalShading;
uniform vec3 cameraPosition;

vec2 computeReflectionUV(vec3 reflectionDirection)
{
    // Project the unit sphere onto a centered disk using an azimuthal
    // equidistant projection. The disk radius is 0.5 in UV space.
    float directionLength = length(reflectionDirection);
    if (directionLength < 0.0001f)
        return vec2(0.5f, 0.5f);

    reflectionDirection /= directionLength;

    float z = clamp(reflectionDirection.z, -1.0f, 1.0f);
    float xyLength = length(reflectionDirection.xy);
    if (xyLength < 0.0001f)
    {
        // The front pole is the disk center. The opposite pole has no
        // azimuth, so place it at a stable point on the disk boundary.
        return z >= 0.0f ? vec2(0.5f, 0.5f) : vec2(0.5f, 0.0f);
    }

    float radius = acos(z) / 3.14159265359f;
    vec2 diskDirection = reflectionDirection.xy / xyLength;
    return vec2(0.5f, 0.5f) + 0.5f * radius * diskDirection;
}

void main() {
	// Output position of the vertex, in clip space : MVP * position
    vec3 worldPosition = (modelToWorld * vec4(vertexPosition_modelspace, 1.0f)).xyz;
    gl_Position = worldToView * vec4(worldPosition, 1.0f);

	vec3 normal = normalize((modelToWorld * vec4(vertexNormal, 0.0f)).xyz);

	// UV of the vertex. No special space for this one.
    if (useMetalShading != 0) {
        vec3 viewDirection = normalize(worldPosition - cameraPosition);
        vec3 reflectionDirection = reflect(viewDirection, normal);
        UV = computeReflectionUV(reflectionDirection);
    }
    else {
        UV = vertexUV;
    }

    // Light color is precomputed on PS3 but we do it here instead.
    if (useLighting == 1) {
        vec3 directionalLight = vec3(0.0f);
        if (levelObjectType >= 1 && levelObjectType <= 4) {
            int index = lightIndex;

            if (levelObjectType == 1) {
                index = min(ALLOCATED_LIGHTS - 1, int(vertexTerrainLight));
            }

            Light l = light[index];

            directionalLight += max(0.0f, -dot(l.direction1.xyz, normal)) * l.color1.xyz;
            directionalLight += max(0.0f, -dot(l.direction2.xyz, normal)) * l.color2.xyz;
        }

        vec3 diffuseLight = vec3(1.0f);
        if (levelObjectType == 1 || levelObjectType == 3) {
            diffuseLight = vertexRGBA.xyz;
        }
        else if (levelObjectType == 2 || levelObjectType == 4) {
            diffuseLight = staticColor.xyz;
        }

        lightColor = mix(diffuseLight, directionalLight, 0.5f);
    }
    else {
        lightColor = vec3(0.5f, 0.5f, 0.5f);
    }

	fogBlend = 0.0f;

	if (useFog == 1 && levelObjectType >= 1 && levelObjectType <= 4) {
        float depth = gl_Position.w - fogParams.x;

        depth = clamp(depth * fogParams.y, 0.0f, 1.0f);

		fogBlend = fogParams.z + depth * fogParams.w;
	}
}
