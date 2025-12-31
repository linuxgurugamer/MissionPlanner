using UnityEngine;

using static MissionPlanner.RegisterToolbar;

public static class SpriteSheetIMGUI
{
    // Last generated sheet:
    // 1536 x 1024 => 6 cols x 4 rows, 256px cells
    public const int LastSheetWidthPx = 1488;
    public const int LastSheetHeightPx = 990;
    public const int LastSheetCols = 6;
    public const int LastSheetRows = 3;
    public const int LastSheetCellPx = 256;

    /// <summary>
    /// Draw one frame from the last generated 1536x1024 sheet (6x4 grid, 256px cells).
    /// frameCount defaults to 20 for your intended loop length.
    /// </summary>
    public static void DrawFrame_LastSheet(
        Rect destRect,
        Texture2D texture,
        int frameIndex,
        int frameCount = 20)
    {
        DrawFrame(
            destRect,
            texture,
            frameIndex,
            columns: LastSheetCols,
            rows: LastSheetRows,
            frameCount: frameCount,
            cellWidthPx: 248 - 24, //LastSheetCellPx,
            cellHeightPx: 332 - 24, //LastSheetCellPx,
            offsetXPx: 24,
            offsetYPx: 24,
            padXPx: 24,
            padYPx: 24,
            sheetIsTopLeftOrigin: true
        );
    }

    /// <summary>
    /// Draw one frame from a sprite sheet using UVs (IMGUI). Supports arbitrary grid layouts,
    /// optional padding/offset, and optional frameCount for looping.
    /// </summary>
    /// <param name="destRect">On-screen rect (e.g. 32x32)</param>
    /// <param name="texture">Sprite sheet texture</param>
    /// <param name="frameIndex">Zero-based index (will wrap)</param>
    /// <param name="columns">Number of columns in the sheet grid</param>
    /// <param name="rows">Number of rows in the sheet grid</param>
    /// <param name="frameCount">If >0, wraps using this count; otherwise uses columns*rows</param>
    /// <param name="cellWidthPx">Source cell width in the sheet (0 = auto)</param>
    /// <param name="cellHeightPx">Source cell height in the sheet (0 = auto)</param>
    /// <param name="offsetXPx">Left offset in pixels before the first cell</param>
    /// <param name="offsetYPx">Top offset in pixels before the first cell</param>
    /// <param name="padXPx">Horizontal padding between cells in pixels</param>
    /// <param name="padYPx">Vertical padding between cells in pixels</param>
    /// <param name="sheetIsTopLeftOrigin">
    /// True if your grid indexing is top-left (typical sprite sheets). IMGUI UVs are still bottom-left;
    /// this flag just controls how row index maps into UVs.
    /// </param>
    public static void DrawFrame(
        Rect destRect,
        Texture2D texture,
        int frameIndex,
        int columns,
        int rows,
        int frameCount = 0,
        int cellWidthPx = 0,
        int cellHeightPx = 0,
        int offsetXPx = 0,
        int offsetYPx = 0,
        int padXPx = 0,
        int padYPx = 0,
        bool sheetIsTopLeftOrigin = true)
    {
        if (texture == null) return;
        if (columns <= 0 || rows <= 0) return;

        int maxFrames = columns * rows;
        int usedFrames = (frameCount > 0 && frameCount <= maxFrames) ? frameCount : maxFrames;

        frameIndex %= usedFrames;
        if (frameIndex < 0) frameIndex += usedFrames;

        // Infer cell size if not provided (accounts for optional padding + offset)
        if (cellWidthPx <= 0)
        {
            int usableW = texture.width - offsetXPx - (padXPx * (columns - 1));
            cellWidthPx = usableW / columns;
        }

        if (cellHeightPx <= 0)
        {
            int usableH = texture.height - offsetYPx - (padYPx * (rows - 1));
            cellHeightPx = usableH / rows;
        }

        int col = frameIndex % columns;
        int row = frameIndex / columns;

        // Convert "sheet row" (top-left indexing) into pixel Y from top.
        int rowFromTop = sheetIsTopLeftOrigin ? row : (rows - 1 - row);

        int srcXPx = offsetXPx + col * (cellWidthPx + padXPx);
        int srcYPxFromTop = offsetYPx + rowFromTop * (cellHeightPx + padYPx);

        // IMGUI UVs: bottom-left origin, so convert top-based Y into bottom-based UV rect
        float uMin = (float)srcXPx / texture.width;
        float uMax = (float)(srcXPx + cellWidthPx) / texture.width;

        float vMax = 1f - (float)srcYPxFromTop / texture.height;
        float vMin = 1f - (float)(srcYPxFromTop + cellHeightPx) / texture.height;

        //Log.Info($"DrawFrame        srcXPx: {srcXPx}  srcYPxFromTop: {srcYPxFromTop}  uMin: {uMin}  uMax: {uMax}  vMin: {vMin}  vMax: {vMax}");


        Rect uv = new Rect(uMin, vMin, uMax - uMin, vMax - vMin);
        //Log.Info("uv: " + uv.ToString());
        GUI.DrawTextureWithTexCoords(destRect, texture, uv, true);
    }


