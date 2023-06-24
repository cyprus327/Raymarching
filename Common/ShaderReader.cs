namespace Raymarching.Common;

internal static class ShaderReader {
    public static string ReadToString(string filepath) {
        string shaderSource = string.Empty;

        try {
            using var sr = new StreamReader(filepath);
            shaderSource = sr.ReadToEnd();
        } catch (IOException ex) {
            //MessageBox.Show("Failed to read shader, error: " + ex.Message);
            Console.WriteLine("Failed to read shader, error: " + ex.Message);
        }

        return shaderSource;
    }
}