using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SuperPupSystems.Helper
{
    public class CountDownText : MonoBehaviour
    {
        public string textHeader = "Timeleft : ";
        public Timer timer;
        public float time { get { return timer.timeLeft; } }
        public float startTime = 30.0f;
        public TMP_Text text;
        void Start()
        {
            if (timer == null)
            {
                Debug.LogError("GameObject " + gameObject.name + " field timer is equal to null");
            }
            StartCountDownTimer();
        }

        // Update is called once per frame
        void Update()
        {
            text.text = textHeader + Mathf.Ceil(time);
        }

        public void StartCountDownTimer()
        {
            timer.StartTimer(startTime, false);
        }

        public void AddTime(float time)
        {
            timer.AddTime(time);
        }
    }
}