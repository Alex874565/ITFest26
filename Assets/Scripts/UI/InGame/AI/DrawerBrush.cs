using UnityEngine;

public class DrawerBrush : MonoBehaviour
{
    [Header("Brush")]
    [SerializeField] private int brushRadius = 10;
    [SerializeField] private float spacing = 0.35f;
    [SerializeField] private float opacity = 0.18f;
    [SerializeField] private float hardness = 0.75f;

    [Header("Shape")]
    [SerializeField] private float stretchAlongStroke = 1.15f;
    [SerializeField] private float squashAcrossStroke = 0.95f;
    [SerializeField] private bool rotateWithStroke = true;

    [Header("Chalk")]
    [SerializeField] private float grain = 0.22f;          // amount of holes
    [SerializeField] private float edgeBreakup = 0.35f;    // more missing pixels near edge
    [SerializeField] private float sizeJitter = 0.05f;
    [SerializeField] private float opacityJitter = 0.05f;
    [SerializeField] private float angleJitterDegrees = 3f;

    [Header("Color")]
    [SerializeField] private Gradient colorGradient;
    [SerializeField] private bool useStrokeGradient = false;
    [SerializeField] private float gradientAdvancePerStamp = 0.015f;

    private Vector2? lastPixelPos;
    private float strokeT;
    private int stampIndex;

    public void BeginStroke()
    {
        lastPixelPos = null;
        strokeT = 0f;
        stampIndex = 0;
    }

    public void Draw(Texture2D visibleTex, Texture2D maskTex, Vector2 pixelPos)
    {
        if (lastPixelPos == null)
        {
            Stamp(visibleTex, maskTex, pixelPos, Vector2.right);
            lastPixelPos = pixelPos;
            visibleTex.Apply(false);
            maskTex.Apply(false);
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

        visibleTex.Apply(false);
        maskTex.Apply(false);
    }

    private void Stamp(Texture2D visibleTex, Texture2D maskTex, Vector2 center, Vector2 strokeDir)
    {
        stampIndex++;

        float sizeMul = 1f + Random.Range(-sizeJitter, sizeJitter);
        float alphaMul = 1f + Random.Range(-opacityJitter, opacityJitter);
        float angleJitter = Random.Range(-angleJitterDegrees, angleJitterDegrees) * Mathf.Deg2Rad;

        float majorRadius = Mathf.Max(1f, brushRadius * stretchAlongStroke * sizeMul);
        float minorRadius = Mathf.Max(1f, brushRadius * squashAcrossStroke * sizeMul);

        Vector2 dir = rotateWithStroke ? strokeDir.normalized : Vector2.right;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        float angle = Mathf.Atan2(dir.y, dir.x) + angleJitter;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        int cx = Mathf.RoundToInt(center.x);
        int cy = Mathf.RoundToInt(center.y);
        int maxRadius = Mathf.CeilToInt(Mathf.Max(majorRadius, minorRadius)) + 1;

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

                if ((uint)px >= visibleTex.width || (uint)py >= visibleTex.height)
                    continue;

                // rotate into local brush space
                float rx = cos * x + sin * y;
                float ry = -sin * x + cos * y;

                float nx = rx / majorRadius;
                float ny = ry / minorRadius;
                float d2 = nx * nx + ny * ny;

                if (d2 > 1f)
                    continue;

                float d = Mathf.Sqrt(d2);

                // cheaper than fancy noise:
                // use a tiny hash/dither so some pixels are skipped
                float edge = Mathf.InverseLerp(0f, 1f, d);
                float skipChance = grain + edge * edgeBreakup;

                float noise = FastPattern(px, py, stampIndex);
                if (noise < skipChance)
                    continue;

                float falloff = SimpleFalloff(d, hardness);
                float a = opacity * alphaMul * falloff;

                if (a <= 0.001f)
                    continue;

                Color dstVisible = visibleTex.GetPixel(px, py);

                Color srcVisible = brushColor;
                srcVisible.a = a;

                visibleTex.SetPixel(px, py, AlphaBlend(dstVisible, srcVisible));

                Color dstMask = maskTex.GetPixel(px, py);
                float outMaskA = Mathf.Max(dstMask.a, a);
                maskTex.SetPixel(px, py, new Color(1f, 1f, 1f, outMaskA));
            }
        }
    }

    private float SimpleFalloff(float d, float hard)
    {
        float edgeStart = Mathf.Lerp(0.55f, 0.92f, hard);

        if (d <= edgeStart)
            return 1f;

        float t = Mathf.InverseLerp(1f, edgeStart, d);
        return t;
    }

    private float FastPattern(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
            h ^= (h >> 13);
            h *= 1274126177;
            h ^= (h >> 16);
            return (h & 1023) / 1023f;
        }
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