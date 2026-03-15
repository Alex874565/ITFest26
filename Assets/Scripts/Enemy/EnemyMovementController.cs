using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovementController : MonoBehaviour
{
    public event Action<bool> OnFlip;

    private EnemyStats _stats;
    private Rigidbody2D _rb;

    private Vector2 _playerPos;

    private bool _reachedPlayer;
    private bool _stopped;
    private Coroutine _reachCoroutine;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Instantiate(EnemyStats stats, Vector2 playerPos)
    {
        _stats = stats;
        _playerPos = playerPos;
    }

    private void FixedUpdate()
    {
        if (_stopped) return;

        if (!_reachedPlayer)
        {
            FlipToPlayer();
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        Vector2 direction = (_playerPos - (Vector2)transform.position).normalized;
        _rb.MovePosition(_rb.position + direction * _stats.MovementSpeed * Time.fixedDeltaTime);
    }

    private void FlipToPlayer()
    {
        OnFlip?.Invoke(transform.position.x < _playerPos.x);
    }

    public void ResetValues()
    {
        if (_reachCoroutine != null)
        {
            StopCoroutine(_reachCoroutine);
            _reachCoroutine = null;
        }

        _reachedPlayer = false;
        _stopped = false;
        _rb.linearVelocity = Vector2.zero;
    }

    public void ReachPlayer(int preparationTime)
    {
        if (_reachCoroutine != null)
            StopCoroutine(_reachCoroutine);

        _reachCoroutine = StartCoroutine(ReachedPlayerCoroutine(preparationTime));
    }

    public IEnumerator ReachedPlayerCoroutine(float preparationTime)
    {
        _rb.linearVelocity = Vector2.zero;
        _reachedPlayer = true;

        yield return new WaitForSeconds(preparationTime);

        if (_stopped) yield break;

        Vector2 start = transform.position;
        Vector2 target = _playerPos;

        float duration = 0.6f;
        float height = 1f;
        float time = 0f;

        while (time < duration)
        {
            if (_stopped) yield break;

            time += Time.deltaTime;
            float t = time / duration;

            Vector2 pos = Vector2.Lerp(start, target, t);

            float arc = height * 4 * (t - t * t);
            pos.y += arc;

            _rb.MovePosition(pos);

            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
        _reachCoroutine = null;
    }

    public void Stop()
    {
        _stopped = true;
        _rb.linearVelocity = Vector2.zero;

        if (_reachCoroutine != null)
        {
            StopCoroutine(_reachCoroutine);
            _reachCoroutine = null;
        }
    }
}