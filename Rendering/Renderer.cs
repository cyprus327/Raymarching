using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using System.Numerics;
using System.Runtime.InteropServices;
using System;

namespace Raymarching.Rendering;

internal sealed class Renderer : GameWindow {
    public Renderer(int width, int height, string title) :
        base(GameWindowSettings.Default,
        new NativeWindowSettings() {
            Size = (width, height),
            Title = title,
            StartVisible = false,
            StartFocused = true,
            MinimumSize = (400, 225)
        }) {
        this.CenterWindow();
    }

    private int vertexArrayHandle;
    private int vertexBufferHandle;
    private int indexBufferHandle;

    private Raymarcher raymarcher;

    private void InitBuffers() {
        float[] vertices = {
            0f, this.ClientSize.Y,                  // top left
            this.ClientSize.X, this.ClientSize.Y,   // top right
            this.ClientSize.X, 0f,                  // bottom right
            0f, 0f                                  // bottom left
        };

        // clockwise rotation
        int[] indices = {
            0, 1, 2,
            0, 2, 3
        };

        vertexBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        indexBufferHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

        vertexArrayHandle = GL.GenVertexArray();
        GL.BindVertexArray(vertexArrayHandle);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferHandle);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.BindVertexArray(0);
    }

    private void SendToBuffer<T>(T[] arr, string blockName, int bindingInd, int handle) where T : struct {
        int bufferSize = arr.Length * Marshal.SizeOf<T>();

        int uboHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, uboHandle);
        GL.BufferData(BufferTarget.UniformBuffer, bufferSize, arr, BufferUsageHint.StaticDraw);

        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingInd, uboHandle);

        int blockIndex = GL.GetUniformBlockIndex(handle, blockName);
        GL.UniformBlockBinding(handle, blockIndex, bindingInd);
    }

    protected override void OnLoad() {
        base.OnLoad();

        this.IsVisible = true;

        InitBuffers();

        raymarcher = new Raymarcher(this.ClientSize.X, this.ClientSize.Y);

        Sphere[] spheres = {
            new Sphere() { C = new Vec3(4f, -2f, 8f), R = 1.5f },
            new Sphere() { C = new Vec3(10f, -2f, 5f), R = 1f },
        };

        Cube[] cubes = {
            new Cube() { C = new Vec3(4f, -2f, 8f), S = new Vec3(1.5f, 1.5f, 1.5f) },
            new Cube() { C = new Vec3(10f, -2f, 5f), S = new Vec3(1f, 1f, 1f) }
        };

        SendToBuffer(spheres, "SpheresBlock", 0, raymarcher.Handle);
        SendToBuffer(cubes, "CubesBlock", 1, raymarcher.Handle);
    }

    protected override void OnUnload() {
        base.OnUnload();

        GL.BindVertexArray(0);
        GL.DeleteVertexArray(vertexArrayHandle);

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        GL.DeleteBuffer(indexBufferHandle);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.DeleteBuffer(vertexBufferHandle);

        raymarcher?.Dispose();
    }

    protected override void OnResize(ResizeEventArgs e) {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);

        InitBuffers();

        raymarcher.OnResize(e);
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.UseProgram(raymarcher.Handle);
        GL.BindVertexArray(vertexArrayHandle);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferHandle);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

        this.SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        base.OnUpdateFrame(args);

        raymarcher.HandleInput((float)args.Time, this.KeyboardState, this.MouseState, out CursorState cursorState);
        this.CursorState = cursorState;

        string fps = $"FPS: {1 / args.Time:F0}";
        this.Title = $"{fps}";
    }
}

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