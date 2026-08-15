using System;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public static class GemzyBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartGame()
    {
        if (UnityEngine.Object.FindAnyObjectByType<GemzyGame>() != null)
        {
            return;
        }

        GameObject root = new GameObject("Gemzy Game");
        root.AddComponent<GemzyGame>();
    }
}

public partial class GemzyGame : MonoBehaviour
{
    private const int Width = 8;
    private const int Height = 8;
    private const int MoveLimit = 30;
    private const int TargetScore = 2500;
    private const float CellSize = 0.92f;
    private const float BoardLift = -0.25f;
    private const float BoardCameraPadding = 0.55f;
    private const float MinCameraSize = 5.2f;
    private const float MaxCameraSize = 8.4f;
    private const int MobileTargetFrameRate = 60;

    private void Awake()
    {
        BuildGame();
    }

#if UNITY_EDITOR
    public void BuildGameInEditor()
    {
        BuildGame();
    }
#endif

    private void BuildGame()
    {
        ConfigureMobileRuntime();
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        LoadAssets();
        ConfigureCamera();
        ClearGeneratedObjects();
        CreateScene();
        RestartGame();
    }

    private void Update()
    {
        RefreshMobileLayout();
        AnimateTiles();

        if (!busy && !finished && PointerDownThisFrame())
        {
            TrySelectTile();
        }
    }
}
