using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowingMultiplePlayers: MonoBehaviour
{
    public List<Transform> playerList;
    public Vector3 offset;
    private Vector3 velocity;
    public float smoothTime = 0.5f;
    public float minZoom = 40f ;
    public float maxZoom = 10f;
    public float zoomLimiter = 50f;
    private Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>(); 
    }

    void LateUpdate()
    {

        if (playerList.Count == 0)
            return;

        Move();
        Zoom(); 
    }

    Vector3 GetCenterPoint()
    {
        if (playerList.Count == 1)
            return playerList[0].position;

        var bounds = new Bounds(playerList[0].position, Vector3.zero);
        for(int i = 0; i < playerList.Count; i++)
        {
            bounds.Encapsulate(playerList[i].position); 
        }

        return bounds.center; 
    }

    void Move()
    {
        Vector3 centerPointOfPlayers = GetCenterPoint();
        Vector3 newPosition = centerPointOfPlayers + offset;
        transform.position = Vector3.SmoothDamp(transform.position, newPosition, ref velocity, smoothTime);
    }

    //Adjusts the camera zoom according to distance between players 
    void Zoom()
    {
        float newZoom = Mathf.Lerp(maxZoom, minZoom, GetGreatestDistance() / zoomLimiter);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newZoom, Time.deltaTime);
    }


    //returns the distance between the furthest apart players 
    float GetGreatestDistance()
    { 
        var bounds = new Bounds(playerList[0].position, Vector3.zero);
        for (int i = 0; i < playerList.Count; i++)
        {
            bounds.Encapsulate(playerList[i].position); 
        }

        return bounds.size.x; 
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}
