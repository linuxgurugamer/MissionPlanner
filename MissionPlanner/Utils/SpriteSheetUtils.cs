#if false
using UnityEngine;

public static class SpriteSheetUtils
{
    /// <summary>
    /// Extracts a single sprite from a sprite sheet using a linear index.
    /// </summary>
    /// <param name="texture">The sprite sheet texture</param>
    /// <param name="frameIndex">Zero-based frame index</param>
    /// <param name="frameWidth">Frame width in pixels (32)</param>
    /// <param name="frameHeight">Frame height in pixels (32)</param>
    /// <returns>Sprite for the requested frame</returns>
    public static Sprite GetFrame(
        Texture2D texture,
        int frameIndex,
        int frameWidth = 32,
        int frameHeight = 32)
    {
        int columns = texture.width / frameWidth;
        int rows = texture.height / frameHeight;
        int totalFrames = columns * rows;

        frameIndex = frameIndex % totalFrames;
        if (frameIndex < 0)
            frameIndex += totalFrames;

        int x = frameIndex % columns;
        int y = rows - 1 - (frameIndex / columns); // Unity textures start bottom-left

        Rect rect = new Rect(
            x * frameWidth,
            y * frameHeight,
            frameWidth,
            frameHeight
        );

        return Sprite.Create(
            texture,
            rect,
            new Vector2(0.5f, 0.5f),
            frameWidth   // pixels per unit (1 unit = 32px)
        );
    }
}
#endif