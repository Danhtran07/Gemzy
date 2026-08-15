using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class GemzyGame
{
    private readonly Tile[,] tiles = new Tile[Width, Height];
    private readonly List<GameObject> effects = new List<GameObject>();
    private Tile selected;
    private int score;
    private int movesLeft;
    private bool busy;
    private bool finished;

    private void RestartGame()
    {
        if (Application.isPlaying)
        {
            StopAllCoroutines();
        }

        ClearTiles();
        ClearEffects();

        score = 0;
        movesLeft = MoveLimit;
        selected = null;
        busy = false;
        finished = false;
        HideResultPanel();

        do
        {
            ClearTiles();
            FillFreshBoard();
        }
        while (!HasPossibleMove());

        UpdateHud("Swap adjacent jewels to match 3+");
    }

    private void FillFreshBoard()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                CreateTile(x, y, PickTypeWithoutMatchingNeighbor(x, y));
            }
        }
    }

    private int PickTypeWithoutMatchingNeighbor(int x, int y)
    {
        int type;
        int guard = 0;
        do
        {
            type = Random.Range(0, GemCount());
            guard++;
        }
        while (guard < 40 && WouldCreateMatch(x, y, type));

        return type;
    }

    private bool WouldCreateMatch(int x, int y, int type)
    {
        bool horizontal = x >= 2 && tiles[x - 1, y] != null && tiles[x - 2, y] != null
            && tiles[x - 1, y].Type == type && tiles[x - 2, y].Type == type;
        bool vertical = y >= 2 && tiles[x, y - 1] != null && tiles[x, y - 2] != null
            && tiles[x, y - 1].Type == type && tiles[x, y - 2].Type == type;
        return horizontal || vertical;
    }

    private void TrySelectTile()
    {
        Vector3 world = GetPointerWorldPosition();
        int x = Mathf.RoundToInt((world.x / CellSize) + (Width - 1) * 0.5f);
        int y = Mathf.RoundToInt(((world.y - BoardLift) / CellSize) + (Height - 1) * 0.5f);

        if (!Inside(x, y))
        {
            SetSelected(null);
            return;
        }

        Tile tile = tiles[x, y];
        if (selected == null)
        {
            SetSelected(tile);
            return;
        }

        if (selected == tile)
        {
            SetSelected(null);
            return;
        }

        if (AreAdjacent(selected, tile))
        {
            StartCoroutine(TrySwap(selected, tile));
            SetSelected(null);
        }
        else
        {
            SetSelected(tile);
        }
    }

    private IEnumerator TrySwap(Tile a, Tile b)
    {
        busy = true;
        SwapTiles(a, b);
        yield return WaitForTiles();

        HashSet<Tile> matches = FindMatches();
        if (matches.Count == 0)
        {
            SwapTiles(a, b);
            UpdateHud("That swap needs a match");
            yield return WaitForTiles();
            busy = false;
            yield break;
        }

        movesLeft--;
        yield return ResolveBoard(matches);
        if (!CheckEndState())
        {
            busy = false;
        }
    }

    private IEnumerator ResolveBoard(HashSet<Tile> matches)
    {
        int chain = 1;
        while (matches.Count > 0)
        {
            int points = matches.Count * 10 * chain;
            score += points;
            UpdateHud(chain > 1 ? "Combo x" + chain : "Nice match");
            ShowScoreFeedback(points, chain);

            foreach (Tile tile in matches)
            {
                SpawnPop(tile.GameObject.transform.position, tile.Renderer.color);
                SafeDestroy(tile.GameObject);
                tiles[tile.X, tile.Y] = null;
            }

            yield return new WaitForSeconds(0.14f);
            CollapseColumns();
            FillEmptySlots();
            yield return WaitForTiles();

            chain++;
            matches = FindMatches();
        }
    }

    private void CollapseColumns()
    {
        for (int x = 0; x < Width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < Height; y++)
            {
                Tile tile = tiles[x, y];
                if (tile == null)
                {
                    continue;
                }

                if (writeY != y)
                {
                    tiles[x, writeY] = tile;
                    tiles[x, y] = null;
                    tile.Y = writeY;
                }

                writeY++;
            }
        }
    }

    private void FillEmptySlots()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x, y] == null)
                {
                    Tile tile = CreateTile(x, y, Random.Range(0, GemCount()));
                    tile.GameObject.transform.position = TilePosition(x, y + Height);
                }
            }
        }
    }

    private HashSet<Tile> FindMatches()
    {
        HashSet<Tile> matches = new HashSet<Tile>();

        for (int y = 0; y < Height; y++)
        {
            int runStart = 0;
            for (int x = 1; x <= Width; x++)
            {
                bool same = x < Width && tiles[x, y] != null && tiles[runStart, y] != null
                    && tiles[x, y].Type == tiles[runStart, y].Type;
                if (same)
                {
                    continue;
                }

                if (x - runStart >= 3)
                {
                    for (int i = runStart; i < x; i++)
                    {
                        matches.Add(tiles[i, y]);
                    }
                }

                runStart = x;
            }
        }

        for (int x = 0; x < Width; x++)
        {
            int runStart = 0;
            for (int y = 1; y <= Height; y++)
            {
                bool same = y < Height && tiles[x, y] != null && tiles[x, runStart] != null
                    && tiles[x, y].Type == tiles[x, runStart].Type;
                if (same)
                {
                    continue;
                }

                if (y - runStart >= 3)
                {
                    for (int i = runStart; i < y; i++)
                    {
                        matches.Add(tiles[x, i]);
                    }
                }

                runStart = y;
            }
        }

        return matches;
    }

    private Tile CreateTile(int x, int y, int type)
    {
        GameObject tileObject = CreateSpriteObject("Gem", tileRoot, TilePosition(x, y), GemSprite(type));
        tileObject.transform.localScale = Vector3.one * 0.74f;

        SpriteRenderer renderer = tileObject.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 3;
        renderer.color = Color.white;
        Sprite[] animationFrames = GemAnimation(type);

        Tile tile = new Tile
        {
            X = x,
            Y = y,
            Type = type,
            GameObject = tileObject,
            Renderer = renderer,
            Animation = tileObject.AddComponent<GemzySpriteAnimation>()
        };
        tile.Animation.Configure(renderer, animationFrames, 9f);

        tiles[x, y] = tile;
        return tile;
    }

    private GameObject CreateSpriteObject(string name, Transform parent, Vector3 position, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;
        return obj;
    }

    private void SwapTiles(Tile a, Tile b)
    {
        tiles[a.X, a.Y] = b;
        tiles[b.X, b.Y] = a;

        int ax = a.X;
        int ay = a.Y;
        a.X = b.X;
        a.Y = b.Y;
        b.X = ax;
        b.Y = ay;
    }

    private IEnumerator WaitForTiles()
    {
        float timer = 0f;
        while (timer < 0.22f)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void AnimateTiles()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Tile tile = tiles[x, y];
                if (tile == null || tile.GameObject == null)
                {
                    continue;
                }

                Vector3 target = TilePosition(tile.X, tile.Y);
                tile.GameObject.transform.position = Vector3.Lerp(tile.GameObject.transform.position, target, Time.deltaTime * 14f);
                float selectedScale = tile == selected ? 0.88f : 0.74f;
                tile.GameObject.transform.localScale = Vector3.Lerp(tile.GameObject.transform.localScale, Vector3.one * selectedScale, Time.deltaTime * 18f);
            }
        }

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i] == null)
            {
                effects.RemoveAt(i);
            }
        }
    }

    private void SpawnPop(Vector3 position, Color color)
    {
        Sprite startSprite = sparkFrames != null && sparkFrames.Length > 0 ? sparkFrames[0] : squareSprite;
        GameObject pop = CreateSpriteObject("Match Spark", boardRoot, new Vector3(position.x, position.y, -0.1f), startSprite);
        pop.transform.localScale = Vector3.one * 0.9f;
        SpriteRenderer renderer = pop.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 5;
        renderer.color = sparkFrames != null && sparkFrames.Length > 0 ? Color.white : new Color(color.r, color.g, color.b, 0.45f);
        effects.Add(pop);
        StartCoroutine(AnimatePop(pop, renderer));
    }

    private IEnumerator AnimatePop(GameObject pop, SpriteRenderer renderer)
    {
        float timer = 0f;
        while (timer < 0.22f && pop != null)
        {
            timer += Time.deltaTime;
            float t = timer / 0.22f;
            if (sparkFrames != null && sparkFrames.Length > 0)
            {
                int frame = Mathf.Min(sparkFrames.Length - 1, Mathf.FloorToInt(t * sparkFrames.Length));
                renderer.sprite = sparkFrames[frame];
                renderer.color = new Color(1f, 1f, 1f, 1f - t);
            }
            else
            {
                pop.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.05f, t);
                renderer.color = new Color(renderer.color.r, renderer.color.g, renderer.color.b, 0.45f * (1f - t));
            }

            yield return null;
        }

        if (pop != null)
        {
            SafeDestroy(pop);
        }
    }

    private bool CheckEndState()
    {
        if (score >= TargetScore)
        {
            finished = true;
            UpdateHud("You cleared the jewel target!");
            ShowResultPanel(true);
            return false;
        }

        if (movesLeft <= 0)
        {
            finished = true;
            UpdateHud("Out of moves - try again");
            ShowResultPanel(false);
            return false;
        }

        if (!HasPossibleMove())
        {
            StartCoroutine(ShuffleBoard());
            return true;
        }

        UpdateHud("Find the next match");
        return false;
    }

    private IEnumerator ShuffleBoard()
    {
        busy = true;
        UpdateHud("Shuffling the board");
        yield return new WaitForSeconds(0.25f);

        do
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (tiles[x, y] != null)
                    {
                        SafeDestroy(tiles[x, y].GameObject);
                        tiles[x, y] = null;
                    }
                }
            }

            FillFreshBoard();
        }
        while (!HasPossibleMove());

        busy = false;
        UpdateHud("Fresh jewels dropped in");
    }

    private bool HasPossibleMove()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (x + 1 < Width && WouldSwapMakeMatch(x, y, x + 1, y))
                {
                    return true;
                }

                if (y + 1 < Height && WouldSwapMakeMatch(x, y, x, y + 1))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool WouldSwapMakeMatch(int ax, int ay, int bx, int by)
    {
        Tile a = tiles[ax, ay];
        Tile b = tiles[bx, by];
        tiles[ax, ay] = b;
        tiles[bx, by] = a;

        bool makesMatch = TileHasMatch(ax, ay) || TileHasMatch(bx, by);

        tiles[ax, ay] = a;
        tiles[bx, by] = b;
        return makesMatch;
    }

    private bool TileHasMatch(int x, int y)
    {
        Tile tile = tiles[x, y];
        if (tile == null)
        {
            return false;
        }

        int horizontal = 1;
        for (int left = x - 1; left >= 0 && tiles[left, y] != null && tiles[left, y].Type == tile.Type; left--)
        {
            horizontal++;
        }

        for (int right = x + 1; right < Width && tiles[right, y] != null && tiles[right, y].Type == tile.Type; right++)
        {
            horizontal++;
        }

        int vertical = 1;
        for (int down = y - 1; down >= 0 && tiles[x, down] != null && tiles[x, down].Type == tile.Type; down--)
        {
            vertical++;
        }

        for (int up = y + 1; up < Height && tiles[x, up] != null && tiles[x, up].Type == tile.Type; up++)
        {
            vertical++;
        }

        return horizontal >= 3 || vertical >= 3;
    }

    private Vector3 GetPointerWorldPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return gameCamera.ScreenToWorldPoint(touchPosition);
        }

        if (Mouse.current != null)
        {
            return gameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        return Vector3.zero;
    }

    private bool PointerDownThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(0);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }

        return false;
    }

    private bool AreAdjacent(Tile a, Tile b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1;
    }

    private bool Inside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    private Vector3 TilePosition(int x, int y)
    {
        return new Vector3((x - (Width - 1) * 0.5f) * CellSize, BoardLift + (y - (Height - 1) * 0.5f) * CellSize, 0f);
    }

    private void ClearTiles()
    {
        if (tileRoot != null)
        {
            for (int i = tileRoot.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(tileRoot.GetChild(i).gameObject);
            }
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (tileRoot == null && tiles[x, y] != null && tiles[x, y].GameObject != null)
                {
                    SafeDestroy(tiles[x, y].GameObject);
                }

                tiles[x, y] = null;
            }
        }
    }

    private void ClearEffects()
    {
        foreach (GameObject effect in effects)
        {
            if (effect != null)
            {
                SafeDestroy(effect);
            }
        }

        effects.Clear();
    }
}
