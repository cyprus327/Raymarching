using System.Runtime.InteropServices;

namespace Raymarching.Common;

[StructLayout(LayoutKind.Sequential)]
internal struct Vec3 {
    public Vec3(float x, float y, float z) {
        X = x;
        Y = y;
        Z = z;
    }

    public float X;
    public float Y;
    public float Z;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Sphere {
    public Vec3 C;
    public float R;
}

[StructLayout(LayoutKind.Sequential, Pack = 16)]
internal struct Cube {
    public Vec3 C;
    private float padding1;
    public Vec3 S;
    private float padding2;
}