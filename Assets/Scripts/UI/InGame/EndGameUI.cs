using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class EndGameUI : MonoBehaviour
{
    [SerializeField] private EquationsCategoriesDatabase equationsDatabase;
    [SerializeField] private PlayerManager playerManager;

    [Header("UI References")]
    [SerializeField] private Transform barsParent;
    [SerializeField] private EquationProgressBarUI barPrefab;
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Buttons")]
    //[SerializeField] private Button settingsButton;
    [SerializeField] private Button hubButton;
    [SerializeField] private Button retryButton;

    [Header("Audio")]
    [SerializeField] private AudioClip countingClip;

    private readonly List<EquationProgressBarUI> spawnedBars = new();

    private void Awake()
    {
        gameObject.SetActive(false);
        playerManager.OnEndGameValuesCalculated += Show;

        //settingsButton.onClick.AddListener(OpenSettings);
        hubButton.onClick.AddListener(BackToHub);
        retryButton.onClick.AddListener(Retry);
    }

    private void OnDestroy()
    {
        playerManager.OnEndGameValuesCalculated -= Show;
    }

    public void Show(
        Dictionary<EquationType, int> previousScores,
        Dictionary<EquationType, int> newScores,
        int moneyGained)
    {
        // Setup canvas group for fade-in
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        transform.localScale = Vector3.one * 0.85f;
        gameObject.SetActive(true);

        ClearBars();

        // Animate entry
        Sequence entrySeq = DOTween.Sequence().SetUpdate(true);
        entrySeq.Join(cg.DOFade(1f, 0.5f));
        entrySeq.Join(transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack));

        entrySeq.OnComplete(() =>
        {
            // Spawn bars with a small stagger
            Sequence barsSeq = DOTween.Sequence().SetUpdate(true);

            foreach (var pair in newScores)
            {
                EquationType type = pair.Key;
                int newScore = pair.Value;
                int previousScore = previousScores.TryGetValue(type, out int oldValue) ? oldValue : 0;

                EquationCategoryData categoryData = equationsDatabase.GetEquationCategoryData(type);
                if (categoryData == null) continue;

                EquationProgressBarUI bar = Instantiate(barPrefab, barsParent);
                List<int> thresholdLevels = categoryData.AchievmentThresholds;
                bar.Setup(type, previousScore, newScore, thresholdLevels);
                spawnedBars.Add(bar);

                // Fade & pop in animation for each bar
                bar.transform.localScale = Vector3.one * 0.8f;
                CanvasGroup barCg = bar.GetComponent<CanvasGroup>();
                if (barCg == null) barCg = bar.gameObject.AddComponent<CanvasGroup>();
                barCg.alpha = 0;

                barsSeq.Append(barCg.DOFade(1f, 0.3f));
                barsSeq.Join(bar.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }

            barsSeq.OnComplete(() =>
            {
                // Animate money counting
                AnimateMoney(moneyGained);
            });
        });
    }

    private void AnimateMoney(int moneyGained)
    {
        moneyText.text = "0";

        // Temporary AudioSource for counting
        AudioSource tempSource = moneyText.gameObject.AddComponent<AudioSource>();
        tempSource.clip = countingClip;
        tempSource.loop = true;
        tempSource.playOnAwake = false;
        tempSource.Play();

        DOVirtual.Float(0, moneyGained, 1.2f, value =>
        {
            moneyText.text = Mathf.RoundToInt(value).ToString();
        })
        .SetEase(Ease.OutCubic)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            tempSource.Stop();
            Destroy(tempSource);

            // Punch scale for final effect
            moneyText.transform
                .DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1)
                .SetUpdate(true);
        });
    }

    private void ClearBars()
    {
        foreach (var bar in spawnedBars)
        {
            if (bar != null) Destroy(bar.gameObject);
        }
        spawnedBars.Clear();
    }

    public void BackToHub()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HubScene");
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Game");
    }

    public void OpenSettings()
    {
        ServiceLocator.Instance.UIManager.SettingsUI.Show();
    }

    public void Hide()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        Sequence hideSeq = DOTween.Sequence().SetUpdate(true);
        hideSeq.Join(cg.DOFade(0f, 0.3f));
        hideSeq.Join(transform.DOScale(0.85f, 0.3f).SetEase(Ease.InBack));
        hideSeq.OnComplete(() => gameObject.SetActive(false));
    }
}