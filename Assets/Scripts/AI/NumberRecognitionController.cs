using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class NumberRecognitionController : MonoBehaviour
{
    [FormerlySerializedAs("draw")]
    [SerializeField] private Drawer drawer;
    [SerializeField] private TextMeshProUGUI recognizedNumberText;
    [SerializeField] private RectTransform recognizedNumberRect;
    [SerializeField] private CanvasGroup recognizedNumberCanvasGroup;

    [SerializeField] private DigitSegmenter segmenter;
    [SerializeField] private MnistRecognizer recognizer;
    
    [Header("Performance")]
    [SerializeField] private bool skipIfBusy = true;
    [SerializeField] private int maxWholeNumberCandidates = 16;
    [SerializeField] private int segmentationWorkBudgetPerFrame = 20000;
    [SerializeField] private float minRecognitionInterval = 0.05f;

    [Header("Analysis Downsampling")]
    [SerializeField] private bool useDownsampledAnalysis = true;
    [SerializeField] private int maxAnalysisSizeNative = 512;
    [SerializeField] private int maxAnalysisSizeWebGL = 256;

    [Header("Whole Number Assist")]
    [SerializeField] private bool enableAssist = true;
    [SerializeField, Range(1, 4)] private int assistTopKPerDigit = 2;

    private bool _pendingRecognition;
    
    public event Action<int> OnNumberRecognized;
    public event Action OnNumberNotRecognized;

    private CancellationTokenSource _segmentationCts;
    private Coroutine _recognizeRoutine;
    private bool _isBusy;
    private float _lastRecognizeTime = -999f;

    private PlayerController _playerController;

    private readonly List<DigitSegmenter.DigitCandidate> _segmentedDigits = new();
    private readonly List<List<MnistRecognizer.DigitPrediction>> _digitPredictions = new();

    private Color32[] _analysisPixelsBuffer;

    private class WholeNumberCandidate
    {
        public string NumberString;
        public int NumberValue;
        public float CombinedConfidence;
        public bool HelpsEnemy;
        public float ClosestDistance;
    }

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
            _playerController = FindFirstObjectByType<PlayerController>();
    }

    private IEnumerator Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    segmentationWorkBudgetPerFrame = 3000;
    minRecognitionInterval = 0.2f;
#else
        segmentationWorkBudgetPerFrame = 25000;
        minRecognitionInterval = 0.05f;
