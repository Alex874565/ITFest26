using UnityEngine;

public class DrawerBrush : MonoBehaviour
{
    [Header("Brush Shape")]
    [SerializeField] private int brushRadius = 10;
    [SerializeField] private float spacing = 0.18f;
    [SerializeField] private float hardness = 0.3f;      // 0 soft, 1 hard
    [SerializeField] private float opacity = 0.28f;

    [Header("Directional Shape")]
    [SerializeField] private float stretchAlongStroke = 1.8f;
    [SerializeField] private float squashAcrossStroke = 0.85f;
    [SerializeField] private bool rotateWithStroke = true;

    [Header("Painter Feel")]
    [SerializeField] private float sizeJitter = 0.08f;
    [SerializeField] private float opacityJitter = 0.08f;
    [SerializeField] private float angleJitterDegrees = 4f;

    [Header("Color")]
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private bool useStrokeGradient = true;
    [SerializeField] private float gradientAdvancePerStamp = 0.015f;

    private Vector2? lastPixelPos;
    private float strokeT;

    public void BeginStroke()
    {
        lastPixelPos = null;
        strokeT = 0f;
    }

    public void Draw(Texture2D visibleTex, Texture2D maskTex, Vector2 pixelPos)
    {
        if (lastPixelPos == null)
        {
            Stamp(visibleTex, maskTex, pixelPos, Vector2.right);
            lastPixelPos = pixelPos;
            return;
        }

        Vector2 from = lastPixelPos.Value;
        Vector2 to = pixelPos;

        Vector2 delta = to - from;
        float dist = delta.magnitude;
        Vector2 dir = dist > 0.0001f ? delta / dist : Vector2.right;

        float step = Mathf.Max(1f, brushRadius * spacing);
        int count = Mathf.Max(1, Mathf.CeilToInt(dist / step));

        for (int i = 1; i <= count; i++)
        {
            float t = i / (float)count;
            Vector2 p = Vector2.Lerp(from, to, t);
            Stamp(visibleTex, maskTex, p, dir);
        }

        lastPixelPos = pixelPos;

        visibleTex.Apply();
        maskTex.Apply();
    }

    private void Stamp(Texture2D visibleTex, Texture2D maskTex, Vector2 center, Vector2 strokeDir)
    {
        float sizeMul = 1f + Random.Range(-sizeJitter, sizeJitter);
        float alphaMul = 1f + Random.Range(-opacityJitter, opacityJitter);
        float angleJitter = Random.Range(-angleJitterDegrees, angleJitterDegrees) * Mathf.Deg2Rad;

        float majorRadius = Mathf.Max(1f, brushRadius * stretchAlongStroke * sizeMul);
        float minorRadius = Mathf.Max(1f, brushRadius * squashAcrossStroke * sizeMul);

        Vector2 dir = rotateWithStroke ? strokeDir.normalized : Vector2.right;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        float baseAngle = Mathf.Atan2(dir.y, dir.x) + angleJitter;
        float cos = Mathf.Cos(baseAngle);
        float sin = Mathf.Sin(baseAngle);

        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);

        int maxRadius = Mathf.CeilToInt(Mathf.Max(majorRadius, minorRadius)) + 2;

        Color brushColor = useStrokeGradient
            ? colorGradient.Evaluate(Mathf.Repeat(strokeT, 1f))
            : colorGradient.Evaluate(0f);

        strokeT += gradientAdvancePerStamp;

        for (int y = -maxRadius; y <= maxRadius; y++)
        {
            for (int x = -maxRadius; x <= maxRadius; x++)
            {
                int px = cx + x;
                int py = cy + y;

                if (px < 0 || py < 0 || px >= visibleTex.width || py >= visibleTex.height)
                    continue;

                // Rotate sample into brush-local space
                float rx =  cos * x + sin * y;
                float ry = -sin * x + cos * y;

                float nx = rx / majorRadius;
                float ny = ry / minorRadius;
                float d = Mathf.Sqrt(nx * nx + ny * ny);

                if (d > 1f)
                    continue;

                float falloff = SoftFalloff(d, hardness);

                // Slightly stronger center buildup for painter look
                float paint = Mathf.Pow(falloff, 0.85f);

                float a = Mathf.Clamp01(paint * opacity * alphaMul);
                if (a <= 0.001f)
                    continue;

                // Visible texture
                Color dstVisible = visibleTex.GetPixel(px, py);
                Color srcVisible = brushColor;
                srcVisible.a *= a;

                Color blendedVisible = AlphaBlend(dstVisible, srcVisible);
                visibleTex.SetPixel(px, py, blendedVisible);

                // Recognition mask texture: pure white, alpha carries intensity
                Color dstMask = maskTex.GetPixel(px, py);
                float outMaskA = Mathf.Clamp01(Mathf.Max(dstMask.a, a));
                maskTex.SetPixel(px, py, new Color(1f, 1f, 1f, outMaskA));
            }
        }
    }

    private float SoftFalloff(float d, float hardness)
    {
        float softEdgeStart = Mathf.Lerp(0.08f, 0.88f, hardness);

        if (d <= softEdgeStart)
            return 1f;

        float t = Mathf.InverseLerp(1f, softEdgeStart, d);
        return t * t * (3f - 2f * t);
    }

    private Color AlphaBlend(Color dst, Color src)
    {
        float outA = src.a + dst.a * (1f - src.a);
        if (outA <= 0.0001f)
            return Color.clear;

        float outR = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / outA;
        float outG = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / outA;
        float outB = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / outA;

        return new Color(outR, outG, outB, outA);
    }
}