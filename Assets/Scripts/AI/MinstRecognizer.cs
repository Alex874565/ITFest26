using System;
using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

public class MnistRecognizer : MonoBehaviour
{
    [SerializeField] private ModelAsset modelAsset;

    private Worker worker;

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

    private void Awake()
    {
        Model model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.GPUCompute);
    }

    public int PredictDigit(float[] pixels28x28)
    {
        List<DigitPrediction> predictions = PredictTopDigits(pixels28x28, 1);
        return predictions.Count > 0 ? predictions[0].Digit : -1;
    }

    public List<DigitPrediction> PredictTopDigits(float[] pixels28x28, int topK = 3)
    {
        if (pixels28x28 == null || pixels28x28.Length != PixelsPerDigit)
        {
            Debug.LogError($"Expected {PixelsPerDigit} pixels, got {pixels28x28?.Length ?? 0}.");
            return new List<DigitPrediction>();
        }

        using var input = new Tensor<float>(new TensorShape(1, 1, InputHeight, InputWidth), pixels28x28);

        worker.Schedule(input);

        var output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.LogError("Model output is not Tensor<float>.");
            return new List<DigitPrediction>();
        }

        using var cpuOutput = output.ReadbackAndClone();

        float[] logits = new float[ClassCount];
        for (int i = 0; i < ClassCount; i++)
            logits[i] = cpuOutput[i];

        float[] probabilities = Softmax(logits);

        List<DigitPrediction> all = new List<DigitPrediction>(ClassCount);
        for (int i = 0; i < ClassCount; i++)
            all.Add(new DigitPrediction(i, logits[i], probabilities[i]));

        all.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));

        if (topK < all.Count)
            all.RemoveRange(topK, all.Count - topK);

        return all;
    }

    private float[] Softmax(float[] logits)
    {
        float max = float.MinValue;
        for (int i = 0; i < logits.Length; i++)
        {
            if (logits[i] > max)
                max = logits[i];
        }

        float sum = 0f;
        float[] exps = new float[logits.Length];

        for (int i = 0; i < logits.Length; i++)
        {
            exps[i] = Mathf.Exp(logits[i] - max);
            sum += exps[i];
        }

        if (sum <= 0f)
            return new float[logits.Length];

        for (int i = 0; i < exps.Length; i++)
            exps[i] /= sum;

        return exps;
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}