using UnityEngine;

public class PlayerCollisionController : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        IReachPlayer reacher = other.GetComponentInParent<IReachPlayer>();
        if (reacher != null)
        {
            reacher.ReachPlayer();
        }
    }
}