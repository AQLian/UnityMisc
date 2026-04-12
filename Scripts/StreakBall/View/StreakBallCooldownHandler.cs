using System;
using System.Collections;
using System.Collections.Generic;

using HappyMahjong.Common;
using HappyMahjong.ShopAndBag;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public class TalentCooldownHandler : MonoBehaviour
    {
        private TextMeshProUGUI m_text;
        private int m_remain;
        public Action OnReach { get; set; }

        public bool IsStarted { get; private set; }

        private static string PreTimerKey = "TalentCooldownHandler";
        private static long nextKey = 0;

        private string timeKey;

        private void Awake()
        {
            m_text = GetComponent<TextMeshProUGUI>();
            m_text.text = "";
        }

        public void StopTimer()
        {
            VPTimer.CancelAll(timeKey);
        }

        public void Started(long endTimestamp)
        {
            StopTimer();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            m_remain = (int)(endTimestamp - now);
            if (m_remain < 0)
            {
                m_remain = 0;
            }
            IsStarted = true;
            StartTick();
        }

        private void StartTick()
        {
            UpdateUI();
            timeKey = $"{PreTimerKey}{nextKey}";
            nextKey++;
            VPTimer.In(1, UpdateCallback, -1, 1, methodName:timeKey);
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        public void UpdateCallback()
        {
            m_remain -= 1;
            UpdateUI();
            if(m_remain < 0)
            {
                StopTimer();
                OnReach();
            }
        }

        void UpdateUI()
        {
            var format = StreakBallUtil.FormatCountdown(m_remain);
            m_text.text = format;
        }

        private void OnDestroy()
        {
            StopTimer();
        }
    }
}

