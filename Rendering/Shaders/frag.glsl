#version 420

uniform float uTime;
uniform vec3 uCamPos;
uniform vec3 uObjPos;

in vec2 iResolution;

struct sphere {
    vec3 c;
    float r;
};
layout(std140, binding = 0) uniform SpheresBlock {
    sphere iSpheres[2];
};

struct cube {
    vec3 c;
    vec3 s;
};
layout(std140, binding = 1) uniform CubesBlock {
    cube iCubes[2];
};

out vec4 fragColor;

#define AA_QUALITY 3
#define SHOW_STEP_COUNT 0
#define ENABLE_POST_PROCESSING 1
#define ENABLE_SHADOWS 1
#define SHADOW_OPACITY 0.8
#define FOG_START_DIST 200.0

const vec3 FOG_COLOR = vec3(0.30, 0.36, 0.60);
const vec3 CAM_TARGET = vec3(7.0, -3.0, 8.0);
const float MAX_DIST = 1.1 * FOG_START_DIST;
const int MAX_ITER = 128;

float sdfSphere(vec3 p, float r) {
    return length(p)-r;
}

float sdfBox(vec3 p, vec3 size) {
    vec3 d = abs(p)-size;
    return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
}

float sdfTorus(vec3 p, vec2 radii) {
    return length(vec2(length(p.xz)-radii.x, p.y))-radii.y;
}

float sdfPlane(vec3 p, float h) {
    return dot(p, vec3(0.0, 1.0, 0.0))+h;
}

float opS(float d1, float d2) {
    return max(-d2, d1);
}

vec4 opU(vec4 d1, vec4 d2) {
	return d1.w < d2.w ? d1 : d2;
}

float smin(float a, float b, float k) {
    float h = max(k-abs(a-b), 0.0);
    return min(a, b)-h*h*h/(6.0*k*k);
}

vec4 opBlend(vec4 d1, vec4 d2) {
    const float k = 2.0;
    float d = smin(d1.w, d2.w, k);
    vec3 m = mix(d1.rgb, d2.rgb, clamp(d1.w - d, 0.0, 1.0));
    return vec4(m, d);
}

vec4 sdfTower(vec3 p, vec3 s) {
    const float W = 0.2;

    // make the outer walls by removing the inside of a box
    float outerWalls = opS(sdfBox(p, s), sdfBox(p, vec3(s.x-W, s.y+W, s.z-W)));

    // make a door hole by removing a section for the door
    outerWalls = opS(outerWalls, sdfBox(p-vec3(0.0, 0.0, 0.5*W-s.z), vec3(0.5, s.y+0.1, 0.5*W*2.0)));

    vec4 res = vec4(vec3(0.9, 0.2, 0.2), outerWalls);

    return res;
}

vec4 sdfScene(vec3 pos) {
    float st = sin(uTime);

    vec4 res = vec4(vec3(-0.5), sdfPlane(pos, 10));
    
    vec4 shapeA = vec4(vec3(0.9, 0.0, 0.1), sdfBox(pos-iCubes[0].c, iCubes[0].s));
    vec4 shapeB = vec4(vec3(0.1, 0.1, 0.9), sdfSphere(pos-iSpheres[0].c, iSpheres[0].r));
    res = opU(res, mix(shapeA, shapeB, clamp(st*1.5, -1.0, 1.0)*0.5+0.5));
    
    res = opBlend(res, vec4(vec3(0.2, 0.9, 0.1), sdfTorus(pos-vec3(st*2.0+1.0, -2.5, 8.0), vec2(2.0, 0.3))));
    
    res = opU(res, vec4(vec3(1.0), opS(sdfBox(pos-iCubes[1].c, iCubes[1].s), sdfSphere(pos-iSpheres[1].c, st*0.28+1.3))));
    
    res = opU(res, sdfTower(pos-vec3(-8.0, -2.0, 4.0), vec3(1.0, 2.0, 3.0)));

    res = opU(res, vec4(vec3(0.1, 0.1, 0.1), sdfSphere(pos-uObjPos, 0.1)));
    return res;
}

vec3 calcNormal(vec3 pos) {
    float c = sdfScene(pos).w;
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        sdfScene(pos + e.xyy).w,
        sdfScene(pos + e.yxy).w,
        sdfScene(pos + e.yyx).w) - c);
}

struct hitInfo {
    float minDist;
    vec3 mat;
    int steps;
};

hitInfo castRay(vec3 rayOrigin, vec3 rayDir) {
    float t = 0.0;
    
    hitInfo info;
    info.mat = vec3(-1.0);
    
    for (info.steps = 0; info.steps < MAX_ITER; info.steps++) {
        vec4 res = sdfScene(rayOrigin + rayDir * t);
        
        if (res.w < (0.0001*t)) {
            info.minDist = t;
            return info;
        }
        
        if (res.w > MAX_DIST) {
            info.mat = vec3(-1.0);
            info.minDist = -1.0;
            break;
        }
        
        info.mat = res.rgb;
        t += res.w;
    }
    
    info.minDist = t;
    return info;
}

