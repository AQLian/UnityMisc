using System.Collections;
using System.Collections.Generic;
using HappyBridge.Networking;
using HappyBridge.UI;
using HappyBridge.Util;
using strange.extensions.command.impl;
using UnityEngine;
using DownloadPicReq = HappyMahjong.Common.DownloadPicReq;
using PopupParam = HappyMahjong.Common.PopupWindowQuene.PopupParam;
using BackComponent = HappyMahjong.Setting.BackComponent;
using PopUpType = HappyMahjong.Common.PopUpType;
using System;

namespace StreakBallSpace
{
    public class ShowPopUpViewStreakBallCmd : EventCommand
    {
        [Inject] public StreakBallModel Model { get; set; }

        public override void Execute()
        {
            StreakBallUtil.LogPlatform($"ShowPopupStreakBallCommand IsValid:{Model.IsValid} IsAddPopupQueue:{Model.IsAddPopupQueue}");

            if (Model.IsAddPopupQueue)
            {
                return;
            }

            if(!Model.IsValid)
            {
                return;
            }

            //弹窗数据
            if (evt.data is Configuration.WebAnnConfig data)
            {
                //可领奖，弹出拍脸
                Model.IsAddPopupQueue = true;
                PopupParam param = new PopupParam();
                param.nID = data.id;
                param.type = HappyMahjong.Common.PopupWindowQuene.PopupType.ILRuntimePanel;
                param.style = HappyMahjong.Common.PopupWindowQuene.PopupStyle.AutoCallPopStyle;
                param.needCountNum = true;
                param.asynResLoadReq = () =>
                {
                    OnShowPopupWindow(data);
                };
                PopupWindowQuene.GetInstance().AddPopup(param);

                //添加缓存图片
                if (!string.IsNullOrEmpty(data.url))
                {
                    AddShareImageDownload(data.url);
                }
            }
        }


        private void OnShowPopupWindow(Configuration.WebAnnConfig data)
        {
            if (CheckShowPopup())
            {
                if (ShowPopupWindow(data, () =>
                    {
                        PopupWindowQuene.GetInstance().PopupNext();
                    }))
                {
                    HappyMahjong.Common.PopupWindowQuene.GetInstance().AddPopTimes();
                }
            }
            else
            {
                PopupWindowQuene.GetInstance().PopupNext();
            }
        }

        private bool ShowPopupWindow(Configuration.WebAnnConfig data,Action onCloseCallback)
        {
            var template = UIUtil.LoadPrefab(UIDef.MainScenePrefabPath, "WebAnnouncement");
            if (template == null)
            {
                return false;
            }
            
            var panel = GameObject.Instantiate(template);
            var poupHandler = panel.GetOrAddComponent<StreakBallMainPopupHandler>();
            int activityId = Model.Info.ActId;
            poupHandler.Init(data,onCloseCallback,activityId);
            return true;
        }

        private bool CheckShowPopup()
        {
            bool showPopup = false;
            if (Model.IsValid)
            {
                var popupSaveKey = $"{UIDef.ConfigKey}_ShowPopup";
                bool isShowPopupToday = ServerTime.GetInstance().GetTodayZeroTimeStamp() == PlayerPrefsHelper.GetInt(popupSaveKey);
                Log.Info($"CheckShowPopup isShowPopupToday:{isShowPopupToday}",ModuleType.StreakBall);
                if (!isShowPopupToday)
                {
                    PlayerPrefsHelper.SetInt(popupSaveKey,ServerTime.GetInstance().GetTodayZeroTimeStamp());
                    showPopup = true;
                }
            }
            return showPopup;
        }

        private void AddShareImageDownload(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            url = url.Replace("///", "//");

            var mDownloadPicReq = new DownloadPicReq();
            mDownloadPicReq.mUrl = url;
            mDownloadPicReq.mCallBack = null;
            DownloadPicManager.GetInstance().AddDownloadReq(mDownloadPicReq);
        }
    }
}
