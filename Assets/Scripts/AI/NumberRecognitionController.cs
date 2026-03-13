using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class NumberRecognitionController : MonoBehaviour
{
    [FormerlySerializedAs("draw")] [SerializeField] private Drawer drawer;
    [SerializeField] private DigitSegmenter segmenter;
    [SerializeField] private MnistRecognizer recognizer;
    [Header("Debug")]
    [SerializeField] private RawImage debugDigit;

    private void Awake()
    {
        drawer.OnTimeToEvaluatePassed += RecognizeNumber;
    }
    
    public void RecognizeNumber()
    {
        var candidates = segmenter.ExtractDigits(drawer.DrawTexture);

        for (int i = 0; i < candidates.Count; i++)
        {
            Debug.Log($"Candidate {i}: bounds={candidates[i].bounds}");
        }

        if (candidates.Count == 0)
        {
            Debug.Log($"No candidates found");
            return;
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < candidates.Count; i++)
        {
            int digit = recognizer.PredictDigit(candidates[i].mnistPixels);
            sb.Append(digit);
        }

        Debug.Log(sb.ToString());
    }
}