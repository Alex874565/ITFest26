using System.Collections.Generic;
using UnityEngine;

public class DigitDebugOverlay : MonoBehaviour
{
    public List<RectInt> Boxes = new List<RectInt>();
    public Drawer Drawer;

    private void OnGUI()
    {
        if (Drawer == null || Drawer.DrawTexture == null)
            return;

        GUI.color = Color.red;

        foreach (RectInt box in Boxes)
        {
            Rect screenRect = TextureRectToScreenRect(box, Drawer);
            DrawRectOutline(screenRect, 2f);
        }
    }

    private Rect TextureRectToScreenRect(RectInt texRect, Drawer draw)
    {
        float sx = Screen.width / (float)draw.DrawTexture.width;
        float sy = Screen.height / (float)draw.DrawTexture.height;

        return new Rect(
            texRect.x * sx,
            Screen.height - (texRect.y + texRect.height) * sy,
            texRect.width * sx,
            texRect.height * sy
        );
    }

    private void DrawRectOutline(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
}