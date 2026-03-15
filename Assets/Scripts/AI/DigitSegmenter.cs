using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DigitSegmenter : MonoBehaviour
{
    [Header("Thresholding")]
    [SerializeField] private float whiteThreshold = 0.08f;
    [SerializeField] private bool invertInput = false;

    [Header("Blob Filtering")]
    [SerializeField] private int minBlobPixels = 12;
    [SerializeField] private int minWidth = 2;
    [SerializeField] private int minHeight = 4;
    [SerializeField] private float minFillRatio = 0.015f;
    [SerializeField] private int maxCandidates = 6;

    [Header("Binary Cleanup")]
    [SerializeField] private bool useBinaryClose = true;
    [SerializeField] private bool useBinaryCloseOnWebGLOnly = true;

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

    private struct ScaledBlobThresholds
    {
        public int MinBlobPixels;
        public int MinWidth;
        public int MinHeight;
        public float MinFillRatio;
    }

    private sealed class Scratch
    {
        public bool[] foreground;
        public bool[] tempBinary;
        public bool[] visited;
        public int[] queue;

        public float[] canvasA;
        public float[] canvasB;
        public float[] scaled;

        public void EnsurePixelBuffers(int pixelCount)
        {
            if (foreground == null || foreground.Length < pixelCount)
                foreground = new bool[pixelCount];

            if (tempBinary == null || tempBinary.Length < pixelCount)
                tempBinary = new bool[pixelCount];

            if (visited == null || visited.Length < pixelCount)
                visited = new bool[pixelCount];

            if (queue == null || queue.Length < pixelCount)
                queue = new int[pixelCount];
        }

        public void EnsureCanvasBuffers(int canvasSize)
        {
            int len = canvasSize * canvasSize;

            if (canvasA == null || canvasA.Length != len)
                canvasA = new float[len];

            if (canvasB == null || canvasB.Length != len)
                canvasB = new float[len];
        }

        public void EnsureScaledBuffer(int length)
        {
            if (scaled == null || scaled.Length < length)
                scaled = new float[length];
        }
    }

    private readonly Scratch _mainScratch = new Scratch();

    public List<DigitCandidate> ExtractDigits(Texture2D source)
    {
        return ExtractDigitsFromPixels(source.GetPixels32(), source.width, source.height, 1f, 1f);
    }

    public List<DigitCandidate> ExtractDigitsFromPixels(
        Color32[] pixels,
        int width,
        int height,
        float scaleX = 1f,
        float scaleY = 1f)
    {
        var results = new List<DigitCandidate>(8);
        ExtractDigitsFromPixelsInto(pixels, width, height, results, _mainScratch, scaleX, scaleY);
        return results;
    }

    public List<DigitCandidate> ExtractDigitsFromPixelsThreadSafe(
        Color32[] pixels,
        int width,
        int height,
        float scaleX = 1f,
        float scaleY = 1f)
    {
        var results = new List<DigitCandidate>(8);
        var scratch = new Scratch();
        ExtractDigitsFromPixelsInto(pixels, width, height, results, scratch, scaleX, scaleY);
        return results;
    }

    public IEnumerator ExtractDigitsFromPixelsCoroutine(
        Color32[] pixels,
        int width,
        int height,
        float scaleX,
        float scaleY,
        List<DigitCandidate> results,
        int workBudgetPerYield = 20000)
    {
        results.Clear();

        int pixelCount = width * height;
        _mainScratch.EnsurePixelBuffers(pixelCount);
        _mainScratch.EnsureCanvasBuffers(outputSize);

        ScaledBlobThresholds thresholds = GetScaledBlobThresholds(scaleX, scaleY);

        bool[] foreground = _mainScratch.foreground;
        bool[] tempBinary = _mainScratch.tempBinary;
        bool[] visited = _mainScratch.visited;
        int[] queue = _mainScratch.queue;

        int work = 0;

        for (int i = 0; i < pixelCount; i++)
        {
            foreground[i] = GetSourceValue(pixels[i]) >= whiteThreshold;
            visited[i] = false;

            if (++work >= workBudgetPerYield)
            {
                work = 0;
                yield return null;
            }
        }

        if (ShouldUseBinaryClose())
        {
            BinaryDilate4(foreground, tempBinary, width, height);
            if (++work >= workBudgetPerYield)
            {
                work = 0;
                yield return null;
            }

            BinaryErode4(tempBinary, foreground, width, height);
            if (++work >= workBudgetPerYield)
            {
                work = 0;
                yield return null;
            }
        }

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;

            for (int x = 0; x < width; x++)
            {
                int startIndex = rowStart + x;
                if (visited[startIndex] || !foreground[startIndex])
                    continue;

                int head = 0;
                int tail = 0;

                visited[startIndex] = true;
                queue[tail++] = startIndex;

                int minX = x;
                int maxX = x;
                int minY = y;
                int maxY = y;
                int blobCount = 0;

                while (head < tail)
                {
                    int idx = queue[head++];
                    blobCount++;

                    int px = idx % width;
                    int py = idx / width;

                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;

                    TryEnqueue(px - 1, py, width, height, foreground, visited, queue, ref tail);
                    TryEnqueue(px + 1, py, width, height, foreground, visited, queue, ref tail);
                    TryEnqueue(px, py - 1, width, height, foreground, visited, queue, ref tail);
                    TryEnqueue(px, py + 1, width, height, foreground, visited, queue, ref tail);

                    if (++work >= workBudgetPerYield)
                    {
                        work = 0;
                        yield return null;
                    }
                }

                if (!PassesBlobFilter(blobCount, minX, maxX, minY, maxY, thresholds))
                    continue;

                RectInt bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                float[] mnist = BuildMnistTensor(pixels, width, height, bounds, _mainScratch);

                results.Add(new DigitCandidate
                {
                    bounds = bounds,
                    mnistPixels = mnist
                });

                if (results.Count >= maxCandidates)
                {
                    results.Sort((a, b) => a.bounds.x.CompareTo(b.bounds.x));
                    yield break;
                }

                if (++work >= workBudgetPerYield)
                {
                    work = 0;
                    yield return null;
                }
            }
        }

        results.Sort((a, b) => a.bounds.x.CompareTo(b.bounds.x));
    }

    private void ExtractDigitsFromPixelsInto(
        Color32[] pixels,
        int width,
        int height,
        List<DigitCandidate> results,
        Scratch scratch,
        float scaleX,
        float scaleY)
    {
        results.Clear();

        int pixelCount = width * height;
        scratch.EnsurePixelBuffers(pixelCount);
        scratch.EnsureCanvasBuffers(outputSize);

        ScaledBlobThresholds thresholds = GetScaledBlobThresholds(scaleX, scaleY);

        bool[] foreground = scratch.foreground;
        bool[] tempBinary = scratch.tempBinary;
        bool[] visited = scratch.visited;
        int[] queue = scratch.queue;

        for (int i = 0; i < pixelCount; i++)
        {
            foreground[i] = GetSourceValue(pixels[i]) >= whiteThreshold;
            visited[i] = false;
        }

        if (ShouldUseBinaryClose())
        {
            BinaryDilate4(foreground, tempBinary, width, height);
            BinaryErode4(tempBinary, foreground, width, height);
        }

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * width;

            for (int x = 0; x < width; x++)
            {
                int startIndex = rowStart + x;
                if (visited[startIndex] || !foreground[startIndex])
                    continue;

                int head = 0;
                int tail = 0;

                visited[startIndex] = true;
                queue[tail++] = startIndex;

                int minX = x;
                int maxX = x;
                int minY = y;
                int maxY = y;
                int blobCount = 0;

                while (head < tail)
                {
                    int idx = queue[head++];
                    blobCount++;

                    int px = idx % width;
                    int py = idx / width;

                    if (px < minX) minX = px;
                    if (px > maxX) maxX = px;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;

                    TryEnqueue(px - 1, py, width, height, foreground, visited, queue, ref tail);
                    TryEnqueue(px + 1, py, width, height, foreground, visited, queue, ref tail);
                    TryEnqueue(px, py - 1, width, height, foreground, visited, queue, ref tail);
                    TryEnqueue(px, py + 1, width, height, foreground, visited, queue, ref tail);
                }

                if (!PassesBlobFilter(blobCount, minX, maxX, minY, maxY, thresholds))
                    continue;

                RectInt bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                float[] mnist = BuildMnistTensor(pixels, width, height, bounds, scratch);

                results.Add(new DigitCandidate
                {
                    bounds = bounds,
                    mnistPixels = mnist
                });

                if (results.Count >= maxCandidates)
                {
                    results.Sort((a, b) => a.bounds.x.CompareTo(b.bounds.x));
                    return;
                }
            }
        }

        results.Sort((a, b) => a.bounds.x.CompareTo(b.bounds.x));
    }

    private ScaledBlobThresholds GetScaledBlobThresholds(float scaleX, float scaleY)
    {
        return new ScaledBlobThresholds
        {
            MinBlobPixels = Mathf.Max(3, Mathf.RoundToInt(minBlobPixels * scaleX * scaleY)),
            MinWidth = Mathf.Max(1, Mathf.RoundToInt(minWidth * scaleX)),
            MinHeight = Mathf.Max(1, Mathf.RoundToInt(minHeight * scaleY)),
            MinFillRatio = minFillRatio
        };
    }

    private bool PassesBlobFilter(
        int blobCount,
        int minX,
        int maxX,
        int minY,
        int maxY,
        ScaledBlobThresholds thresholds)
    {
        int blobWidth = maxX - minX + 1;
        int blobHeight = maxY - minY + 1;
        int area = blobWidth * blobHeight;

        if (blobCount < thresholds.MinBlobPixels)
            return false;

        if (blobWidth < thresholds.MinWidth || blobHeight < thresholds.MinHeight)
            return false;

        float fillRatio = blobCount / (float)area;
        if (fillRatio < thresholds.MinFillRatio)
            return false;

        return true;
    }

    private static void TryEnqueue(
        int x,
        int y,
        int width,
        int height,
        bool[] foreground,
        bool[] visited,
        int[] queue,
        ref int tail)
    {
        if ((uint)x >= (uint)width || (uint)y >= (uint)height)
            return;

        int idx = y * width + x;
        if (visited[idx] || !foreground[idx])
            return;

        visited[idx] = true;
        queue[tail++] = idx;
    }

    private bool ShouldUseBinaryClose()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return useBinaryClose;
