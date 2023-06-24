using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;

namespace Raymarching.Rendering;

internal sealed class Renderer : GameWindow {
    public Renderer(int width, int height, string title) :
        base(GameWindowSettings.Default,
        new NativeWindowSettings() {
            Size = (width, height),
            Title = title,
            StartVisible = false,
            StartFocused = true,
            MaximumSize = (width, height),
            MinimumSize = (width, height)
        }) {
        this.CenterWindow();
    }

    private int vertexArrayHandle;
    private int vertexBufferHandle;
    private int indexBufferHandle;

    private Raymarcher raymarcher;

    protected override void OnLoad() {
        base.OnLoad();

        this.IsVisible = true;

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

        raymarcher = new Raymarcher(this.ClientSize.X, this.ClientSize.Y);
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

        raymarcher.HandleInput((float)args.Time, this.KeyboardState, this.MouseState);

        string fps = $"FPS: {1 / args.Time:F0}";
        this.Title = $"{fps}";
    }
}