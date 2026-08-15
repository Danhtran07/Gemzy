using UnityEngine;

public sealed class JewelMatchSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 9f;
    [SerializeField] private float offset;

    public void Configure(SpriteRenderer targetRenderer, Sprite[] animationFrames, float fps)
    {
        spriteRenderer = targetRenderer;
        frames = animationFrames;
        framesPerSecond = fps;
        offset = Random.value * 10f;
    }

    private void Update()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
        {
            return;
        }

        int frame = Mathf.FloorToInt((Time.time + offset) * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frame];
    }
}
