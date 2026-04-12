using System.Collections;
using System.Collections.Generic;

using GCloud.MSDK;

using HappyBridge.Audio;
using HappyBridge.UI;
using HappyBridge.Util;

using HappyMahjong.Common;
using LitJson;
using strange.extensions.mediation.impl;

using UnityEngine;

using DialogManager = HappyBridge.UI.DialogManager;
using DynamicConfig = HappyBridge.Util.DynamicConfig;
using HLURLSystem = HappyBridge.Util.HLURLSystem;
using PopUpManager = HappyBridge.UI.PopUpManager;
using PopUpType = HappyMahjong.Common.PopUpType;
using PopupWindowQuene = HappyBridge.UI.PopupWindowQuene;
using Toast = HappyBridge.UI.Toast;
using UIUtil = HappyBridge.UI.UIUtil;
using Util = HappyBridge.Util.Util;

namespace StreakBallSpace
{
    public class StreakBallPopupPanelHandler : HappyMahjong.Common.BubbleBehaviour
    {
        private bool m_isShowPopup;
        private string m_jumValue;

        private Transform m_btnClose;
        private Transform m_btnGoto;
        private Transform m_btnHelp;
        private Transform m_btnSubscribe;
        private Transform m_btnSubscribed;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(bool isShowPopup, string jumValue)
        {
            StreakBallUtil.LogPlatform("StreakBallPopupPanelHandler");
            m_isShowPopup = isShowPopup;
            m_jumValue = jumValue;
            BindUI();

            //弹窗
            PopUpManager.GetInstance().AddPopUp(gameObject, PopUpType.UGUI);
        }

        private void BindUI()
        {
            m_btnClose = transform.Find("Trans_Adapter/Btn_Close");
            m_btnGoto = transform.Find("Trans_Adapter/Btn_Goto");
            m_btnHelp = transform.Find("Trans_Adapter/Btn_help");
            m_btnSubscribe = transform.Find("Trans_Adapter/Btn_subscribe");
            m_btnSubscribed = transform.Find("Trans_Adapter/Btn_subscribed");

            if (m_btnClose != null)
            {
                UIEventListener.Get(m_btnClose.gameObject).onClick = (btnObj) =>
                {
                    AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);

                    BtnCloseOnClick();
                };
            }

            if (m_btnGoto != null)
            {
                UIEventListener.Get(m_btnGoto.gameObject).onClick = (btnObj) =>
                {
                    AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);

                    BtnGotoOnClick();
                };
            }

            if (m_btnHelp != null)
            {
                UIEventListener.Get(m_btnHelp.gameObject).onClick = (btnObj) =>
                {
                    AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);

                    var rulesPrefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "StreakBallRulePanel");
                    if (rulesPrefab != null)
                    {
                        var rulesPanel = UIUtil.Instantiate(rulesPrefab);
                        rulesPanel.GetOrAddComponent<StreakBallRulesHandler>().Init();
                    }
                };
            }
        }

        private void ClosePanel()
        {
            PopUpManager.GetInstance().RemovePopUp(gameObject);
        }

        public void BtnCloseOnClick()
        {
            ClosePanel();
            // 拍脸显示的活动界面，关闭后popup队列中下一个界面
            HappyBridge.UI.PopupWindowQuene.GetInstance().PopupNext();
        }
        
        public void BtnGotoOnClick()
        {
            bubble.ContextDispatcher(transform, StreakBallEvent.ShowStreakBall);
            ClosePanel();
        }
    }
}
