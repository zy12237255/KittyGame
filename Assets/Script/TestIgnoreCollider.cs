using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestIgnoreCollider : MonoBehaviour
{
    [SerializeField]
    private Collider2D collider1;
    [SerializeField]
    private Collider2D collider2;
    // Start is called before the first frame update
    void Start()
    {
       // Physics2D.IgnoreCollision(collider1, collider2, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
