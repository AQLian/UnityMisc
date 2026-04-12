using HappyBridge.UI;
using HappyBridge.Util;

using strange.extensions.command.impl;
using UnityEngine;


namespace HappyMahjong.StreakBallSpace
{
    public class ShowStreakBallViewCommand : EventCommand
    {
        [Inject]
        public StreakBallModel Model { get; set; }
        [Inject]
        public StreakBallService Service { get; set; }
        
        private string m_popupSaveKey;
        public override void Execute()
        {
            StreakBallUtil.LogPlatform("ShowStreakBallViewCommand");
            m_popupSaveKey = $"{UIDef.ConfigKey}_ShowPopup";
            bool isShowPopup = false;
            string jumValue = string.Empty;
            if (evt.data != null)
            {
                if (evt.data is Configuration.WebAnnConfig)
                {
                    var webAnnConfig = evt.data as Configuration.WebAnnConfig;
                    if (webAnnConfig != null)
                    {
                        isShowPopup = true;
                    }
                }
                else if (evt.data is string)
                {
                    jumValue = (evt.data as string);
                }
            }

            bool isShow = ShowView(isShowPopup, jumValue);
            if (isShowPopup)
            {
                if (isShow)
                {
                    //增加计数
                    HappyMahjong.Common.PopupWindowQuene.GetInstance().AddPopTimes();
                    //保存每日拍脸记录
                    PlayerPrefsHelper.SetInt(m_popupSaveKey, ServerTime.GetInstance().GetTodayZeroTimeStamp());
                }
                else
                {
                    PopupWindowQuene.GetInstance().PopupNext();
                }
            }
        }

        private bool ShowView(bool isShowPopup, string jumValue)
        {
            StreakBallUtil.LogPlatform($"ShowStreakBallViewCommand bResReady {Model.IsResourceReady} IsValid {Model.IsValid} isShowPopup:{isShowPopup}");
            if (!Model.IsResourceReady)
            {
                return false;
            }
            if (!Model.IsValid)
            {
                return false;
            }
            if (isShowPopup)
            {
                bool isShowPopupToday = ServerTime.GetInstance().GetTodayZeroTimeStamp() == PlayerPrefsHelper.GetInt(m_popupSaveKey);
                if (isShowPopupToday)
                {
                    StreakBallUtil.LogPlatform("ShowStreakBallCommand ShowPopupToday is true.");
                    return false;
                }

                var prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "StreakBallPopupPanel");
                if (prefab == null)
                {
                    StreakBallUtil.LogPlatform("StreakBallPopupPanel is null.");
                    return false;
                }
                var panel = UIUtil.Instantiate(prefab);
                if (panel == null)
                {
                    StreakBallUtil.LogPlatform("StreakBallPopupPanel instantiate prefab failed.");
                    return false;
                }
                var handler = panel.GetOrAddComponent<StreakBallPopupPanelHandler>();
                if (handler == null)
                {
                    StreakBallUtil.LogPlatform("StreakBallPopupPanel add handler failed.");
                    if (panel != null)
                    {
                        GameObject.Destroy(panel);
                    }
                    return false;
                }
                handler.Init(true,jumValue);
                return true;
            }
            return false;
        }
    }
}