#else
        return useBinaryClose && !useBinaryCloseOnWebGLOnly;
#endif
    }

    private void BinaryDilate4(bool[] src, bool[] dst, int width, int height)
    {
        Array.Clear(dst, 0, width * height);

        for (int y = 0; y < height; y++)
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                int idx = row + x;
                if (src[idx])
                {
                    dst[idx] = true;
                    if (x > 0) dst[idx - 1] = true;
                    if (x + 1 < width) dst[idx + 1] = true;
                    if (y > 0) dst[idx - width] = true;
                    if (y + 1 < height) dst[idx + width] = true;
                }
            }
        }
    }

    private void BinaryErode4(bool[] src, bool[] dst, int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                int idx = row + x;

                bool keep =
                    src[idx] &&
                    (x > 0 && src[idx - 1]) &&
                    (x + 1 < width && src[idx + 1]) &&
                    (y > 0 && src[idx - width]) &&
                    (y + 1 < height && src[idx + width]);

                dst[idx] = keep;
            }
        }
    }

    private float[] BuildMnistTensor(
        Color32[] pixels,
        int sourceWidth,
        int sourceHeight,
        RectInt bounds,
        Scratch scratch)
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
        int scaledLen = scaledW * scaledH;

        scratch.EnsureScaledBuffer(scaledLen);
        scratch.EnsureCanvasBuffers(outputSize);

        float[] scaled = scratch.scaled;
        float[] canvasA = scratch.canvasA;
        float[] canvasB = scratch.canvasB;

        Array.Clear(canvasA, 0, canvasA.Length);

        for (int y = 0; y < scaledH; y++)
        {
            int srcRow = y * scaledW;

            for (int x = 0; x < scaledW; x++)
            {
                float srcX = (x + 0.5f) / scale - 0.5f;
                float srcY = (y + 0.5f) / scale - 0.5f;

                scaled[srcRow + x] = SampleBilinear(
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
                canvasA[dstRow + x + offsetX] = scaled[srcRow + x];
        }

        RecenterByMass(canvasA, canvasB);

        float[] current = canvasA;
        float[] temp = canvasB;

        if (applyDilation)
        {
            Dilate(current, temp);
            (current, temp) = (temp, current);
        }

        if (rotate90Clockwise)
        {
            Rotate90CW(current, temp);
            (current, temp) = (temp, current);
        }

        if (rotate90CounterClockwise)
        {
            Rotate90CCW(current, temp);
            (current, temp) = (temp, current);
        }

        if (flipHorizontal)
        {
            FlipHorizontal(current, temp);
            (current, temp) = (temp, current);
        }

        if (flipVertical)
        {
            FlipVertical(current, temp);
            (current, temp) = (temp, current);
        }

        float[] output = new float[outputSize * outputSize];
        for (int i = 0; i < output.Length; i++)
            output[i] = current[i] - 0.5f;

        return output;
    }

    private float GetSourceValue(Color32 c)
    {
        float v = c.a * (1f / 255f);
        return invertInput ? 1f - v : v;
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
        int x0 = Mathf.Clamp((int)x, 0, cropW - 1);
        int x1 = Mathf.Clamp(x0 + 1, 0, cropW - 1);
        int y0 = Mathf.Clamp((int)y, 0, cropH - 1);
        int y1 = Mathf.Clamp(y0 + 1, 0, cropH - 1);

        float tx = x - x0;
        float ty = y - y0;

        float p00 = GetSourceValue(pixels[(cropY + y0) * sourceWidth + (cropX + x0)]);
        float p10 = GetSourceValue(pixels[(cropY + y0) * sourceWidth + (cropX + x1)]);
        float p01 = GetSourceValue(pixels[(cropY + y1) * sourceWidth + (cropX + x0)]);
        float p11 = GetSourceValue(pixels[(cropY + y1) * sourceWidth + (cropX + x1)]);

        float a = p00 + (p10 - p00) * tx;
        float b = p01 + (p11 - p01) * tx;
        return a + (b - a) * ty;
    }

    private void RecenterByMass(float[] src, float[] dst)
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
                float v = src[row + x];
                sum += v;
                cx += x * v;
                cy += y * v;
            }
        }

        if (sum <= 0.0001f)
            return;

        cx /= sum;
        cy /= sum;

        int shiftX = Mathf.RoundToInt(size * 0.5f - cx);
        int shiftY = Mathf.RoundToInt(size * 0.5f - cy);

        Array.Clear(dst, 0, dst.Length);

        for (int y = 0; y < size; y++)
        {
            int dstRow = y * size;
            int sy = y - shiftY;

            if ((uint)sy >= (uint)size)
                continue;

            int srcRow = sy * size;

            for (int x = 0; x < size; x++)
            {
                int sx = x - shiftX;
                if ((uint)sx < (uint)size)
                    dst[dstRow + x] = src[srcRow + sx];
            }
        }

        Array.Copy(dst, src, src.Length);
    }

    private void Dilate(float[] src, float[] dst)
    {
        int size = outputSize;

        for (int y = 0; y < size; y++)
        {
            int row = y * size;

            for (int x = 0; x < size; x++)
            {
                float maxVal = 0f;

                for (int oy = -1; oy <= 1; oy++)
                {
                    int ny = y + oy;
                    if ((uint)ny >= (uint)size) continue;

                    int nRow = ny * size;

                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int nx = x + ox;
                        if ((uint)nx >= (uint)size) continue;

                        float v = src[nRow + nx];
                        if (v > maxVal)
                            maxVal = v;
                    }
                }

                dst[row + x] = maxVal;
            }
        }
    }

    private void Rotate90CW(float[] src, float[] dst)
    {
        int size = outputSize;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                dst[y * size + x] = src[(size - 1 - x) * size + y];
    }

    private void Rotate90CCW(float[] src, float[] dst)
    {
        int size = outputSize;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                dst[y * size + x] = src[x * size + (size - 1 - y)];
    }

    private void FlipHorizontal(float[] src, float[] dst)
    {
        int size = outputSize;
        for (int y = 0; y < size; y++)
        {
            int row = y * size;
            for (int x = 0; x < size; x++)
                dst[row + x] = src[row + (size - 1 - x)];
        }
    }

    private void FlipVertical(float[] src, float[] dst)
    {
        int size = outputSize;
        for (int y = 0; y < size; y++)
        {
            int srcRow = (size - 1 - y) * size;
            int dstRow = y * size;
            for (int x = 0; x < size; x++)
                dst[dstRow + x] = src[srcRow + x];
        }
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