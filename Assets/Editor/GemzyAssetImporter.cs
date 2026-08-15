using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class GemzyTexturePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Pixel Art Gem Pack - Animated") &&
            !assetPath.Contains("Resources/GemAnimations") &&
            !assetPath.Contains("Resources/Effects/Spark"))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = assetPath.Contains("GEM 10") ? 48f : 32f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
    }
}

public static class GemzyAssetImporter
{
    private const string SourceRoot = "Assets/Pixel Art Gem Pack - Animated";
    private const string GemDestinationRoot = "Assets/Resources/GemAnimations";
    private const string SparkDestination = "Assets/Resources/Effects/Spark";

    private static readonly (string source, string destination)[] GemSets =
    {
        ("GEM 1/BLUE", "Blue"),
        ("GEM 2/LIGHT GREEN", "Green"),
        ("GEM 3/RED", "Red"),
        ("GEM 4/GOLD", "Gold"),
        ("GEM 5/PURPLE", "Purple"),
        ("GEM 6/TURQUOISE", "Teal")
    };

    [MenuItem("Gemzy/Assets/Sync Pixel Gem Pack")]
    public static void SyncPixelGemPack()
    {
        if (!AssetDatabase.IsValidFolder(SourceRoot))
        {
            EditorUtility.DisplayDialog("Gemzy", "Pixel Art Gem Pack - Animated was not found in Assets.", "OK");
            return;
        }

        foreach ((string source, string destination) in GemSets)
        {
            CopyPngFolder($"{SourceRoot}/{source}", $"{GemDestinationRoot}/{destination}");
        }

        CopyPngFolder($"{SourceRoot}/Spark/Sprites", SparkDestination);
        AssetDatabase.Refresh();
        ForceReimport("Assets/Resources/GemAnimations");
        ForceReimport("Assets/Resources/Effects");
        Debug.Log("Gemzy asset sync complete.");
    }

    private static void CopyPngFolder(string sourceFolder, string destinationFolder)
    {
        string absoluteSource = Path.GetFullPath(sourceFolder);
        string absoluteDestination = Path.GetFullPath(destinationFolder);

        if (!Directory.Exists(absoluteSource))
        {
            Debug.LogWarning($"Missing source folder: {sourceFolder}");
            return;
        }

        Directory.CreateDirectory(absoluteDestination);
        foreach (string sourceFile in Directory.GetFiles(absoluteSource, "*.png"))
        {
            string destinationFile = Path.Combine(absoluteDestination, Path.GetFileName(sourceFile));
            File.Copy(sourceFile, destinationFile, true);
        }
    }

    private static void ForceReimport(string assetFolder)
    {
        string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { assetFolder });
        foreach (string guid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
