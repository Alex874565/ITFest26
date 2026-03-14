using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;
using TMPro;

public class NumberRecognitionController : MonoBehaviour
{
    [FormerlySerializedAs("draw")]
    [SerializeField] private Drawer drawer;
    [SerializeField] private TextMeshProUGUI recognizedNumberText;
    [SerializeField] private RectTransform recognizedNumberRect;
    [SerializeField] private CanvasGroup recognizedNumberCanvasGroup;

    [SerializeField] private DigitSegmenter segmenter;
    [SerializeField] private MnistRecognizer recognizer;

    [Header("Debug")]
    [SerializeField] private RawImage debugDigit;
    [SerializeField] private bool createDebugTexture = false;

    [Header("Performance")]
    [SerializeField] private bool skipIfBusy = true;
    [SerializeField] private bool onlyShowFirstDebugDigit = true;
    [SerializeField] private int predictOneEveryNFrames = 1;
    
    public event Action<int> OnNumberRecognized;
    public event Action OnNumberNotRecognized;

    private CancellationTokenSource _segmentationCts;
    private Coroutine _recognizeRoutine;
    private bool _isBusy;
    private Texture2D _debugTexture;
    
    private PlayerController _playerController;

    private void Awake()
    {
        drawer.OnTimeToEvaluatePassed += RecognizeNumber;
    }

    private void OnDestroy()
    {
        if (drawer != null)
            drawer.OnTimeToEvaluatePassed -= RecognizeNumber;

        if (_recognizeRoutine != null)
            StopCoroutine(_recognizeRoutine);

        _segmentationCts?.Cancel();
        _segmentationCts?.Dispose();

        if (_debugTexture != null)
            Destroy(_debugTexture);
    }

    public void RecognizeNumber()
    {
        if (_isBusy && skipIfBusy)
            return;

        if (_recognizeRoutine != null)
            StopCoroutine(_recognizeRoutine);

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
            _isBusy = false;
            _recognizeRoutine = null;
            yield break;
        }

        // Main thread only
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();

        // Run segmentation off the main thread
        Task<List<DigitSegmenter.DigitCandidate>> segmentationTask =
            Task.Run(() => segmenter.ExtractDigitsFromPixels(pixels, width, height), token);

        while (!segmentationTask.IsCompleted)
            yield return null;

        if (token.IsCancellationRequested)
        {
            _isBusy = false;
            _recognizeRoutine = null;
            yield break;
        }

        if (segmentationTask.IsFaulted)
        {
            Debug.LogException(segmentationTask.Exception);
            _isBusy = false;
            _recognizeRoutine = null;
            yield break;
        }

        List<DigitSegmenter.DigitCandidate> candidates = segmentationTask.Result;

        for (int i = 0; i < candidates.Count; i++)
            Debug.Log($"Candidate {i}: bounds={candidates[i].bounds}");

        if (candidates.Count == 0)
        {
            Debug.Log("No candidates found");
            OnNumberNotRecognized?.Invoke();
            _isBusy = false;
            _recognizeRoutine = null;
            yield break;
        }

        StringBuilder sb = new StringBuilder(candidates.Count);

        // IMPORTANT: Keep prediction on main thread
        for (int i = 0; i < candidates.Count; i++)
        {
            int digit = recognizer.PredictDigit(candidates[i].mnistPixels);
            sb.Append(digit);

            if (createDebugTexture && debugDigit != null)
            {
                if (!onlyShowFirstDebugDigit || i == 0)
                {
                    if (_debugTexture != null)
                        Destroy(_debugTexture);

                    _debugTexture = segmenter.CreateDebugTexture(candidates[i].mnistPixels);
                    debugDigit.texture = _debugTexture;
                }
            }

            int framesToWait = Mathf.Max(1, predictOneEveryNFrames);
            for (int f = 0; f < framesToWait; f++)
                yield return null;
        }

        string number = sb.ToString();
        recognizedNumberText.text = number;
        drawer.PlayRecognizedNumberPop(recognizedNumberRect, recognizedNumberCanvasGroup);
        OnNumberRecognized?.Invoke(int.Parse(number));

        _isBusy = false;
        _recognizeRoutine = null;
    }
}