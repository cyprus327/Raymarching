using Raymarching.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace Raymarching.Rendering;

internal sealed class Raymarcher : IDisposable {
    public Raymarcher(int width, int height) {
        char c = Path.DirectorySeparatorChar;
        string fragShaderPath = @$"..{c}..{c}..{c}Rendering{c}Shaders{c}frag.glsl";
        string fragCode = ShaderReader.ReadToString(fragShaderPath);

        int vertShaderHandle = GL.CreateShader(ShaderType.VertexShader);
        string vertShaderPath = @$"..{c}..{c}..{c}Rendering{c}Shaders{c}vert.glsl";
        GL.ShaderSource(vertShaderHandle, ShaderReader.ReadToString(vertShaderPath));
        GL.CompileShader(vertShaderHandle);

        string vertShaderInfoLog = GL.GetShaderInfoLog(vertShaderHandle);
        if (vertShaderInfoLog != string.Empty) {
            //MessageBox.Show("VERT ERROR: " + vertShaderInfoLog);
            Console.WriteLine("VERT ERROR: " + vertShaderInfoLog);
        }

        int fragShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragShaderHandle, fragCode);
        GL.CompileShader(fragShaderHandle);

        string fragShaderInfoLog = GL.GetShaderInfoLog(fragShaderHandle);
        if (fragShaderInfoLog != string.Empty) {
            //MessageBox.Show("FRAG ERROR: " + fragShaderInfoLog);
            Console.WriteLine("FRAG ERROR: " + fragShaderInfoLog);
        }

        GL.UseProgram(0);
        Handle = GL.CreateProgram();

        GL.AttachShader(Handle, vertShaderHandle);
        GL.AttachShader(Handle, fragShaderHandle);
        GL.LinkProgram(Handle);

        GL.DetachShader(Handle, vertShaderHandle);
        GL.DetachShader(Handle, fragShaderHandle);

        GL.DeleteShader(vertShaderHandle);
        GL.DeleteShader(fragShaderHandle);

        GL.UseProgram(Handle);

        int viewportLocation = GL.GetUniformLocation(Handle, "uViewport");
        GL.Uniform2(viewportLocation, (float)width, (float)height);

        _timeUniformLocation = GL.GetUniformLocation(Handle, "uTime");
        _camPosUniformLocation = GL.GetUniformLocation(Handle, "uCamPos");
        _objPosUniformLocation = GL.GetUniformLocation(Handle, "uObjPos");
    }

    public int Handle { get; init; }

    private readonly int _timeUniformLocation;
    private float elapsedTime = 0f;

    private readonly int _camPosUniformLocation;
    private Vector3 camPos = new(0f, 0f, -7f);

    private readonly int _objPosUniformLocation;
    private Vector3 objPos = new(1f);

    private bool disposed = false;

    public void HandleInput(float deltaTime, KeyboardState keyboardState, MouseState mouseState) {
        elapsedTime += deltaTime;

        // camera movement
        float speed = keyboardState.IsKeyDown(Keys.LeftShift) ? 20f : 5f;
        if (keyboardState.IsKeyDown(Keys.Space)) {
            camPos.Y += deltaTime * speed;
        } else if (keyboardState.IsKeyDown(Keys.LeftControl)) {
            camPos.Y -= deltaTime * speed;
        } 
        if (keyboardState.IsKeyDown(Keys.W)) {
            camPos.Z += deltaTime * speed;
        } else if (keyboardState.IsKeyDown(Keys.S)) {
            camPos.Z -= deltaTime * speed;
        } 
        if (keyboardState.IsKeyDown(Keys.A)) {
            camPos.X -= deltaTime * speed;
        } else if (keyboardState.IsKeyDown(Keys.D)) {
            camPos.X += deltaTime * speed;
        }

        // object uniform movement
        if (keyboardState.IsKeyDown(Keys.R)) {
            objPos.Y -= deltaTime;
        } else if (keyboardState.IsKeyDown(Keys.Y)) {
            objPos.Y += deltaTime;
        } else if (keyboardState.IsKeyDown(Keys.T)) {
            objPos.Z += deltaTime;
        } else if (keyboardState.IsKeyDown(Keys.F)) {
            objPos.X -= deltaTime;
        } else if (keyboardState.IsKeyDown(Keys.G)) {
            objPos.Z -= deltaTime;
        } else if (keyboardState.IsKeyDown(Keys.H)) {
            objPos.X += deltaTime;
        }

        GetMouseDelta(mouseState, out float dx, out float dy);

        GL.Uniform1(_timeUniformLocation, elapsedTime);
        GL.Uniform3(_camPosUniformLocation, camPos);
        GL.Uniform3(_objPosUniformLocation, objPos);
    }

    private void GetMouseDelta(MouseState state, out float dx, out float dy) {
        if (state.IsButtonDown(MouseButton.Button1)) {
            dx = state.Delta.X;
            dy = state.Delta.Y;
        } else {
            dx = 0f;
            dy = 0f;
        }
    }

    ~Raymarcher() {
        Dispose();
    }

    public void Dispose() {
        if (disposed) return;

        GL.UseProgram(0);
        GL.DeleteProgram(Handle);

        disposed = true;
        GC.SuppressFinalize(this);
    }
}