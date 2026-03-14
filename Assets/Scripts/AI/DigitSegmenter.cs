using System.Collections.Generic;
using UnityEngine;

public class DigitSegmenter : MonoBehaviour
{
    [Header("Thresholding")]
    [SerializeField] private float whiteThreshold = 0.1f;
    [SerializeField] private bool invertInput = false;

    [Header("Blob Filtering")]
    [SerializeField] private int minBlobPixels = 20;
    [SerializeField] private int minWidth = 3;
    [SerializeField] private int minHeight = 6;
    [SerializeField] private float minFillRatio = 0.02f;

    [Header("Gap Tolerance")]
    [SerializeField] private int maxHorizontalGap = 1;
    [SerializeField] private int maxVerticalGap = 3;

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

    private bool[] _foreground;
    private bool[] _visited;
    private int[] _queue;

    public List<DigitCandidate> ExtractDigits(Texture2D source)
    {
        return ExtractDigitsFromPixels(source.GetPixels32(), source.width, source.height);
    }

    public List<DigitCandidate> ExtractDigitsFromPixels(Color32[] pixels, int width, int height)
    {
        int pixelCount = width * height;
        EnsureBuffers(pixelCount);

        for (int i = 0; i < pixelCount; i++)
        {
            float v = GetSourceValue(pixels[i]);
            _foreground[i] = v >= whiteThreshold;
            _visited[i] = false;
        }

        List<DigitCandidate> results = new List<DigitCandidate>(8);

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;

            for (int x = 0; x < width; x++)
            {
                int startIndex = rowStart + x;
                if (_visited[startIndex] || !_foreground[startIndex])
                    continue;

                int head = 0;
                int tail = 0;

                _visited[startIndex] = true;
                _queue[tail++] = startIndex;

                int minX = x;
                int maxX = x;
                int minY = y;
                int maxY = y;
                int blobCount = 0;

                while (head < tail)
                {
                    int idx = _queue[head++];
                    blobCount++;

                    int px = idx % width;
                    int py = idx / width;

                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;

                    VisitNeighborsWithGapTolerance(px, py);
                }

                int blobWidth = maxX - minX + 1;
                int blobHeight = maxY - minY + 1;
                int area = blobWidth * blobHeight;

                if (blobCount < minBlobPixels)
                    continue;

                if (blobWidth < minWidth || blobHeight < minHeight)
                    continue;

                float fillRatio = blobCount / (float)area;
                if (fillRatio < minFillRatio)
                    continue;

                RectInt bounds = new RectInt(minX, minY, blobWidth, blobHeight);
                float[] mnist = BuildMnistTensor(pixels, width, height, bounds);

                results.Add(new DigitCandidate
                {
                    bounds = bounds,
                    mnistPixels = mnist
                });

                void VisitNeighborsWithGapTolerance(int cx, int cy)
                {
                    for (int dy = -maxVerticalGap; dy <= maxVerticalGap; dy++)
                    {
                        for (int dx = -maxHorizontalGap; dx <= maxHorizontalGap; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            int nx = cx + dx;
                            int ny = cy + dy;

                            if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                                continue;

                            int nIdx = ny * width + nx;
                            if (_visited[nIdx] || !_foreground[nIdx])
                                continue;

                            _visited[nIdx] = true;
                            _queue[tail++] = nIdx;
                        }
                    }
                }
            }
        }

