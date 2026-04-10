using UnityEngine;

/// <summary>
/// Optional harness to ignore a pair of 2D colliders (left disabled by default).
/// </summary>
public class TestIgnoreCollider : MonoBehaviour
{
    [SerializeField]
    private Collider2D collider1;
    [SerializeField]
    private Collider2D collider2;

    private void Start()
    {
        // Physics2D.IgnoreCollision(collider1, collider2, true);
    }
}