#endif

        yield return null;
        yield return null;

        if (recognizer != null)
            recognizer.Warmup();
    }

    private void OnDisable()
    {

        if (_recognizeRoutine != null)
        {
            StopCoroutine(_recognizeRoutine);
            _recognizeRoutine = null;
        }

#if !(UNITY_WEBGL && !UNITY_EDITOR)
        _segmentationCts?.Cancel();
        _segmentationCts?.Dispose();
        _segmentationCts = null;
#endif

        _isBusy = false;
    }

    public bool RecognizeNumber()
    {
        if (_isBusy && skipIfBusy)
        {
            _pendingRecognition = true;
            return false;
        }

        if (Time.unscaledTime - _lastRecognizeTime < minRecognitionInterval)
        {
            _pendingRecognition = true;
            return false;
        }

        _pendingRecognition = false;
        _lastRecognizeTime = Time.unscaledTime;

        if (_recognizeRoutine != null)
        {
            StopCoroutine(_recognizeRoutine);
            _recognizeRoutine = null;
        }

        _recognizeRoutine = StartCoroutine(RecognizeNumberCoroutine());
        return true;
    }

    private IEnumerator RecognizeNumberCoroutine()
    {
        _isBusy = true;
        _segmentedDigits.Clear();
        _digitPredictions.Clear();

        Texture2D texture = drawer != null ? drawer.DrawTexture : null;
        if (texture == null)
        {
            if (drawer != null)
                drawer.Clear();
            FinishRecognition();
            yield break;
        }

        Color32[] sourcePixels = texture.GetPixels32();
        int sourceWidth = texture.width;
        int sourceHeight = texture.height;

        PrepareAnalysisPixels(
            sourcePixels,
            sourceWidth,
            sourceHeight,
            out Color32[] analysisPixels,
            out int analysisWidth,
            out int analysisHeight
        );

#if UNITY_WEBGL && !UNITY_EDITOR
        yield return segmenter.ExtractDigitsFromPixelsCoroutine(
            analysisPixels,
            analysisWidth,
            analysisHeight,
            _segmentedDigits,
            segmentationWorkBudgetPerFrame
        );
#else
        _segmentationCts?.Cancel();
        _segmentationCts?.Dispose();
        _segmentationCts = new CancellationTokenSource();
        CancellationToken token = _segmentationCts.Token;

        Task<List<DigitSegmenter.DigitCandidate>> segmentationTask =
            Task.Run(() => segmenter.ExtractDigitsFromPixelsThreadSafe(analysisPixels, analysisWidth, analysisHeight), token);

        while (!segmentationTask.IsCompleted)
            yield return null;

        if (token.IsCancellationRequested)
        {
            if (drawer != null)
                drawer.Clear();
            FinishRecognition();
            yield break;
        }

        if (segmentationTask.IsFaulted)
        {
            Debug.LogException(segmentationTask.Exception);
            if (drawer != null)
                drawer.Clear();
            FinishRecognition();
            yield break;
        }

        _segmentedDigits.AddRange(segmentationTask.Result);
#endif

        if (_segmentedDigits.Count == 0)
        {
            OnNumberNotRecognized?.Invoke();
            if (drawer != null)
                drawer.Clear();
            FinishRecognition();
            yield break;
        }

        int topK = Mathf.Max(1, assistTopKPerDigit);

        recognizer.PredictTopDigitsBatchNonAlloc(_segmentedDigits, _digitPredictions, topK);

        if (_digitPredictions.Count != _segmentedDigits.Count)
        {
            OnNumberNotRecognized?.Invoke();
            if (drawer != null)
                drawer.Clear();
            FinishRecognition();
            yield break;
        }

        for (int i = 0; i < _digitPredictions.Count; i++)
        {
            if (_digitPredictions[i] == null || _digitPredictions[i].Count == 0)
            {
                OnNumberNotRecognized?.Invoke();
                if (drawer != null)
                    drawer.Clear();
                FinishRecognition();
                yield break;
            }
        }

        WholeNumberCandidate chosen = ChooseWholeNumberWithAssist(_digitPredictions);

        if (chosen == null)
        {
            OnNumberNotRecognized?.Invoke();
            if (drawer != null)
                drawer.Clear();
            FinishRecognition();
            yield break;
        }

        recognizedNumberText.text = chosen.NumberString;
        bool isCorrect = chosen.HelpsEnemy;

        if (drawer != null)
        {
            drawer.PlayRecognizedNumberPop(
                recognizedNumberRect,
                isCorrect,
                recognizedNumberCanvasGroup,
                tmpText: recognizedNumberText
            );
        }

        OnNumberRecognized?.Invoke(chosen.NumberValue);
        if (drawer != null)
            drawer.Clear();
        FinishRecognition();
    }

    private void PrepareAnalysisPixels(
        Color32[] sourcePixels,
        int sourceWidth,
        int sourceHeight,
        out Color32[] analysisPixels,
        out int analysisWidth,
        out int analysisHeight)
    {
        int maxSize =
#if UNITY_WEBGL && !UNITY_EDITOR
            maxAnalysisSizeWebGL;
#else
            maxAnalysisSizeNative;
#endif

        if (!useDownsampledAnalysis || maxSize <= 0)
        {
            analysisPixels = sourcePixels;
            analysisWidth = sourceWidth;
            analysisHeight = sourceHeight;
            return;
        }

        int longestSide = Mathf.Max(sourceWidth, sourceHeight);
        if (longestSide <= maxSize)
        {
            analysisPixels = sourcePixels;
            analysisWidth = sourceWidth;
            analysisHeight = sourceHeight;
            return;
        }

        float scale = maxSize / (float)longestSide;
        analysisWidth = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
        analysisHeight = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));

        int dstLen = analysisWidth * analysisHeight;
        if (_analysisPixelsBuffer == null || _analysisPixelsBuffer.Length != dstLen)
            _analysisPixelsBuffer = new Color32[dstLen];

        for (int y = 0; y < analysisHeight; y++)
        {
            int srcY = Mathf.Min(sourceHeight - 1, Mathf.FloorToInt((y + 0.5f) / scale));
            int dstRow = y * analysisWidth;
            int srcRow = srcY * sourceWidth;

            for (int x = 0; x < analysisWidth; x++)
            {
                int srcX = Mathf.Min(sourceWidth - 1, Mathf.FloorToInt((x + 0.5f) / scale));
                _analysisPixelsBuffer[dstRow + x] = sourcePixels[srcRow + srcX];
            }
        }

        analysisPixels = _analysisPixelsBuffer;
    }

    private WholeNumberCandidate ChooseWholeNumberWithAssist(List<List<MnistRecognizer.DigitPrediction>> digitPredictions)
    {
        List<WholeNumberCandidate> candidates = BuildWholeNumberCandidates(digitPredictions);

        if (candidates == null || candidates.Count == 0)
            return null;

        WholeNumberCandidate topPath = candidates[0];

        if (!enableAssist || _playerController == null || candidates.Count == 1)
            return topPath;

        List<WholeNumberCandidate> usefulCandidates = new(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            WholeNumberCandidate candidate = candidates[i];

            if (_playerController.TryGetBestAssistDistance(candidate.NumberValue, out float closestDistance))
            {
                candidate.HelpsEnemy = true;
                candidate.ClosestDistance = closestDistance;
                usefulCandidates.Add(candidate);
            }
            else
            {
                candidate.HelpsEnemy = false;
                candidate.ClosestDistance = float.PositiveInfinity;
            }
        }

        if (usefulCandidates.Count == 0)
            return topPath;

        usefulCandidates.Sort((a, b) =>
        {
            int distanceCompare = a.ClosestDistance.CompareTo(b.ClosestDistance);
            if (distanceCompare != 0) return distanceCompare;

            int confidenceCompare = b.CombinedConfidence.CompareTo(a.CombinedConfidence);
            if (confidenceCompare != 0) return confidenceCompare;

            return string.CompareOrdinal(a.NumberString, b.NumberString);
        });

        return usefulCandidates[0];
    }

    private List<WholeNumberCandidate> BuildWholeNumberCandidates(List<List<MnistRecognizer.DigitPrediction>> digitPredictions)
    {
        List<WholeNumberCandidate> results = new(maxWholeNumberCandidates);
        StringBuilder currentDigits = new(digitPredictions.Count);

        BuildWholeNumberCandidatesRecursive(
            digitPredictions,
            0,
            currentDigits,
            0L,
            1f,
            results
        );

        results.Sort((a, b) => b.CombinedConfidence.CompareTo(a.CombinedConfidence));

        if (results.Count > maxWholeNumberCandidates)
            results.RemoveRange(maxWholeNumberCandidates, results.Count - maxWholeNumberCandidates);

        return results;
    }

    private void BuildWholeNumberCandidatesRecursive(
        List<List<MnistRecognizer.DigitPrediction>> digitPredictions,
        int digitIndex,
        StringBuilder currentDigits,
        long currentValue,
        float currentConfidence,
        List<WholeNumberCandidate> results)
    {
        if (results.Count >= maxWholeNumberCandidates * 4)
            return;

        if (digitIndex >= digitPredictions.Count)
        {
            if (currentDigits.Length == 0)
                return;

            if (currentValue < int.MinValue || currentValue > int.MaxValue)
                return;

            results.Add(new WholeNumberCandidate
            {
                NumberString = currentDigits.ToString(),
                NumberValue = (int)currentValue,
                CombinedConfidence = currentConfidence
            });

            return;
        }

        List<MnistRecognizer.DigitPrediction> predictions = digitPredictions[digitIndex];
        if (predictions == null || predictions.Count == 0)
            return;

        int originalLength = currentDigits.Length;

        for (int i = 0; i < predictions.Count; i++)
        {
            var prediction = predictions[i];
            currentDigits.Append(prediction.Digit);

            long nextValue = currentValue * 10L + prediction.Digit;
            if (nextValue >= 0L && nextValue <= int.MaxValue)
            {
                BuildWholeNumberCandidatesRecursive(
                    digitPredictions,
                    digitIndex + 1,
                    currentDigits,
                    nextValue,
                    currentConfidence * prediction.Confidence,
                    results
                );
            }

            currentDigits.Length = originalLength;
        }
    }

    private void FinishRecognition()
    {
        _isBusy = false;
        _recognizeRoutine = null;

        if (_pendingRecognition)
        {
            _pendingRecognition = false;
            RecognizeNumber();
        }
    }
}