using OlympicSpeedrunners;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
    private bool countRun;

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
            // Check if the current run's duration is at least 25 seconds
            if (system._currentRun.Duration >= 25)
            {
                countRun = true;
                fastestRun = system.FinishRun(countRun);

                // If this run is the fastest, update the best time
                if (fastestRun)
                {
                    system.GetRun(RecordingType.Best, out var best);
                    logic.updateTimes(best.Duration, true);
                }
                else
                {
                    // Otherwise, just update the last run's time
                    system.GetRun(RecordingType.Last, out var last);
                    logic.updateTimes(last.Duration, false);
                }
            }
            else
            {
                countRun = false;
                logic.currTime = 00.00f;
                system.FinishRun(countRun); // Ensure the system knows the run didn't count
            }

            // Stop the replay system
            system.StopReplay();
        }
    }

}
