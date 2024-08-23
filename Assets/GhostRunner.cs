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
    private Logic logic;
    private bool fastestRun;

    private void Awake() => system = new ReplaySystem(this);

    void Start()
    {
        logic = FindObjectOfType<Logic>();
    }
    public void FinishLineCrossed(bool runStarting)
    {
        if (runStarting)
        {
            system.StartRun(recordTarget, captureEveryNFrames);
            system.PlayRecording(RecordingType.Best, Instantiate(ghostPrefab));
        }
        else
        {
            fastestRun = system.FinishRun();
            if (fastestRun)
            {
                system.GetRun(RecordingType.Best, out var best);
                logic.updateTimes(best.Duration, true);
            }
            else
            {
                system.GetRun(RecordingType.Last, out var last);
                logic.updateTimes(last.Duration, false);
            }
            system.StopReplay();
        }
    }

}
