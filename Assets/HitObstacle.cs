using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class HitObstacle : MonoBehaviour
{
    private Vector3 position;
    public float timer;
    private float respawnTime;
    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(timer < respawnTime)
        {
            timer += Time.deltaTime;
        }
        else
        {

        }
    }
}
