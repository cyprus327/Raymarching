using Raymarching.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace Raymarching.Rendering;

internal sealed class Raymarcher : IDisposable {
    public Raymarcher(int width, int height) {
        viewport = new Vector2((float)width, (float)height);

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

        _viewportUniformLocation = GL.GetUniformLocation(Handle, "uViewport");
        GL.Uniform2(_viewportUniformLocation, viewport);

        _timeUniformLocation = GL.GetUniformLocation(Handle, "uTime");
        _camPosUniformLocation = GL.GetUniformLocation(Handle, "uCamPos");
        _objPosUniformLocation = GL.GetUniformLocation(Handle, "uObjPos");
    }

    public int Handle { get; init; }

    private readonly int _viewportUniformLocation;
    private Vector2 viewport;

    private readonly int _timeUniformLocation;
    private float elapsedTime = 0f;

    private readonly int _camPosUniformLocation;
    private Vector3 camPos = new(0f, 0f, -7f);

    private readonly int _objPosUniformLocation;
    private Vector3 objPos = new(1f);

    private bool disposed = false;

    public void OnResize(ResizeEventArgs e) {
        viewport = new Vector2(e.Width, e.Height);
    }

    public void HandleInput(float deltaTime, KeyboardState keyboardState, MouseState mouseState, out CursorState cursorState) {
        elapsedTime += deltaTime;

        float dx = 0f, dy = 0f;
        if (mouseState.IsButtonDown(MouseButton.Button2)) {
            dx = mouseState.Delta.X;
            // ignore dy for now
            dy = 0f;//mouseState.Delta.Y;
            cursorState = CursorState.Grabbed;
        } else {
            cursorState = CursorState.Normal;
        }

        Vector3 dir = Vector3.Normalize(camPos - objPos);
        Vector3 right = Vector3.Cross(dir, Vector3.UnitY);

        const float SENS = 0.025f;
        camPos -= right * dx * SENS;
        camPos -= Vector3.Cross(dir, right) * dy * SENS;

        float speed = keyboardState.IsKeyDown(Keys.LeftShift) ? 20f : 5f;
        if (keyboardState.IsKeyDown(Keys.E)) {
            objPos += Vector3.UnitY * deltaTime * speed;
        } else if (keyboardState.IsKeyDown(Keys.Q)) {
            objPos -= Vector3.UnitY * deltaTime * speed;
        }

        // TODO: make this (using startY) better by removing it
        float startY = objPos.Y;

        if (keyboardState.IsKeyDown(Keys.W)) {
            objPos -= dir * deltaTime * speed;
        } else if (keyboardState.IsKeyDown(Keys.S)) {
            objPos += dir * deltaTime * speed;
        }
        if (keyboardState.IsKeyDown(Keys.A)) {
            Vector3 delta = right * deltaTime * speed;
            objPos -= delta;
            camPos -= delta;
        } else if (keyboardState.IsKeyDown(Keys.D)) {
            Vector3 delta = right * deltaTime * speed;
            objPos += delta;
            camPos += delta;
        }
        objPos.Y = startY;

        const float CAM_DIST = 6f;
        camPos = objPos - Vector3.Normalize(objPos - camPos) * CAM_DIST;

        //const float MAX_HEIGHT = 5.5f;
        //if (MathF.Abs(camPos.Y - objPos.Y) > MAX_HEIGHT) {
        //    camPos.Y = camPos.Y < objPos.Y ? objPos.Y - MAX_HEIGHT : objPos.Y + MAX_HEIGHT;
        //}
        camPos.Y = objPos.Y + 4f;

        GL.Uniform2(_viewportUniformLocation, viewport);
        GL.Uniform1(_timeUniformLocation, elapsedTime);
        GL.Uniform3(_camPosUniformLocation, camPos);
        GL.Uniform3(_objPosUniformLocation, objPos);
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
