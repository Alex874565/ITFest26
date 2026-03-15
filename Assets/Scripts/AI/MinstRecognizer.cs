using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

public class MnistRecognizer : MonoBehaviour
{
    [SerializeField] private ModelAsset modelAsset;
    [SerializeField] private bool preferGpu = true;

    private Worker _worker;
    private BackendType _backendType;

    [Serializable]
    public struct DigitPrediction
    {
        public int Digit;
        public float Logit;
        public float Confidence;

        public DigitPrediction(int digit, float logit, float confidence)
        {
            Digit = digit;
            Logit = logit;
            Confidence = confidence;
        }
    }

    private const int InputWidth = 28;
    private const int InputHeight = 28;
    private const int ClassCount = 10;
    private const int PixelsPerDigit = InputWidth * InputHeight;

    // Reused scratch buffers to reduce GC
    private readonly float[] _logits = new float[ClassCount];
    private readonly float[] _probabilities = new float[ClassCount];

    private void Awake()
    {
        Model model = ModelLoader.Load(modelAsset);
        _backendType = ChooseBackend();
        _worker = new Worker(model, _backendType);
    }

    public void Warmup()
    {
        float[] dummy = new float[28 * 28];
        PredictDigit(dummy);
    }
    
    private BackendType ChooseBackend()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL-safe default
        return BackendType.CPU;
#else
        if (!preferGpu)
            return BackendType.CPU;

        if (SystemInfo.supportsComputeShaders)
            return BackendType.GPUCompute;

        return BackendType.CPU;
#endif
    }

    public int PredictDigit(float[] pixels28x28)
    {
        if (!TryValidateInput(pixels28x28))
            return -1;

        using var input = new Tensor<float>(
            new TensorShape(1, 1, InputHeight, InputWidth),
            pixels28x28
        );

        _worker.Schedule(input);

        var output = _worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.LogError("Model output is not Tensor<float>.");
            return -1;
        }

        using var cpuOutput = output.ReadbackAndClone();

        int bestDigit = 0;
        float bestLogit = cpuOutput[0];

        for (int i = 1; i < ClassCount; i++)
        {
            float logit = cpuOutput[i];
            if (logit > bestLogit)
            {
                bestLogit = logit;
                bestDigit = i;
            }
        }

        return bestDigit;
    }

    public void PredictTopDigitsBatchNonAlloc(
    List<DigitSegmenter.DigitCandidate> candidates,
    List<List<DigitPrediction>> results,
    int topK = 3)
{
    results.Clear();

    if (candidates == null || candidates.Count == 0)
        return;

    topK = Mathf.Clamp(topK, 1, ClassCount);

    int batch = candidates.Count;
    float[] batchedInput = new float[batch * PixelsPerDigit];

    for (int i = 0; i < batch; i++)
    {
        float[] src = candidates[i].mnistPixels;
        if (src == null || src.Length != PixelsPerDigit)
        {
            Debug.LogError($"Candidate {i} does not have {PixelsPerDigit} pixels.");
            results.Clear();
            return;
        }

        Array.Copy(src, 0, batchedInput, i * PixelsPerDigit, PixelsPerDigit);
    }

    using var input = new Tensor<float>(
        new TensorShape(batch, 1, InputHeight, InputWidth),
        batchedInput
    );

    _worker.Schedule(input);

    var output = _worker.PeekOutput() as Tensor<float>;
    if (output == null)
    {
        Debug.LogError("Model output is not Tensor<float>.");
        return;
    }

    using var cpuOutput = output.ReadbackAndClone();

    for (int b = 0; b < batch; b++)
    {
        List<DigitPrediction> perDigit = new List<DigitPrediction>(topK);

        int baseIndex = b * ClassCount;
        float max = float.MinValue;

        for (int i = 0; i < ClassCount; i++)
        {
            float logit = cpuOutput[baseIndex + i];
            _logits[i] = logit;
            if (logit > max)
                max = logit;
        }

        float sum = 0f;
        for (int i = 0; i < ClassCount; i++)
        {
            float p = Mathf.Exp(_logits[i] - max);
            _probabilities[i] = p;
            sum += p;
        }

        if (sum > 0f)
        {
            float invSum = 1f / sum;
            for (int i = 0; i < ClassCount; i++)
                _probabilities[i] *= invSum;

            for (int rank = 0; rank < topK; rank++)
            {
                int bestDigit = -1;
                float bestConfidence = float.NegativeInfinity;

                for (int i = 0; i < ClassCount; i++)
                {
                    float confidence = _probabilities[i];
                    bool alreadyPicked = false;

                    for (int j = 0; j < perDigit.Count; j++)
                    {
                        if (perDigit[j].Digit == i)
                        {
                            alreadyPicked = true;
                            break;
                        }
                    }

                    if (!alreadyPicked && confidence > bestConfidence)
                    {
                        bestConfidence = confidence;
                        bestDigit = i;
                    }
                }

                if (bestDigit < 0)
                    break;

                perDigit.Add(new DigitPrediction(
                    bestDigit,
                    _logits[bestDigit],
                    _probabilities[bestDigit]
                ));
            }
        }

        results.Add(perDigit);
    }
}

    private bool ContainsDigit(List<DigitPrediction> results, int digit)
    {
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Digit == digit)
                return true;
        }

        return false;
    }

    private bool TryValidateInput(float[] pixels28x28)
    {
        if (pixels28x28 == null || pixels28x28.Length != PixelsPerDigit)
        {
            Debug.LogError($"Expected {PixelsPerDigit} pixels, got {pixels28x28?.Length ?? 0}.");
            return false;
        }

        return true;
    }

    public BackendType GetBackendType() => _backendType;

    private void OnDestroy()
    {
        _worker?.Dispose();
        _worker = null;
    }
}