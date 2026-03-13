using System.Collections.Generic;
using UnityEngine;

public class DigitSegmenter : MonoBehaviour
{
    [Header("Thresholding")]
    [SerializeField] private float whiteThreshold = 0.1f;
    [SerializeField] private bool invertInput = false; // usually false for mask textures

    [Header("Blob Filtering")]
    [SerializeField] private int minBlobPixels = 20;
    [SerializeField] private int minWidth = 3;
    [SerializeField] private int minHeight = 6;
    [SerializeField] private float minFillRatio = 0.02f;

    [Header("MNIST / EMNIST Formatting")]
    [SerializeField] private int outputSize = 28;
    [SerializeField] private int fittedDigitSize = 20;
    [SerializeField] private int padding = 2;
    [SerializeField] private bool applyDilation = false;

    [Header("Orientation Debug")]
    [SerializeField] private bool rotate90Clockwise = false;
    [SerializeField] private bool rotate90CounterClockwise = false;
    [SerializeField] private bool flipHorizontal = false;
    [SerializeField] private bool flipVertical = false;

    public struct DigitCandidate
    {
        public RectInt bounds;
        public float[] mnistPixels;
    }

    public List<DigitCandidate> ExtractDigits(Texture2D source)
    {
        int width = source.width;
        int height = source.height;

        Color[] pixels = source.GetPixels();
        bool[] foreground = new bool[width * height];
        bool[] visited = new bool[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            float v = GetSourceValue(pixels[i]);
            foreground[i] = v >= whiteThreshold;
        }

        List<DigitCandidate> results = new List<DigitCandidate>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        List<Vector2Int> blobPixels = new List<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int startIndex = y * width + x;
                if (visited[startIndex] || !foreground[startIndex])
                    continue;

                blobPixels.Clear();
                queue.Clear();

                visited[startIndex] = true;
                queue.Enqueue(new Vector2Int(x, y));

                int minX = x, maxX = x, minY = y, maxY = y;

                while (queue.Count > 0)
                {
                    Vector2Int p = queue.Dequeue();
                    blobPixels.Add(p);

                    if (p.x < minX) minX = p.x;
                    if (p.x > maxX) maxX = p.x;
                    if (p.y < minY) minY = p.y;
                    if (p.y > maxY) maxY = p.y;

                    TryVisit(p.x - 1, p.y);
                    TryVisit(p.x + 1, p.y);
                    TryVisit(p.x, p.y - 1);
                    TryVisit(p.x, p.y + 1);
                    TryVisit(p.x - 1, p.y - 1);
                    TryVisit(p.x + 1, p.y - 1);
                    TryVisit(p.x - 1, p.y + 1);
                    TryVisit(p.x + 1, p.y + 1);
                }

                int blobWidth = maxX - minX + 1;
                int blobHeight = maxY - minY + 1;
                int boxArea = blobWidth * blobHeight;

                if (blobPixels.Count < minBlobPixels)
                    continue;

                if (blobWidth < minWidth || blobHeight < minHeight)
                    continue;

                float fillRatio = blobPixels.Count / (float)boxArea;
                if (fillRatio < minFillRatio)
                    continue;

                RectInt bounds = new RectInt(minX, minY, blobWidth, blobHeight);
                float[] mnist = BuildMnistTensor(source, bounds);

                results.Add(new DigitCandidate
                {
                    bounds = bounds,
                    mnistPixels = mnist
                });

                void TryVisit(int nx, int ny)
                {
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        return;

                    int idx = ny * width + nx;
                    if (visited[idx] || !foreground[idx])
                        return;

                    visited[idx] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        results.Sort((a, b) => a.bounds.x.CompareTo(b.bounds.x));
        return results;
    }

    private float[] BuildMnistTensor(Texture2D source, RectInt bounds)
    {
        int cropX = Mathf.Max(0, bounds.x - padding);
        int cropY = Mathf.Max(0, bounds.y - padding);
        int cropW = Mathf.Min(source.width - cropX, bounds.width + padding * 2);
        int cropH = Mathf.Min(source.height - cropY, bounds.height + padding * 2);

        Color[] crop = source.GetPixels(cropX, cropY, cropW, cropH);

        float[,] src = new float[cropH, cropW];
        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                Color c = crop[y * cropW + x];
                src[y, x] = GetSourceValue(c);
            }
        }

        float scale = Mathf.Min(
            fittedDigitSize / (float)cropW,
            fittedDigitSize / (float)cropH
        );

        int scaledW = Mathf.Max(1, Mathf.RoundToInt(cropW * scale));
        int scaledH = Mathf.Max(1, Mathf.RoundToInt(cropH * scale));

        float[,] scaled = new float[scaledH, scaledW];

