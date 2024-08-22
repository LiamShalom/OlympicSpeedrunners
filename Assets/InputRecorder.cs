using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using System;
using UnityEditor;

public class InputRecorder : MonoBehaviour
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

    public List<PlayerInputData> inputRecord = new List<PlayerInputData>();
    private bool isRecording = false;
    private float startTime;
    public float recordInterval = 0.1f; // Time interval in seconds between recordings
    private float nextRecordTime = 0f;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isRecording)
        {
            // Check if it's time to record the input
            if (Time.time >= nextRecordTime)
            {
                RecordInput();
                nextRecordTime = Time.time + recordInterval;
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            StopRecording();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveRecording("/inputRecord.txt");
        }
    }

    void StartRecording()
    {
        inputRecord.Clear();
        startTime = Time.time;
        isRecording = true;
        Debug.Log("Recording started.");
    }

    void StopRecording()
    {
        isRecording = false;
        Debug.Log("Recording stopped. Total inputs recorded: " + inputRecord.Count);
    }

    void RecordInput()
    {
        PlayerInputData data = new PlayerInputData
        {
            time = Time.time - startTime,
            xInput = Input.GetAxisRaw("Horizontal"),
            jump = Input.GetKey(KeyCode.UpArrow),
            slide = Input.GetKey(KeyCode.DownArrow),
            grapple = Input.GetKey(KeyCode.Z)
        };

        inputRecord.Add(data);
    }

    public void SaveRecording(string fileName)
    {
        string filepath = Application.persistentDataPath + fileName;
        StreamWriter sw = new StreamWriter(filepath);
        try
    {
            foreach (var data in inputRecord)
            {
                sw.WriteLine($"{data.time},{data.xInput},{data.jump},{data.slide},{data.grapple}");
            }

            // Check if the file was created successfully
            if (File.Exists(filepath))
            {
                Debug.Log("Recording saved successfully to " + filepath);
            }
            else
            {
                Debug.LogError("File not found after saving attempt. Something went wrong.");
            }
        }
    catch (Exception e)
    {
            Debug.LogError("Error saving file: " + e.Message);
        }
    }
}