// https://iquilezles.org/articles/checkerfiltering
float checkersGradBox(vec2 p) {
    vec2 w = fwidth(p) + 0.001;
    vec2 i = 2.0*(abs(fract((p-0.5*w)*0.5)-0.5)-abs(fract((p+0.5*w)*0.5)-0.5))/w;
    return clamp(0.5 - 0.5*i.x*i.y,0.0,1.0);
}

vec3 applyFog(vec3 rgb, float dist) {
    float fogAmount = 1.0 - exp(-(dist - FOG_START_DIST / 10.0) * (1.0 / FOG_START_DIST));
    return mix(rgb, FOG_COLOR, fogAmount);
}

vec3 render(vec3 rayOrigin, vec3 rayDir) {
    vec3 col = FOG_COLOR - rayDir.y / 2.0;
    hitInfo info = castRay(rayOrigin, rayDir);

#if SHOW_STEP_COUNT
    return vec3(float(info.steps) / float(MAX_ITER), 0.0, 0.0);
#endif
    
    vec3 pos = rayOrigin + rayDir * info.minDist;
    vec3 normal = calcNormal(pos);
    vec3 light = normalize(vec3(sin(uTime / 3.0), 0.9, -0.5));

    if (info.mat != vec3(-1.0)) {        
        if (info.mat != vec3(-0.5)) {            
            col = info.mat;
			
            float NoL = max(dot(normal, light), 0.0);
            vec3 LDirectional = vec3(1.25, 1.2, 0.8) * NoL;
            vec3 LAmbient = FOG_COLOR / 3.5;
            vec3 diffuse = col * (LDirectional + LAmbient);
        	col = diffuse;
            
            //col = normal*0.5+0.5;
        } else {
            float grid = checkersGradBox(pos.xz*0.2) * 0.03 + 0.1;
            col = vec3(grid / 5.0, grid, grid / 7.0);
            
#if ENABLE_SHADOWS
            float shadow = 0.0;
            vec3 shadowRayOrigin = pos + normal * 0.01;
            if (castRay(shadowRayOrigin, light).mat != vec3(-1.0)) {
                shadow += 1.0;
            }
            
    		vec3 cshadow = pow(vec3(shadow), vec3(1.0, 1.2, 1.5));
            col = mix(col, col*cshadow*(1.0-SHADOW_OPACITY), shadow);
#endif
        }

        col = applyFog(col, length(rayDir) * info.minDist);
    }
    
    return col;
}

vec3 getCamRayDir(vec2 uv, vec3 camPos, vec3 camTarget) {
	vec3 camForward = normalize(camTarget - camPos);
	vec3 camRight = normalize(cross(vec3(0.0, 1.0, 0.0), camForward));
	vec3 camUp = normalize(cross(camForward, camRight));

    float persp = 1.0;
	return normalize(uv.x * camRight + uv.y * camUp + camForward * persp);
}

vec4 getSceneCol(vec2 fragCoord) {
    vec2 uv = (-iResolution.xy + 2.0 * fragCoord) / iResolution.y;
    vec3 rayDir = getCamRayDir(uv, uCamPos, uObjPos);
    
    vec3 col = render(uCamPos, rayDir);
    
    return vec4(col, 1.0);
}


void main() {
	fragColor = vec4(0.0);
    
#if AA_QUALITY > 1
    float AA = float(AA_QUALITY);
    for (float aaY = 0.0; aaY < AA; aaY++) {
        for (float aaX = 0.0; aaX < AA; aaX++) {
            fragColor += getSceneCol(gl_FragCoord.xy + vec2(aaX, aaY) / AA);
        }
    }
    fragColor /= AA * AA;
#else
    fragColor = getSceneCol(gl_FragCoord.xy);
#endif
    
    
#if ENABLE_POST_PROCESSING
    vec2 screenCoord = gl_FragCoord.xy / iResolution.xy;

    if (screenCoord == vec2(0.5)) {
        fragColor = vec4(0.0, 1.0, 0.0, 1.0);
        return;
    }

    // vignette
    float radius = 0.8;
    float d = smoothstep(radius, radius-0.5, length(screenCoord-vec2(0.5)));
    fragColor = mix(fragColor, fragColor * d, 0.6);
    
    // contrast
    float constrast = 0.3;
    fragColor = mix(fragColor, smoothstep(0.0, 1.0, fragColor), constrast);    
#endif
    
    fragColor = pow(fragColor, vec4(0.4545));
}