        for (int y = 0; y < scaledH; y++)
        {
            for (int x = 0; x < scaledW; x++)
            {
                float srcX = (x + 0.5f) / scale - 0.5f;
                float srcY = (y + 0.5f) / scale - 0.5f;
                scaled[y, x] = SampleBilinear(src, cropW, cropH, srcX, srcY);
            }
        }

        float[,] canvas = new float[outputSize, outputSize];

        int offsetX = (outputSize - scaledW) / 2;
        int offsetY = (outputSize - scaledH) / 2;

        for (int y = 0; y < scaledH; y++)
        {
            for (int x = 0; x < scaledW; x++)
            {
                canvas[y + offsetY, x + offsetX] = scaled[y, x];
            }
        }

        RecenterByMass(ref canvas);

        if (applyDilation)
            canvas = Dilate(canvas, outputSize);

        ApplyOrientation(ref canvas);

        float[] output = new float[outputSize * outputSize];
        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                output[y * outputSize + x] = canvas[y, x] - 0.5f;
            }
        }

        return output;
    }

    private float GetSourceValue(Color c)
    {
        float v = c.a; // use alpha from the mask texture

        if (invertInput)
            v = 1f - v;

        return Mathf.Clamp01(v);
    }

    private float SampleBilinear(float[,] src, int srcW, int srcH, float x, float y)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, srcW - 1);
        int x1 = Mathf.Clamp(x0 + 1, 0, srcW - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, srcH - 1);
        int y1 = Mathf.Clamp(y0 + 1, 0, srcH - 1);

        float tx = x - x0;
        float ty = y - y0;

        float a = Mathf.Lerp(src[y0, x0], src[y0, x1], tx);
        float b = Mathf.Lerp(src[y1, x0], src[y1, x1], tx);

        return Mathf.Lerp(a, b, ty);
    }

    private void RecenterByMass(ref float[,] canvas)
    {
        float sum = 0f;
        float cx = 0f;
        float cy = 0f;

        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                float v = canvas[y, x];
                sum += v;
                cx += x * v;
                cy += y * v;
            }
        }

        if (sum <= 0.0001f)
            return;

        cx /= sum;
        cy /= sum;

        int targetX = outputSize / 2;
        int targetY = outputSize / 2;
        int shiftX = Mathf.RoundToInt(targetX - cx);
        int shiftY = Mathf.RoundToInt(targetY - cy);

        float[,] shifted = new float[outputSize, outputSize];

        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                int sx = x - shiftX;
                int sy = y - shiftY;

                if (sx >= 0 && sx < outputSize && sy >= 0 && sy < outputSize)
                    shifted[y, x] = canvas[sy, sx];
            }
        }

        canvas = shifted;
    }

    private float[,] Dilate(float[,] src, int size)
    {
        float[,] dst = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float maxVal = 0f;

                for (int oy = -1; oy <= 1; oy++)
                {
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox;
                        int ny = y + oy;

                        if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                            maxVal = Mathf.Max(maxVal, src[ny, nx]);
                    }
                }

                dst[y, x] = maxVal;
            }
        }

        return dst;
    }

    private void ApplyOrientation(ref float[,] canvas)
    {
        if (rotate90Clockwise)
            canvas = Rotate90CW(canvas);

        if (rotate90CounterClockwise)
            canvas = Rotate90CCW(canvas);

        if (flipHorizontal)
            canvas = FlipHorizontal(canvas);

        if (flipVertical)
            canvas = FlipVertical(canvas);
    }

    private float[,] Rotate90CW(float[,] src)
    {
        float[,] dst = new float[outputSize, outputSize];
        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                dst[y, x] = src[outputSize - 1 - x, y];
            }
        }
        return dst;
    }

    private float[,] Rotate90CCW(float[,] src)
    {
        float[,] dst = new float[outputSize, outputSize];
        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                dst[y, x] = src[x, outputSize - 1 - y];
            }
        }
        return dst;
    }

    private float[,] FlipHorizontal(float[,] src)
    {
        float[,] dst = new float[outputSize, outputSize];
        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                dst[y, x] = src[y, outputSize - 1 - x];
            }
        }
        return dst;
    }

    private float[,] FlipVertical(float[,] src)
    {
        float[,] dst = new float[outputSize, outputSize];
        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                dst[y, x] = src[outputSize - 1 - y, x];
            }
        }
        return dst;
    }

    public Texture2D CreateDebugTexture(float[] mnistPixels)
    {
        Texture2D tex = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);

        Color[] colors = new Color[outputSize * outputSize];

        for (int y = 0; y < outputSize; y++)
        {
            for (int x = 0; x < outputSize; x++)
            {
                int idx = y * outputSize + x;
                float v = mnistPixels[idx] + 0.5f;
                colors[idx] = new Color(v, v, v, 1f);
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }
}