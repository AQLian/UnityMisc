using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HappyBridge.Audio;
using HappyBridge.UI;
using ItemIconHandlerUgui = HappyMahjong.Common.ItemIconHandlerUgui;
using BubbleBehaviour = HappyMahjong.Common.BubbleBehaviour;
using BackComponent = HappyMahjong.Setting.BackComponent;
using PopUpType = HappyMahjong.Common.PopUpType;
using HappyBridge.Util;
using System;
using UnityEngine.UI;

namespace StreakBallSpace
{
    public class StreakBallMainPopupHandler : StreakBallPopupHandler
    {
        private Action m_onCloseCallback;
        private string m_url;
        private GameObject m_panel;
        
        public void Init(Configuration.WebAnnConfig data,Action onCloseCallback,int activityId)
        {
            StreakBallUtil.RecordEvent((int) ReportEventType.PopUpViewShow);

            m_url = data.url;
            m_panel = gameObject;
            m_onCloseCallback = onCloseCallback;
            
            //返回按钮
            BindPopupBackBtn(transform.Find("Image/btn_close"));

            SetImage();

            //跳转url
            Transform tfClick = transform.Find("Image");
            tfClick.GetComponent<RectTransform>().sizeDelta = new Vector2(1334, 750);
            var tfClickNodePrefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "WebAnnouncementClick");
            if (tfClickNodePrefab != null)
            {
                var tfClickNode = UIUtil.Instantiate(tfClickNodePrefab, tfClick).transform;
                tfClickNode.SetAsFirstSibling();
                tfClick = tfClickNode.Find("Empty");
            }
            UIEventListener.Get(tfClick.gameObject).onClick = (btnObj) =>
            {
                StreakBallUtil.RecordBtnEvent((int) ReportButtonType.PopUpButtonClick);
                if (gameObject!=null)
                {
                    PopUpManager.GetInstance().RemovePopUp(gameObject);
                }

                bubble.ContextDispatcher(transform, StreakBallEvent.ShowStreakBall);
            };
        }

        private void SetImage()
        {
            LoadImage(m_panel, m_url);
        }
        
        private void LoadImage(GameObject panel, string url)
        {
            if (panel == null)
            {
                return;
            }
            var tfTexture = panel.transform.Find("Image");
            if (tfTexture == null)
            {
                return;
            }
            var loading = panel.transform.Find("Loading");
            if (loading != null)
            {
                loading.gameObject.SetActive(true);
            }
            var rawImage = tfTexture.gameObject.GetComponent<RawImage>();
            rawImage.enabled = true;
            Util.SetWebTextureUIUGUI(tfTexture, url, null, OnURlImageLoaded);
        }

        private void OnURlImageLoaded(string url, Texture texture)
        {
            if (m_url == url && texture != null)
            {
                if (m_panel != null)
                {
                    var image = m_panel.transform.Find("Image").GetComponent<RawImage>();
                    image.enabled = true;
                    var effect = image.transform.Find("ImageEffect");
                    if (effect != null)
                    {
                        effect.gameObject.SetActive(true);
                    }

                    Transform loading = m_panel.transform.Find("Loading");
                    if (loading != null)
                    {
                        loading.gameObject.SetActive(false);
                    }

                    image.SetNativeSize();
                }
            }
        }

        public override void ClosePopupHandler()
        {
            base.ClosePopupHandler();
            if (m_onCloseCallback!=null)
            {
                m_onCloseCallback();
            }
        }
    }
}
