using Raymarching.Rendering;

namespace Raymarching;

internal static class Program {
    private static void Main() {
        using var r = new Renderer(800, 600, "test");
        r.Run();
    }
}