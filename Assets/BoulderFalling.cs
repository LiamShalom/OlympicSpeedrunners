using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderFalling : MonoBehaviour
{
    public Rigidbody2D rb;
    public Rigidbody2D rb2;
    public Rigidbody2D rb3;
    void Start()
    {
        rb.gravityScale = 0;
        rb2.gravityScale = 0;
        rb3.gravityScale = 0;
    }
    void OnTriggerEnter(Collider coll)
    {
        if (coll.CompareTag("Player"))
        {
            rb.gravityScale = 1;
            rb2.gravityScale = 1;
            rb3.gravityScale = 1;
        }
    }
}
