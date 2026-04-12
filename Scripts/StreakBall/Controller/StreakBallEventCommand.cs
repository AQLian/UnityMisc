using strange.extensions.command.impl;
using System;
using UnityEngine;
using HappyMahjong.Common;

#if !compatible_758
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallEventCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }

        private static string s_streakBallNextUpdateTimer = "StreakBall_NextUpdateTimer";
        protected void StartNextUpdateTimer()
        {
            using var _ = UnityEngine.Pool.ListPool<long>.Get(out var nextQueryEndTime);
            var current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if(current < model.Info.StartTime)
            {
                nextQueryEndTime.Add(model.Info.StartTime);
            }
            else if(current < model.Info.EndTime)
            {
                nextQueryEndTime.Add(model.Info.EndTime);
            }
            if(current < model.ExchangeInfo.StartTime)
            {
                nextQueryEndTime.Add(model.ExchangeInfo.StartTime);
            }
            else if(current < model.ExchangeInfo.EndTime)
            {
                nextQueryEndTime.Add(model.ExchangeInfo.EndTime);
            }

            if(current < model.StreakInfo.RefreshTime)
            {
                nextQueryEndTime.Add(model.StreakInfo.RefreshTime);
            }

            CancelNextUpdateTimer();
            if (nextQueryEndTime.Count > 0)
            {
                nextQueryEndTime.Sort();
                var end = nextQueryEndTime[0];
                if (end > current)
                {
                    var gap = end - current;
                    var startup = Time.realtimeSinceStartup;
                    VPTimer.In(3, () =>
                    {
                        if (!model.ViewCreated && Time.realtimeSinceStartup - startup > gap)
                        {
                            CancelNextUpdateTimer();
                            service.GetDetailReq();
                        }
                    }, -1, methodName: s_streakBallNextUpdateTimer);
                }
            }
        }

        protected void CancelNextUpdateTimer()
        {
            VPTimer.CancelAll(s_streakBallNextUpdateTimer);
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
