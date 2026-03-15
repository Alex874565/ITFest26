using System;
using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
public class EnemyVisualController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI equationText;
    [SerializeField] private GameObject equationObject;

    public event Action OnAttackAnimationFinished;
    
    private EquationData _equationData;

    private SpriteRenderer _sprite;
    private Animator _animator;
    
    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    public void Flip(bool right)
    {
        if (right)
        {
            _sprite.flipX = false;
        }
        else
        {
            _sprite.flipX = true;
        }
    }

    public void ReachPlayer(int preparationTime)
    {
        StartCoroutine(ReachPlayerCoroutine(preparationTime));
    }

    private IEnumerator ReachPlayerCoroutine(int preparationTime)
    {
        _animator.SetTrigger("Prepare");
        yield return new WaitForSeconds(preparationTime);
        _animator.SetTrigger("Attack");
    }

    public void AttackAnimationFinishedEvent()
    {
        OnAttackAnimationFinished?.Invoke();
    }
    
    public void ResetValues(EquationData data)
    {
        _equationData = data;
        equationText.text = _equationData.Equation;
    }

    public void PlayDisappear()
    {
        equationObject.SetActive(false);
        _animator.SetTrigger("Defeat");
    }
}