        results.Sort((a, b) => a.bounds.x.CompareTo(b.bounds.x));
        return results;
    }

    private void EnsureBuffers(int pixelCount)
    {
        if (_foreground == null || _foreground.Length != pixelCount)
            _foreground = new bool[pixelCount];

        if (_visited == null || _visited.Length != pixelCount)
            _visited = new bool[pixelCount];

        if (_queue == null || _queue.Length != pixelCount)
            _queue = new int[pixelCount];
    }

    private float[] BuildMnistTensor(Color32[] pixels, int sourceWidth, int sourceHeight, RectInt bounds)
    {
        int cropX = Mathf.Max(0, bounds.x - padding);
        int cropY = Mathf.Max(0, bounds.y - padding);
        int cropW = Mathf.Min(sourceWidth - cropX, bounds.width + padding * 2);
        int cropH = Mathf.Min(sourceHeight - cropY, bounds.height + padding * 2);

        float scale = Mathf.Min(
            fittedDigitSize / (float)cropW,
            fittedDigitSize / (float)cropH
        );

        int scaledW = Mathf.Max(1, Mathf.RoundToInt(cropW * scale));
        int scaledH = Mathf.Max(1, Mathf.RoundToInt(cropH * scale));

        float[] scaled = new float[scaledW * scaledH];
        float[] canvas = new float[outputSize * outputSize];

        for (int y = 0; y < scaledH; y++)
        {
            for (int x = 0; x < scaledW; x++)
            {
                float srcX = (x + 0.5f) / scale - 0.5f;
                float srcY = (y + 0.5f) / scale - 0.5f;
                scaled[y * scaledW + x] = SampleBilinear(
                    pixels,
                    sourceWidth,
                    cropX,
                    cropY,
                    cropW,
                    cropH,
                    srcX,
                    srcY
                );
            }
        }

        int offsetX = (outputSize - scaledW) / 2;
        int offsetY = (outputSize - scaledH) / 2;

        for (int y = 0; y < scaledH; y++)
        {
            int dstRow = (y + offsetY) * outputSize;
            int srcRow = y * scaledW;

            for (int x = 0; x < scaledW; x++)
                canvas[dstRow + x + offsetX] = scaled[srcRow + x];
        }

        RecenterByMass(canvas);

        if (applyDilation)
            canvas = Dilate(canvas);

        ApplyOrientation(ref canvas);

        float[] output = new float[outputSize * outputSize];
        for (int i = 0; i < output.Length; i++)
            output[i] = canvas[i] - 0.5f;

        return output;
    }

    private float GetSourceValue(Color32 c)
    {
        float v = c.a / 255f;
        if (invertInput)
            v = 1f - v;
        return v;
    }

    private float SampleBilinear(
        Color32[] pixels,
        int sourceWidth,
        int cropX,
        int cropY,
        int cropW,
        int cropH,
        float x,
        float y)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(x), 0, cropW - 1);
        int x1 = Mathf.Clamp(x0 + 1, 0, cropW - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(y), 0, cropH - 1);
        int y1 = Mathf.Clamp(y0 + 1, 0, cropH - 1);

        float tx = x - x0;
        float ty = y - y0;

        float p00 = GetSourceValue(pixels[(cropY + y0) * sourceWidth + (cropX + x0)]);
        float p10 = GetSourceValue(pixels[(cropY + y0) * sourceWidth + (cropX + x1)]);
        float p01 = GetSourceValue(pixels[(cropY + y1) * sourceWidth + (cropX + x0)]);
        float p11 = GetSourceValue(pixels[(cropY + y1) * sourceWidth + (cropX + x1)]);

        float a = Mathf.Lerp(p00, p10, tx);
        float b = Mathf.Lerp(p01, p11, tx);
        return Mathf.Lerp(a, b, ty);
    }

    private void RecenterByMass(float[] canvas)
    {
        float sum = 0f;
        float cx = 0f;
        float cy = 0f;
        int size = outputSize;

        for (int y = 0; y < size; y++)
        {
            int row = y * size;
            for (int x = 0; x < size; x++)
            {
                float v = canvas[row + x];
                sum += v;
                cx += x * v;
                cy += y * v;
            }
        }

        if (sum <= 0.0001f)
            return;

        cx /= sum;
        cy /= sum;

        int shiftX = Mathf.RoundToInt(size / 2f - cx);
        int shiftY = Mathf.RoundToInt(size / 2f - cy);

        float[] shifted = new float[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int sx = x - shiftX;
                int sy = y - shiftY;

                if ((uint)sx < (uint)size && (uint)sy < (uint)size)
                    shifted[y * size + x] = canvas[sy * size + sx];
            }
        }

        for (int i = 0; i < canvas.Length; i++)
            canvas[i] = shifted[i];
    }

    private float[] Dilate(float[] src)
    {
        int size = outputSize;
        float[] dst = new float[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float maxVal = 0f;

                for (int oy = -1; oy <= 1; oy++)
                {
                    int ny = y + oy;
                    if ((uint)ny >= (uint)size) continue;

                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox;
                        if ((uint)nx >= (uint)size) continue;

                        float v = src[ny * size + nx];
                        if (v > maxVal)
                            maxVal = v;
                    }
                }

                dst[y * size + x] = maxVal;
            }
        }

        return dst;
    }

    private void ApplyOrientation(ref float[] canvas)
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

    private float[] Rotate90CW(float[] src)
    {
        int size = outputSize;
        float[] dst = new float[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                dst[y * size + x] = src[(size - 1 - x) * size + y];
        return dst;
    }

    private float[] Rotate90CCW(float[] src)
    {
        int size = outputSize;
        float[] dst = new float[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                dst[y * size + x] = src[x * size + (size - 1 - y)];
        return dst;
    }

    private float[] FlipHorizontal(float[] src)
    {
        int size = outputSize;
        float[] dst = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            int row = y * size;
            for (int x = 0; x < size; x++)
                dst[row + x] = src[row + (size - 1 - x)];
        }
        return dst;
    }

    private float[] FlipVertical(float[] src)
    {
        int size = outputSize;
        float[] dst = new float[size * size];
        for (int y = 0; y < size; y++)
        {
            int srcRow = (size - 1 - y) * size;
            int dstRow = y * size;
            for (int x = 0; x < size; x++)
                dst[dstRow + x] = src[srcRow + x];
        }
        return dst;
    }

    public Texture2D CreateDebugTexture(float[] mnistPixels)
    {
        Texture2D tex = new Texture2D(outputSize, outputSize, TextureFormat.RGBA32, false);
        Color32[] colors = new Color32[outputSize * outputSize];

        for (int i = 0; i < colors.Length; i++)
        {
            float v = Mathf.Clamp01(mnistPixels[i] + 0.5f);
            byte b = (byte)Mathf.RoundToInt(v * 255f);
            colors[i] = new Color32(b, b, b, 255);
        }

        tex.SetPixels32(colors);
        tex.Apply();
        return tex;
    }
}