using UnityEngine;
using System.IO;

public static class ResourceImageLoader
{
    public static Sprite LoadSpriteFromResource(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            Debug.LogError("[ResourceImageLoader] Resource path is null or empty");
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        
        if (sprite == null)
        {
            Debug.LogError($"[ResourceImageLoader] Failed to load Sprite from Resources: {resourcePath}");
            return null;
        }

        Debug.Log($"[ResourceImageLoader] Successfully loaded Sprite: {resourcePath} ({sprite.texture.width}x{sprite.texture.height})");
        return sprite;
    }

    public static Texture2D SpriteToTexture2D(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("[ResourceImageLoader] Sprite is null");
            return null;
        }

        try
        {
            Texture2D sourceTex = sprite.texture;
            
            Texture2D readableTex = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, true);
            Color[] pixels = sourceTex.GetPixels();
            readableTex.SetPixels(pixels);
            readableTex.Apply();
            
            return readableTex;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ResourceImageLoader] Failed to convert Sprite to Texture2D: {e.Message}");
            return null;
        }
    }

    public static void SaveSpriteAsPng(Sprite sprite, string outputPath)
    {
        if (sprite == null)
        {
            Debug.LogError("[ResourceImageLoader] Sprite is null, cannot save");
            return;
        }

        if (File.Exists(outputPath))
        {
            Debug.LogWarning($"[ResourceImageLoader] File already exists: {outputPath}");
            return;
        }

        try
        {
            Texture2D tex = SpriteToTexture2D(sprite);
            if (tex == null) return;

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);

            Object.DestroyImmediate(tex);

            Debug.Log($"[ResourceImageLoader] Saved Sprite as PNG: {outputPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ResourceImageLoader] Failed to save Sprite as PNG: {e.Message}");
        }
    }

    public static void SaveTextureAsPng(Texture2D texture, string outputPath)
    {
        if (texture == null)
        {
            Debug.LogError("[ResourceImageLoader] Texture is null, cannot save");
            return;
        }

        if (File.Exists(outputPath))
        {
            Debug.LogWarning($"[ResourceImageLoader] File already exists: {outputPath}");
            return;
        }

        try
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);

            Debug.Log($"[ResourceImageLoader] Saved Texture as PNG: {outputPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ResourceImageLoader] Failed to save Texture as PNG: {e.Message}");
        }
    }
}
