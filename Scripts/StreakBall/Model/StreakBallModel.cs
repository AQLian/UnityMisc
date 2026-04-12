using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using Configuration;

using HappyMahjong.Common;
using HappyMahjong.ShopAndBag;
using HappyMahjong.update;

using JetBrains.Annotations;

using MJWinStreakBallActivity;

using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallModel : IStreakBallModel, IModelSerializer
    {
        // 活动是否有效
        public bool IsValid { get; set; }
        // 界面是否已经创建，用来判断是否有缓存
        public bool ViewCreated { get; set; }
        public bool IsResourceReady{get;set;}=true;
        public bool IsAddPopupQueue { get; set; }

        private Dictionary<int, CoinBuyInfo> m_coinBuyInfoMap = new();

        #region StreakBall
        public GetDetailRes Info { get; private set; } = new();
        public ExchangeInfo ExchangeInfo => Info.ExchangeInfo;
       
        public WinStreakInfo StreakInfo => Info.WinStreakInfo;

        internal GameObject iconTopRight;

        public GameObject EntryIcon { get; internal set; }
        #endregion

        public string Serialize()
        {
            return String.Empty;
        }

        public void AddBoughtCoin(int itemID, int boughtNum)
        {
            if (m_coinBuyInfoMap.ContainsKey(itemID))
            {
                CoinBuyInfo buyInfo = m_coinBuyInfoMap[itemID];
                buyInfo.boughtNum += boughtNum;
                m_coinBuyInfoMap[itemID] = buyInfo;
            }
        }

        /// <summary>
        /// 查询响应
        /// </summary>
        /// <param name="rsp"></param>
        public void QueryInfoRsp(GetDetailRes rsp)
        {
            Info = rsp;
            long serverTime = ServerTime.GetInstance().GetServerTime();
            long startTime = Info.StartTime;
            long endTime = Info.EndTime;
            bool isTimeValid = startTime <= serverTime && endTime >= serverTime;
            IsValid = Info.ActId > 0 && isTimeValid;
            UpdateRedDot();
        }

        public void UpdateStreakInfo(ClaimWinStreakBallRes res)
        {
            Info.ActId = res.ActId;
            Info.WinStreakInfo = res.WinStreakInfo;
            UpdateRedDot();
        }

        public void UpdateExchangeInfo(ExchangeRes res)
        {
            Info.ActId=res.ActId;
            Info.ExchangeInfo = res.ExchangeInfo;
            UpdateRedDot();
        }


        // test 
        public static T BinaryClone<T>(T obj)
        {
            var f = new BinaryFormatter();
            using var ms = new System.IO.MemoryStream();
            f.Serialize(ms, obj);
            ms.Position = 0;
            return (T)f.Deserialize(ms);
        }


        /// <summary>
        /// 后台可以控制开启时间点
        /// </summary>
        /// <returns></returns>
        public bool HasReachStartTime()
        {
            if (Info == null)
            {
                return false;
            }
            if (Info.StartTime > 0)
            {
                var nowTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (Info.StartTime <= nowTime && Info.EndTime >= nowTime)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查活动状态
        /// </summary>
        /// <returns></returns>
        internal bool CheckTempOpen()
        {
            if (!HasReachStartTime())
            {
                Toast.GetInstance().showUGUI(LangKeys.tempNotOpen);
                return false;
            }

            return true;
        }

        public bool IsOpen()
        {
            return DynamicConfig.GetInstance().GetBool(UIDef.ConfigKey, "IsOpen") && CheckVersion();
        }

        public bool CheckVersion()
        {
            return CheckMinVersion() && CheckIgnoreVersions();
        }

        private bool CheckMinVersion()
        {
            var minVersion = DynamicConfig.GetInstance().GetString(UIDef.ConfigKey, HappyMahjong.Common.Util.IsPCPlatform() ? "MinVersionPC" : "MinVersion");

            if (!String.IsNullOrEmpty(minVersion) &&
                ConfigRequest.GetVersion() < TSDK4CSharp.QGDeviceInfo.versionToUint(minVersion))
            {
                Log.Info("EnterRoomCardShelves CheckMinVersion: false", ModuleType.StreakBall);

                return false;
            }

            // 没配或者当前版本大于等于minVersion 返回true
            return true;
        }

        private bool CheckIgnoreVersions()
        {
            var ignoreVersions = HappyBridge.Util.DynamicConfig.GetInstance().GetList<string>(UIDef.ConfigKey, "IgnoreVersions");
            foreach (var igVersion in ignoreVersions)
            {
                if (!String.IsNullOrEmpty(igVersion) && igVersion.Equals(ConfigRequest.GetVersionString()))
                {
                    HappyBridge.Util.Log.Info("CheckIgnoreVersions: false", ModuleType.StreakBall);

                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// 红点刷新
        /// </summary>
        internal void UpdateRedDot()
        {
            if(EntryIcon != null)
            {
                var redDot = EntryIcon.transform.Find("Red_Dot");
                var parent = EntryIcon.transform.parent;
                Transform parentRedDot = null;
                if (parent)
                {
                    parentRedDot = FindParent(parent, "Red_Dot");
                }
                if (redDot != null)
                {
                    var showRedDot = AnyExchangeable() || IsStreakInfoInit();
                    redDot.gameObject.SetActive(showRedDot);
                    if (parentRedDot)
                    {
                        parentRedDot.gameObject.SetActive(showRedDot);
                    }
                }
                var tempLock = EntryIcon.transform.Find("TempLock");
                if (tempLock != null)
                {
                    tempLock.gameObject.SetActive(!HasReachStartTime());
                }
            }
        }

        public bool IsStreakInfoInit()
        {
            return StreakInfo != null && StreakInfo.Status == (int) WinStreakStatus.WinStreakStatusNone;
        }

        public bool AnyExchangeable()
        {
            return ReachExchangeTime() && ExchangeInfo.ExchangeItems.Exists(item => item.Status == (int) ExchangeStatus.ExchangeStatusCanExchange);
        }

        public bool ReachExchangeTime()
        {
            var now = DateTimeOffset.Now.ToUnixTimeSeconds();
            return ExchangeInfo.StartTime >= now && ExchangeInfo.EndTime <= now;
        }

        internal Transform FindParent(Transform from, string name)
        {
            while (from != null)
            {
                var find = from.Find(name);
                if (find != null)
                {
                    return find;
                }
                from = from.parent;
            }
            return null;
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
