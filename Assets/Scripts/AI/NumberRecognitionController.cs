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

    [Header("Debug - Whole Number Assist")]
    [SerializeField] private bool debugAssistLogs = true;
    [SerializeField] private bool debugLogDigitPredictions = true;
    [SerializeField] private bool debugLogWholeCandidates = true;
    [SerializeField] private TextMeshProUGUI debugAssistText;

    [Header("Performance")]
    [SerializeField] private bool skipIfBusy = true;
    [SerializeField] private int predictOneEveryNFrames = 0;
    [SerializeField] private int maxWholeNumberCandidates = 16;

    [Header("Whole Number Assist")]
    [SerializeField] private bool enableAssist = true;
    [SerializeField] [Range(1, 4)] private int assistTopKPerDigit = 2;
    [SerializeField] [Range(0f, 1f)] private float assistConfidenceGap = 0.08f;
    [SerializeField] private bool onlyAssistWhenTopWholeNumberDoesNotHelp = true;
    [SerializeField] private bool preferCloserEnemy = true;
    [SerializeField] [Min(0f)] private float minimumDistanceAdvantage = 0.25f;
    [SerializeField] private bool useConfidenceAsTieBreaker = true;

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
        public bool IsTopPath;
    }

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
            _playerController = FindObjectOfType<PlayerController>();
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
        ClearDebugAssistText();

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
            AppendDebug("No digit candidates found.");
            OnNumberNotRecognized?.Invoke();
            FinishRecognition();
            yield break;
        }

        List<List<MnistRecognizer.DigitPrediction>> digitPredictions = new List<List<MnistRecognizer.DigitPrediction>>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            List<MnistRecognizer.DigitPrediction> predictions =
                recognizer.PredictTopDigits(candidates[i].mnistPixels, Mathf.Max(1, assistTopKPerDigit));

            if (predictions == null || predictions.Count == 0)
            {
                AppendDebug($"Digit {i}: no predictions.");
                OnNumberNotRecognized?.Invoke();
                FinishRecognition();
                yield break;
            }

            digitPredictions.Add(predictions);

            if (debugLogDigitPredictions)
                AppendDigitPredictionDebug(i, predictions);

            if (createDebugTexture && debugDigit != null)
            {
                if (!onlyShowFirstDebugDigit || i == 0)
                {
                    if (_debugTexture != null)
                    {
                        Destroy(_debugTexture);
                        _debugTexture = null;
                    }

                    _debugTexture = segmenter.CreateDebugTexture(candidates[i].mnistPixels);
                    debugDigit.texture = _debugTexture;
                }
            }

            int framesToWait = Mathf.Max(0, predictOneEveryNFrames);
            for (int f = 0; f < framesToWait; f++)
                yield return null;
        }

        WholeNumberCandidate chosen = ChooseWholeNumberWithAssist(digitPredictions);

        if (chosen == null)
        {
            AppendDebug("No whole-number candidate selected.");
            OnNumberNotRecognized?.Invoke();
            FinishRecognition();
            yield break;
        }

        recognizedNumberText.text = chosen.NumberString;

        if (drawer != null)
            drawer.PlayRecognizedNumberPop(recognizedNumberRect, recognizedNumberCanvasGroup);

        AppendDebug($"Final recognized number: {chosen.NumberString}");

        OnNumberRecognized?.Invoke(chosen.NumberValue);

        FinishRecognition();
    }

    private WholeNumberCandidate ChooseWholeNumberWithAssist(List<List<MnistRecognizer.DigitPrediction>> digitPredictions)
    {
        List<WholeNumberCandidate> candidates = BuildWholeNumberCandidates(digitPredictions);

        if (candidates.Count == 0)
            return null;

        WholeNumberCandidate topPath = candidates[0];

        if (!enableAssist || _playerController == null || candidates.Count == 1)
        {
            AppendDebug($"Assist disabled or unnecessary -> chose {topPath.NumberString}");
            return topPath;
        }

        WholeNumberCandidate best = topPath;

        bool canAssist = false;
        if (candidates.Count > 1)
        {
            float gap = topPath.CombinedConfidence - candidates[1].CombinedConfidence;
            canAssist = gap <= assistConfidenceGap;

            if (!canAssist)
            {
                AppendDebug($"Top whole-number confidence clear ({topPath.NumberString}, gap {gap:0.000} > {assistConfidenceGap:0.000})");
                return topPath;
            }
        }

        if (onlyAssistWhenTopWholeNumberDoesNotHelp && topPath.HelpsEnemy)
        {
            AppendDebug($"Top whole number already helps -> kept {topPath.NumberString}");
            return topPath;
        }

        for (int i = 1; i < candidates.Count; i++)
        {
            WholeNumberCandidate candidate = candidates[i];

            if (candidate.HelpsEnemy && !best.HelpsEnemy)
            {
                best = candidate;
                continue;
            }

            if (!candidate.HelpsEnemy)
                continue;

            if (preferCloserEnemy && best.HelpsEnemy)
            {
                bool meaningfullyCloser = candidate.ClosestDistance < (best.ClosestDistance - minimumDistanceAdvantage);
                if (meaningfullyCloser)
                {
                    best = candidate;
                    continue;
                }
            }

            if (best.HelpsEnemy &&
                useConfidenceAsTieBreaker &&
                Mathf.Abs(candidate.ClosestDistance - best.ClosestDistance) <= minimumDistanceAdvantage &&
                candidate.CombinedConfidence > best.CombinedConfidence)
            {
                best = candidate;
            }
        }

        if (debugLogWholeCandidates)
            AppendWholeCandidateDebug(candidates, best);

        if (best.NumberValue != topPath.NumberValue)
            AppendDebug($"ASSIST OVERRIDE {topPath.NumberString} -> {best.NumberString}");

        return best;
    }

    private List<WholeNumberCandidate> BuildWholeNumberCandidates(List<List<MnistRecognizer.DigitPrediction>> digitPredictions)
    {
        List<WholeNumberCandidate> results = new List<WholeNumberCandidate>();
        StringBuilder currentDigits = new StringBuilder();

        BuildWholeNumberCandidatesRecursive(
            digitPredictions,
            0,
            currentDigits,
            1f,
            true,
            results
        );

        results.Sort((a, b) => b.CombinedConfidence.CompareTo(a.CombinedConfidence));

        if (results.Count > maxWholeNumberCandidates)
            results.RemoveRange(maxWholeNumberCandidates, results.Count - maxWholeNumberCandidates);

        for (int i = 0; i < results.Count; i++)
        {
            WholeNumberCandidate c = results[i];
            c.IsTopPath = (i == 0);

            if (_playerController != null)
                c.HelpsEnemy = _playerController.TryGetBestAssistDistance(c.NumberValue, out c.ClosestDistance);
            else
            {
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
        float currentConfidence,
        bool isTopPath,
        List<WholeNumberCandidate> results)
    {
        if (results.Count > maxWholeNumberCandidates * 4)
            return;

        if (digitIndex >= digitPredictions.Count)
        {
            string numberString = currentDigits.ToString();

            if (string.IsNullOrEmpty(numberString))
                return;

            if (!int.TryParse(numberString, out int numberValue))
                return;

            results.Add(new WholeNumberCandidate
            {
                NumberString = numberString,
                NumberValue = numberValue,
                CombinedConfidence = currentConfidence,
                IsTopPath = isTopPath
            });

            return;
        }

        List<MnistRecognizer.DigitPrediction> predictions = digitPredictions[digitIndex];
        if (predictions == null || predictions.Count == 0)
            return;

        int originalLength = currentDigits.Length;

        for (int i = 0; i < predictions.Count; i++)
        {
            MnistRecognizer.DigitPrediction p = predictions[i];
            currentDigits.Append(p.Digit);

            BuildWholeNumberCandidatesRecursive(
                digitPredictions,
                digitIndex + 1,
                currentDigits,
                currentConfidence * p.Confidence,
                isTopPath && i == 0,
                results
            );

            currentDigits.Length = originalLength;
        }
    }

    private void AppendDigitPredictionDebug(int digitIndex, List<MnistRecognizer.DigitPrediction> predictions)
    {
        StringBuilder line = new StringBuilder();
        line.Append($"Digit {digitIndex} predictions: ");

        for (int i = 0; i < predictions.Count; i++)
        {
            MnistRecognizer.DigitPrediction p = predictions[i];
            line.Append($"[{p.Digit} conf={p.Confidence:0.000}]");

            if (i < predictions.Count - 1)
                line.Append(" ");
        }

        AppendDebug(line.ToString());
    }

    private void AppendWholeCandidateDebug(List<WholeNumberCandidate> candidates, WholeNumberCandidate chosen)
    {
        AppendDebug("Whole-number candidates:");

        for (int i = 0; i < candidates.Count; i++)
        {
            WholeNumberCandidate c = candidates[i];

            string marker = c == chosen ? " <-- chosen" : "";
            string helpText = c.HelpsEnemy
                ? $"helps dist={c.ClosestDistance:0.00}"
                : "no-help";

            AppendDebug($"  {c.NumberString} conf={c.CombinedConfidence:0.000000} {helpText}{marker}");
        }
    }

    private void AppendDebug(string message)
    {
        if (debugAssistLogs)
            Debug.Log("[Recognizer] " + message);

        if (debugAssistText != null)
        {
            if (string.IsNullOrEmpty(debugAssistText.text))
                debugAssistText.text = message;
            else
                debugAssistText.text += "\n" + message;
        }
    }

    private void ClearDebugAssistText()
    {
        if (debugAssistText != null)
            debugAssistText.text = string.Empty;
    }

    private void FinishRecognition()
    {
        _isBusy = false;
        _recognizeRoutine = null;
    }
}