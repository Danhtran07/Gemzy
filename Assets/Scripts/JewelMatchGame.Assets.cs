using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class JewelMatchGame
{
    private Sprite[] gemSprites;
    private Sprite[][] gemAnimations;
    private Sprite[] sparkFrames;
    private Sprite squareSprite;

    private void LoadAssets()
    {
        gemSprites = Resources.LoadAll<Sprite>("Gems");
        gemAnimations = LoadGemAnimations();
        sparkFrames = LoadSortedSprites("Effects/Spark");
        squareSprite = CreateSquareSprite();
    }

    private Sprite GemSprite(int type)
    {
        Sprite[] animationFrames = GemAnimation(type);
        if (animationFrames != null && animationFrames.Length > 0)
        {
            return animationFrames[0];
        }

        if (gemSprites != null && gemSprites.Length > 0)
        {
            return gemSprites[type % gemSprites.Length];
        }

        return CreateFallbackGem(type);
    }

    private int GemCount()
    {
        int animatedCount = gemAnimations == null ? 0 : gemAnimations.Length;
        int staticCount = gemSprites == null ? 0 : gemSprites.Length;
        return Mathf.Max(5, Mathf.Max(animatedCount, staticCount));
    }

    private Sprite[] GemAnimation(int type)
    {
        if (gemAnimations == null || gemAnimations.Length == 0)
        {
            return null;
        }

        Sprite[] frames = gemAnimations[type % gemAnimations.Length];
        return frames != null && frames.Length > 0 ? frames : null;
    }

    private Sprite[][] LoadGemAnimations()
    {
        string[] names = { "Blue", "Green", "Red", "Gold", "Purple", "Teal" };
        List<Sprite[]> animations = new List<Sprite[]>();

        foreach (string gemName in names)
        {
            Sprite[] frames = LoadSortedSprites("GemAnimations/" + gemName);
            if (frames.Length > 0)
            {
                animations.Add(frames);
            }
        }

        return animations.ToArray();
    }

    private Sprite[] LoadSortedSprites(string resourcePath)
    {
        return Resources.LoadAll<Sprite>(resourcePath)
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private Sprite CreateFallbackGem(int type)
    {
        Color[] colors =
        {
            new Color(0.2f, 0.55f, 1f),
            new Color(0.15f, 0.85f, 0.42f),
            new Color(1f, 0.35f, 0.28f),
            new Color(0.95f, 0.65f, 0.18f),
            new Color(0.62f, 0.35f, 1f)
        };

        Texture2D texture = new Texture2D(64, 64);
        Color color = colors[type % colors.Length];

        for (int py = 0; py < texture.height; py++)
        {
            for (int px = 0; px < texture.width; px++)
            {
                Vector2 delta = new Vector2(px - 31.5f, py - 31.5f);
                float alpha = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) < 42f ? 1f : 0f;
                float shine = py > 34 && px < 34 ? 1.25f : 1f;
                texture.SetPixel(px, py, new Color(color.r * shine, color.g * shine, color.b * shine, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 64f);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(4, 4);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }
}
