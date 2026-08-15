using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class JewelMatchSetupWindow : EditorWindow
{
    private const string SourceRoot = "Assets/Pixel Art Gem Pack - Animated";
    private const string GemAnimationRoot = "Assets/Resources/GemAnimations";
    private const string SparkRoot = "Assets/Resources/Effects/Spark";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    private string windowsBuildPath = "Builds/Windows/JewelMatch.exe";
    private string androidBuildPath = "Builds/Android/JewelMatch.apk";
    private Vector2 scroll;

    [MenuItem("Jewel Match/Setup Window")]
    public static void Open()
    {
        JewelMatchSetupWindow window = GetWindow<JewelMatchSetupWindow>("Jewel Match Setup");
        window.minSize = new Vector2(440f, 560f);
        window.Show();
    }

    [MenuItem("Jewel Match/Hierarchy/Build Game Objects Into Scene")]
    public static void BuildObjectsIntoHierarchy()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        DestroyIfExists("Jewel Match Setup");
        DestroyIfExists("Jewel Match Game");
        DestroyIfExists("Board");
        DestroyIfExists("Jewel Match HUD");

        GameObject root = new GameObject("Jewel Match Game");
        JewelMatchGame game = root.AddComponent<JewelMatchGame>();
        game.BuildGameInEditor();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Jewel Match objects were built into the hierarchy and saved to SampleScene.");
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
        EditorGUILayout.LabelField("Jewel Match Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Dung cua so nay de xem asset dang dung, tao lai hierarchy trong scene, sync asset moi, va build game.", MessageType.Info);
    }

    private void DrawAssetStatus()
    {
        GUILayout.Space(10f);
        EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

        DrawStatusRow("Source pack", SourceRoot, AssetDatabase.IsValidFolder(SourceRoot));
        DrawStatusRow("Runtime gem animations", GemAnimationRoot, AssetDatabase.IsValidFolder(GemAnimationRoot));
        DrawStatusRow("Runtime spark effect", SparkRoot, AssetDatabase.IsValidFolder(SparkRoot));

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
            JewelMatchAssetImporter.SyncPixelGemPack();
        }
    }

    private void DrawSceneTools()
    {
        GUILayout.Space(14f);
        EditorGUILayout.LabelField("Scene / Hierarchy", EditorStyles.boldLabel);
        DrawStatusRow("Main scene", ScenePath, File.Exists(ScenePath));

        EditorGUILayout.HelpBox("Nut nay build object that len Hierarchy va save vao SampleScene: Board, Tiles, Gem, HUD, EventSystem, va runtime component.", MessageType.None);

        if (GUILayout.Button("Build Game Objects Into Hierarchy"))
        {
            BuildObjectsIntoHierarchy();
        }

        if (GUILayout.Button("Open SampleScene"))
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
            JewelMatchBuildTool.BuildWindowsAt(windowsBuildPath);
        }

        if (GUILayout.Button("Build Android APK"))
        {
            JewelMatchBuildTool.BuildAndroidAt(androidBuildPath);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Open Build Folder"))
        {
            JewelMatchBuildTool.OpenBuildFolder();
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