    public static Texture2D GetFrame_LastSheet(
    Texture2D texture,
    int frameIndex,
    int frameCount = 20)
    {
        return GetFrameTexture(
            texture,
            frameIndex,
            columns: LastSheetCols,
            rows: LastSheetRows,
            frameCount: frameCount,
            cellWidthPx: 248 - 24, //LastSheetCellPx,
            cellHeightPx: 332 - 24, //LastSheetCellPx,
            offsetXPx: 24,
            offsetYPx: 24,
            padXPx: 24,
            padYPx: 24,
            sheetIsTopLeftOrigin: true
        );
    }

    public static Texture2D GetFrameTexture(
            Texture2D texture,
            int frameIndex,
            int columns,
            int rows,
            int frameCount = 0,
            int cellWidthPx = 0,
            int cellHeightPx = 0,
            int offsetXPx = 0,
            int offsetYPx = 0,
            int padXPx = 0,
            int padYPx = 0,
            bool sheetIsTopLeftOrigin = true,
            bool pointFilter = true)
    {
        if (texture == null) return null;
        if (columns <= 0 || rows <= 0) return null;

        int maxFrames = columns * rows;
        int usedFrames = (frameCount > 0 && frameCount <= maxFrames) ? frameCount : maxFrames;

        frameIndex %= usedFrames;
        if (frameIndex < 0) frameIndex += usedFrames;

        // Infer cell size if not provided (accounts for optional padding + offset)
        if (cellWidthPx <= 0)
        {
            int usableW = texture.width - offsetXPx - (padXPx * (columns - 1));
            cellWidthPx = usableW / columns;
        }

        if (cellHeightPx <= 0)
        {
            int usableH = texture.height - offsetYPx - (padYPx * (rows - 1));
            cellHeightPx = usableH / rows;
        }

        int col = frameIndex % columns;
        int row = frameIndex / columns;

        // Convert "sheet row" (top-left indexing) into pixel Y from top.
        int rowFromTop = sheetIsTopLeftOrigin ? row : (rows - 1 - row);

        int srcXPx = offsetXPx + col * (cellWidthPx + padXPx);
        int srcYPxFromTop = offsetYPx + rowFromTop * (cellHeightPx + padYPx);

        // IMGUI UVs: bottom-left origin, so convert top-based Y into bottom-based UV rect
        float uMin = (float)srcXPx; // / texture.width;
        float uMax = (float)(srcXPx + cellWidthPx); // / texture.width;

        float vMax = 1f - (float)srcYPxFromTop; /// texture.height;
        float vMin = 1f - (float)(srcYPxFromTop + cellHeightPx); // / texture.height;

        //Log.Info($"GetFrameTexture  srcXPx: {srcXPx}  srcYPxFromTop: {srcYPxFromTop}  uMin: {uMin}  uMax: {uMax}  vMin: {vMin}  vMax: {vMax}");

        //Rect uv = new Rect(uMin, vMin, uMax - uMin, vMax - vMin);

        var frame = ExtractTexture(texture, (int)uMin, (int)vMin, (int)(uMax - uMin), (int)(vMax - vMin));


        return frame;
    }

    /// <summary>
    /// Extracts a rectangular region from a texture and returns it as a new Texture2D.
    /// (x, y) is bottom-left, matching Unity texture coordinates.
    /// </summary>
    public static Texture2D ExtractTexture(
        Texture2D source,
        int x,
        int y,
        int width,
        int height,
        TextureFormat format = TextureFormat.ARGB32,
        bool pointFilter = true)
    {
        if (source == null)
            return null;
        //Log.Info($"ExtractTexture    x: {x}  y: {y}  width: {width}  height: {height}");

        // Clamp to texture bounds (defensive)
        x = Mathf.Clamp(x, 0, source.width - 1);
        y = Mathf.Clamp(y, 0, source.height - 1);

        width = Mathf.Clamp(width, 1, source.width - x);
        height = Mathf.Clamp(height, 1, source.height - y);

        Color[] pixels = source.GetPixels(x, y, width, height);

        Texture2D result = new Texture2D(width, height, format, false);
        result.SetPixels(pixels);
        result.Apply(false, false);

        if (pointFilter)
            result.filterMode = FilterMode.Point;

        result.wrapMode = TextureWrapMode.Clamp;

        return result;
    }
}

