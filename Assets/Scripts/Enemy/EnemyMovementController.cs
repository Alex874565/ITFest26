using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovementController : MonoBehaviour
{
    private EnemyStats _stats;
    private Rigidbody2D _rb;

    private Vector2 _playerPos;

    private bool _reachedPlayer;

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
        if (_playerPos.x < transform.position.x)
            transform.localScale = new Vector3(-1, 1, 1);
        else
            transform.localScale = new Vector3(1, 1, 1);
    }

    public void ResetValues()
    {
        _rb.linearVelocity = Vector2.zero;
    }

    public void ReachPlayer()
    {
        _reachedPlayer = true;
        _rb.linearVelocity = Vector2.zero;
    }
}