using OlympicSpeedrunners;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OlympicSpeedrunners
{
    public class Logic : MonoBehaviour
    {
        public Text BestTime;
        public Text LastTime;
        public Text CurrTime;
        private float currTime = 00.00f;
        private float lastTime = 00.00f;
        private float bestTime = 00.00f;
        // Start is called before the first frame update
        void Start()
        {
            updateTimes(00.00f, false);
        }

        // Update is called once per frame
        void Update()
        {
            currTime += Time.deltaTime;
            CurrTime.text = "Current: " + currTime.ToString("#.000");

        }

        public void updateTimes(float time, bool newBest)
        {
            currTime = 00.00f;
            lastTime = time;
            if(newBest) bestTime = time;
            BestTime.text = "Best: " + bestTime.ToString("#.000");
            LastTime.text = "Last: " + lastTime.ToString("#.000");
        }

    }
}

