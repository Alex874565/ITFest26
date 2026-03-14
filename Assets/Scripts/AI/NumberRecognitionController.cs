using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class NumberRecognitionController : MonoBehaviour
{
    [FormerlySerializedAs("draw")]
    [SerializeField] private Drawer drawer;
    [SerializeField] private TextMeshProUGUI recognizedNumberText;
    [SerializeField] private RectTransform recognizedNumberRect;
    [SerializeField] private CanvasGroup recognizedNumberCanvasGroup;

    [SerializeField] private DigitSegmenter segmenter;
    [SerializeField] private MnistRecognizer recognizer;

    [Header("Debug - Digit Preview")]
    [SerializeField] private RawImage debugDigit;
    [SerializeField] private bool createDebugTexture = false;
    [SerializeField] private bool onlyShowFirstDebugDigit = true;

    [Header("Performance")]
    [SerializeField] private bool skipIfBusy = true;
    [SerializeField] private int predictOneEveryNFrames = 0;
    [SerializeField] private int maxWholeNumberCandidates = 16;

    [Header("Whole Number Assist")]
    [SerializeField] private bool enableAssist = true;
    [SerializeField, Range(1, 4)] private int assistTopKPerDigit = 2;

    public event Action<int> OnNumberRecognized;
    public event Action OnNumberNotRecognized;

    private CancellationTokenSource _segmentationCts;
    private Coroutine _recognizeRoutine;
    private bool _isBusy;
    private Texture2D _debugTexture;

    private PlayerController _playerController;

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

    private void OnEnable()
    {
        if (drawer != null)
            drawer.OnTimeToEvaluatePassed += RecognizeNumber;
    }

    private void OnDisable()
    {
        if (drawer != null)
            drawer.OnTimeToEvaluatePassed -= RecognizeNumber;

        if (_recognizeRoutine != null)
        {
            StopCoroutine(_recognizeRoutine);
            _recognizeRoutine = null;
        }

        _segmentationCts?.Cancel();
        _segmentationCts?.Dispose();
        _segmentationCts = null;

        if (_debugTexture != null)
        {
            Destroy(_debugTexture);
            _debugTexture = null;
        }

        _isBusy = false;
    }

    public void RecognizeNumber()
    {
        if (_isBusy && skipIfBusy)
            return;

        if (_recognizeRoutine != null)
        {
            StopCoroutine(_recognizeRoutine);
            _recognizeRoutine = null;
        }

        _recognizeRoutine = StartCoroutine(RecognizeNumberCoroutine());
    }

    private IEnumerator RecognizeNumberCoroutine()
    {
        _isBusy = true;

        _segmentationCts?.Cancel();
        _segmentationCts?.Dispose();
        _segmentationCts = new CancellationTokenSource();
        CancellationToken token = _segmentationCts.Token;

        Texture2D texture = drawer != null ? drawer.DrawTexture : null;
        if (texture == null)
        {
            FinishRecognition();
            yield break;
        }

        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();

        Task<List<DigitSegmenter.DigitCandidate>> segmentationTask =
            Task.Run(() => segmenter.ExtractDigitsFromPixels(pixels, width, height), token);

        while (!segmentationTask.IsCompleted)
            yield return null;

        if (token.IsCancellationRequested)
        {
            FinishRecognition();
            yield break;
        }

        if (segmentationTask.IsFaulted)
        {
            Debug.LogException(segmentationTask.Exception);
            FinishRecognition();
            yield break;
        }

        List<DigitSegmenter.DigitCandidate> candidates = segmentationTask.Result;

        if (candidates == null || candidates.Count == 0)
        {
            OnNumberNotRecognized?.Invoke();
            FinishRecognition();
            yield break;
        }

        int topK = Mathf.Max(1, assistTopKPerDigit);
        List<List<MnistRecognizer.DigitPrediction>> digitPredictions =
            new List<List<MnistRecognizer.DigitPrediction>>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            List<MnistRecognizer.DigitPrediction> predictions =
                recognizer.PredictTopDigits(candidates[i].mnistPixels, topK);

            if (predictions == null || predictions.Count == 0)
            {
                OnNumberNotRecognized?.Invoke();
                FinishRecognition();
                yield break;
            }

            digitPredictions.Add(predictions);

            if (createDebugTexture && debugDigit != null && (!onlyShowFirstDebugDigit || i == 0))
            {
                if (_debugTexture != null)
                {
                    Destroy(_debugTexture);
                    _debugTexture = null;
                }

                _debugTexture = segmenter.CreateDebugTexture(candidates[i].mnistPixels);
                debugDigit.texture = _debugTexture;
            }

            int framesToWait = Mathf.Max(0, predictOneEveryNFrames);
            for (int f = 0; f < framesToWait; f++)
                yield return null;
        }

        WholeNumberCandidate chosen = ChooseWholeNumberWithAssist(digitPredictions);

        if (chosen == null)
        {
            OnNumberNotRecognized?.Invoke();
            FinishRecognition();
            yield break;
        }

        recognizedNumberText.text = chosen.NumberString;

        if (drawer != null)
            drawer.PlayRecognizedNumberPop(recognizedNumberRect, recognizedNumberCanvasGroup);

        OnNumberRecognized?.Invoke(chosen.NumberValue);

        FinishRecognition();
    }

    private WholeNumberCandidate ChooseWholeNumberWithAssist(List<List<MnistRecognizer.DigitPrediction>> digitPredictions)
    {
        List<WholeNumberCandidate> candidates = BuildWholeNumberCandidates(digitPredictions);

        if (candidates == null || candidates.Count == 0)
            return null;

        WholeNumberCandidate topPath = candidates[0];

        if (!enableAssist || _playerController == null || candidates.Count == 1)
            return topPath;

        List<WholeNumberCandidate> viableCandidates = new List<WholeNumberCandidate>(candidates.Count);
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].HelpsEnemy)
                viableCandidates.Add(candidates[i]);
        }

        if (viableCandidates.Count == 0)
            return topPath;

        viableCandidates.Sort((a, b) =>
        {
            int confidenceCompare = b.CombinedConfidence.CompareTo(a.CombinedConfidence);
            if (confidenceCompare != 0)
                return confidenceCompare;

            int distanceCompare = a.ClosestDistance.CompareTo(b.ClosestDistance);
            if (distanceCompare != 0)
                return distanceCompare;

            return string.CompareOrdinal(a.NumberString, b.NumberString);
        });

        return viableCandidates[0];
    }

    private List<WholeNumberCandidate> BuildWholeNumberCandidates(List<List<MnistRecognizer.DigitPrediction>> digitPredictions)
    {
        List<WholeNumberCandidate> results = new List<WholeNumberCandidate>(maxWholeNumberCandidates);
        StringBuilder currentDigits = new StringBuilder(digitPredictions.Count);

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

        if (_playerController != null)
        {
            for (int i = 0; i < results.Count; i++)
            {
                WholeNumberCandidate c = results[i];
                c.HelpsEnemy = _playerController.TryGetBestAssistDistance(c.NumberValue, out c.ClosestDistance);
            }
        }
        else
        {
            for (int i = 0; i < results.Count; i++)
            {
                WholeNumberCandidate c = results[i];
                c.HelpsEnemy = false;
                c.ClosestDistance = float.PositiveInfinity;
            }
        }

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
            MnistRecognizer.DigitPrediction prediction = predictions[i];

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
    }
}