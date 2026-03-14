using UnityEngine;
using DG.Tweening;

public class TapToContinueAnimation : MonoBehaviour
{
    private Vector3 startPos;

    [SerializeField] private float moveAmount = 8f;
    [SerializeField] private float duration = 1f;

    private void Start()
    {
        startPos = transform.localPosition;

        transform.DOLocalMoveY(startPos.y + moveAmount, duration)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);
    }
}