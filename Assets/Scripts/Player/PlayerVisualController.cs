using UnityEngine;
using DG.Tweening;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private GameObject heart;
    [SerializeField] private int heartCount = 3;

    public void PlayHappyAnimation()
    {
        for (int i = 0; i < heartCount; i++)
        {
            SpawnHeart();
        }
    }

    private void SpawnHeart()
    {
        GameObject h = Instantiate(heart, heart.transform.position, Quaternion.identity, heart.transform.parent);

        Transform t = h.transform;
        SpriteRenderer sr = h.GetComponent<SpriteRenderer>();

        Vector3 originalScale = t.localScale;

        // Reset
        t.localScale = Vector3.zero;

        if (sr != null)
            sr.color = new Color(1, 1, 1, 1);

        // Random float direction
        Vector2 randomDir = new Vector2(
            Random.Range(-0.6f, 0.6f),
            Random.Range(0.6f, 1.2f)
        ).normalized;

        Vector3 targetPos = t.position + (Vector3)randomDir * Random.Range(0.8f, 1.2f);

        Sequence seq = DOTween.Sequence();
        float delay = Random.Range(0f, 0.15f);
        seq.PrependInterval(delay);

        // POP using YOUR scale
        seq.Append(t.DOScale(originalScale * 1.3f, 0.25f).SetEase(Ease.OutBack));

        // Bounce back to original
        seq.Append(t.DOScale(originalScale, 0.15f));

        // Float
        seq.Join(t.DOMove(targetPos, 1f).SetEase(Ease.OutQuad));

        // Slight rotation
        seq.Join(t.DORotate(new Vector3(0, 0, Random.Range(-30f, 30f)), 1f));

        // Fade
        if (sr != null)
            seq.Join(sr.DOFade(0, 1f));

        seq.OnComplete(() => Destroy(h));
    }
}