// Editor script to create rainbow texture
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngineInternal;

public class RainbowTextureCreator
{
    [MenuItem("Assets/Create/Rainbow Gradient Texture")]
    static void CreateRainbowTexture()
    {
        int width = 256;
        int height = 1;
        Texture2D rainbowTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int x = 0; x < width; x++)
        {
            float h = (float)x / width; // 0 to 1 (full spectrum)
            Color color = Color.HSVToRGB(h, 1f, 1f);
            rainbowTex.SetPixel(x, 0, color);
        }

        rainbowTex.Apply();

        string path = "Assets/RainbowGradient.png";
        System.IO.File.WriteAllBytes(path, rainbowTex.EncodeToPNG());
        AssetDatabase.Refresh();

        // Configure texture
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }
}
#endif
