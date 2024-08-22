using OlympicSpeedrunners;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class GhostRunner : MonoBehaviour
{
    [SerializeField] private Transform recordTarget;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField, Range(1, 10)] private int captureEveryNFrames = 2;
    private ReplaySystem system;

    private void Awake() => system = new ReplaySystem(this);

    void Update()
    {
        if (Input.GetKey(KeyCode.R))
        {
            system.StartRun(recordTarget, captureEveryNFrames);
        }
        if (Input.GetKey(KeyCode.T))
        {
            system.FinishRun();
        }
        if (Input.GetKey(KeyCode.P))
        {
            system.PlayRecording(RecordingType.Best, Instantiate(ghostPrefab));
        }
    }
    
}
