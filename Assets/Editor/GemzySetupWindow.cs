using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class GemzySetupWindow : EditorWindow
{
    private const string SourceRoot = "Assets/Pixel Art Gem Pack - Animated";
    private const string GemAnimationRoot = "Assets/Resources/GemAnimations";
    private const string SparkRoot = "Assets/Resources/Effects/Spark";
    private const string ScenePath = "Assets/Scenes/Gemzy.unity";
    private const string PixelFontPath = "Assets/Thaleah_PixelFont/Materials/ThaleahFat_TTF.ttf";

    private string windowsBuildPath = "Builds/Windows/Gemzy.exe";
    private string androidBuildPath = "Builds/Android/Gemzy.apk";
    private Vector2 scroll;

    [MenuItem("Gemzy/Setup Window")]
    public static void Open()
    {
        GemzySetupWindow window = GetWindow<GemzySetupWindow>("Gemzy Setup");
        window.minSize = new Vector2(440f, 560f);
        window.Show();
    }

    [MenuItem("Gemzy/Hierarchy/Build Game Objects Into Scene")]
    public static void BuildObjectsIntoHierarchy()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        DestroyIfExists("Gemzy Setup");
        DestroyIfExists("Gemzy Game");
        DestroyIfExists("Board");
        DestroyIfExists("Gemzy HUD");

        GameObject root = new GameObject("Gemzy Game");
        GemzyGame game = root.AddComponent<GemzyGame>();
        game.SetupGameInEditor(AssetDatabase.LoadAssetAtPath<Font>(PixelFontPath));

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Gemzy objects were built into the hierarchy and saved to Gemzy.");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawHeader();
        DrawAssetStatus();
        DrawSceneTools();
        DrawBuildTools();
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        GUILayout.Space(8f);
        EditorGUILayout.LabelField("Gemzy Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Dung cua so nay de xem asset Gemzy, tao lai hierarchy trong scene, sync asset moi, va build game.", MessageType.Info);
    }

    private void DrawAssetStatus()
    {
        GUILayout.Space(10f);
        EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

        DrawStatusRow("Source pack", SourceRoot, AssetDatabase.IsValidFolder(SourceRoot));
        DrawStatusRow("Runtime gem animations", GemAnimationRoot, AssetDatabase.IsValidFolder(GemAnimationRoot));
        DrawStatusRow("Runtime spark effect", SparkRoot, AssetDatabase.IsValidFolder(SparkRoot));
        DrawStatusRow("Pixel font", PixelFontPath, AssetDatabase.LoadAssetAtPath<Font>(PixelFontPath) != null);

        GUILayout.Space(4f);
        DrawFolderCount("Blue", $"{GemAnimationRoot}/Blue");
        DrawFolderCount("Green", $"{GemAnimationRoot}/Green");
        DrawFolderCount("Red", $"{GemAnimationRoot}/Red");
        DrawFolderCount("Gold", $"{GemAnimationRoot}/Gold");
        DrawFolderCount("Purple", $"{GemAnimationRoot}/Purple");
        DrawFolderCount("Teal", $"{GemAnimationRoot}/Teal");
        DrawFolderCount("Spark", SparkRoot);

        GUILayout.Space(6f);
        if (GUILayout.Button("Sync Pixel Gem Pack To Game"))
        {
            GemzyAssetImporter.SyncPixelGemPack();
        }
    }

    private void DrawSceneTools()
    {
        GUILayout.Space(14f);
        EditorGUILayout.LabelField("Scene / Hierarchy", EditorStyles.boldLabel);
        DrawStatusRow("Main scene", ScenePath, File.Exists(ScenePath));

        EditorGUILayout.HelpBox("Nut nay build object that len Hierarchy va save vao Gemzy scene: Board, Tiles, HUD, EventSystem, runtime component, va font pixel.", MessageType.None);

        if (GUILayout.Button("Build Game Objects Into Hierarchy"))
        {
            BuildObjectsIntoHierarchy();
        }

        if (GUILayout.Button("Open Gemzy Scene"))
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    private void DrawBuildTools()
    {
        GUILayout.Space(14f);
        EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);

        windowsBuildPath = EditorGUILayout.TextField("Windows output", windowsBuildPath);
        androidBuildPath = EditorGUILayout.TextField("Android output", androidBuildPath);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build Windows x64"))
        {
            GemzyBuildTool.BuildWindowsAt(windowsBuildPath);
        }

        if (GUILayout.Button("Build Android APK"))
        {
            GemzyBuildTool.BuildAndroidAt(androidBuildPath);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Open Build Folder"))
        {
            GemzyBuildTool.OpenBuildFolder();
        }
    }

    private void DrawStatusRow(string label, string path, bool ok)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(ok ? "OK" : "Missing", GUILayout.Width(62f));
        EditorGUILayout.LabelField(label, path);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFolderCount(string label, string folder)
    {
        int count = AssetDatabase.IsValidFolder(folder)
            ? AssetDatabase.FindAssets("t:Texture2D", new[] { folder }).Length
            : 0;
        EditorGUILayout.LabelField(label, $"{count} png frames");
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            DestroyImmediate(existing);
        }
    }
}
