﻿using Raymarching.Common;
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
    }

    public int Handle { get; init; }

    private readonly int _timeUniformLocation;
    private float elapsedTime = 0f;

    private readonly int _camPosUniformLocation;
    private Vector3 camPos = new(0f, 0f, -7f);

    private bool disposed = false;

    public void HandleInput(float deltaTime, KeyboardState keyboardState, MouseState mouseState) {
        elapsedTime += deltaTime;

        const float SPEED = 4f;
        if (keyboardState.IsKeyDown(Keys.Space)) {
            camPos.Y += deltaTime * SPEED;
        } else if (keyboardState.IsKeyDown(Keys.LeftControl)) {
            camPos.Y -= deltaTime * SPEED;
        } 
        if (keyboardState.IsKeyDown(Keys.W)) {
            camPos.Z += deltaTime * SPEED;
        } else if (keyboardState.IsKeyDown(Keys.S)) {
            camPos.Z -= deltaTime * SPEED;
        } 
        if (keyboardState.IsKeyDown(Keys.A)) {
            camPos.X -= deltaTime * SPEED;
        } else if (keyboardState.IsKeyDown(Keys.D)) {
            camPos.X += deltaTime * SPEED;
        }

        GetMouseDelta(mouseState, out float dx, out float dy);

        GL.Uniform1(_timeUniformLocation, elapsedTime);
        GL.Uniform3(_camPosUniformLocation, camPos);
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