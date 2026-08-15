using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GemzyBuildTool
{
    private const string ScenePath = "Assets/Scenes/Gemzy.unity";
    private const string DefaultWindowsBuildPath = "Builds/Windows/Gemzy.exe";
    private const string DefaultAndroidBuildPath = "Builds/Android/Gemzy.apk";

    [MenuItem("Gemzy/Build/Windows x64")]
    public static void BuildWindowsMenu()
    {
        BuildWindows(DefaultWindowsBuildPath);
    }

    [MenuItem("Gemzy/Build/Android APK")]
    public static void BuildAndroidMenu()
    {
        BuildAndroid(DefaultAndroidBuildPath);
    }

    [MenuItem("Gemzy/Build/Open Build Folder")]
    public static void OpenBuildFolder()
    {
        string absolutePath = Path.GetFullPath("Builds");
        Directory.CreateDirectory(absolutePath);
        EditorUtility.RevealInFinder(absolutePath);
    }

    public static void BuildWindowsFromCommandLine()
    {
        BuildWindows(GetCommandLineOutputPath(DefaultWindowsBuildPath));
    }

    public static void BuildAndroidFromCommandLine()
    {
        BuildAndroid(GetCommandLineOutputPath(DefaultAndroidBuildPath));
    }

    public static void BuildWindowsAt(string outputPath)
    {
        BuildWindows(outputPath);
    }

    public static void BuildAndroidAt(string outputPath)
    {
        BuildAndroid(outputPath);
    }

    private static void BuildWindows(string outputPath)
    {
        Build(outputPath, BuildTarget.StandaloneWindows64, BuildOptions.None);
    }

    private static void BuildAndroid(string outputPath)
    {
        Build(outputPath, BuildTarget.Android, BuildOptions.None);
    }

    private static void Build(string outputPath, BuildTarget target, BuildOptions options)
    {
        GemzyAssetImporter.SyncPixelGemPack();
        GemzySetupWindow.BuildObjectsIntoHierarchy();
        EnsureSceneInBuildSettings();

        string absoluteOutput = Path.GetFullPath(outputPath);
        string outputDirectory = Path.GetDirectoryName(absoluteOutput);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outputPath,
            target = target,
            options = options
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Gemzy build failed: {summary.result}");
        }

        Debug.Log($"Gemzy build completed: {absoluteOutput} ({summary.totalSize} bytes)");
    }

    private static void EnsureSceneInBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
    }

    private static string GetCommandLineOutputPath(string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-buildOutput")
            {
                return args[i + 1];
            }
        }

        return fallback;
    }
}
