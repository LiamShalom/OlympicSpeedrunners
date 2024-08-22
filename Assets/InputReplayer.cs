using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InputRecorder;
using UnityEngine.Profiling;
using System.IO;
using System;
using UnityEngine.XR;

public class InputReplayer : MonoBehaviour
{
    [System.Serializable]
    public struct PlayerInputData
    {
        public float time;
        public float xInput;
        public bool jump;
        public bool slide;
        public bool grapple;
    }

    public InputRecorder recorder;
    public PlayerController playerController;
    private bool isReplaying = false;
    private int currentInputIndex = 0;
    private float replayStartTime;
    public List<PlayerInputData> inputRecord = new List<PlayerInputData>();

    // Start is called before the first frame update
    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !isReplaying)
        {
            LoadRecording("/inputRecord.txt");
            StartReplay();
        }
    }

    void StartReplay()
    {
        StopAllCoroutines();
        currentInputIndex = 0;
        replayStartTime = Time.time;
        isReplaying = true;
        Debug.Log("Starting Replay");
        StartCoroutine(ReplayInputs());
    }

    public void LoadRecording(string fileName)
    {
        string filePath = Application.persistentDataPath + fileName;
        Debug.Log("Loading file from: " + filePath);

        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return;
        }

        try
        {
            StreamReader sr = new StreamReader(filePath);
            string line;
            inputRecord.Clear(); // Clear any existing data
            while ((line = sr.ReadLine()) != null)
            {
                Debug.Log("Read raw line: " + line);
                string[] parts = line.Split(',');
                if (parts.Length == 5)
                {
                    float time = float.Parse(parts[0]);
                    float xInput = float.Parse(parts[1]);
                    bool jump = bool.Parse(parts[2]);
                    bool slide = bool.Parse(parts[3]);
                    bool grapple = bool.Parse(parts[4]);

                    // Create a new InputData object and add it to the inputRecord list
                    PlayerInputData data = new PlayerInputData
                    {
                        time = time,
                        xInput = xInput,
                        jump = jump,
                        slide = slide,
                        grapple = grapple,
                    };

                    inputRecord.Add(data);
                }

            }
            Debug.Log("Recording loaded successfully from " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error loading file: " + e.Message);
        }
    }
    IEnumerator ReplayInputs()
    {
        while (currentInputIndex < inputRecord.Count)
        {
            PlayerInputData data = inputRecord[currentInputIndex];
            float currentTime = Time.time - replayStartTime;

            // Wait until the correct time to replay this input
            while (currentTime < data.time)
            {
                Debug.Log(currentTime);
                yield return null; // Wait until it's time to apply the next input
                currentTime = Time.time - replayStartTime;
            }

            // Apply the input once the correct time is reached
            ApplyInput(data);

            // Move to the next input
            currentInputIndex++;
        }

        isReplaying = false;
        currentInputIndex = 0;
        Debug.Log("Replay finished");
    }

    void ApplyInput(PlayerInputData data)
    {
        if(data.xInput != 0)
        {
            playerController.ApplyMovement(new Vector2(data.xInput, 0));
        }

        if (data.jump)
        {
            playerController.jump();
        }

        if (data.slide)
        {
            playerController.slide();
        }

        if (data.grapple)
        {
            playerController.grapple();
        }
    }
}
