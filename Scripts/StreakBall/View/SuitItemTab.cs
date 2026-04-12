using System;
using System.Collections;
using System.Collections.Generic;
using HappyMahjong.Common;
using HappyMahjong.Puffer;
using HappyMahjong.ShopAndBag;
using UGUIExtend;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

using DeviceType = HappyMahjong.Common.DeviceType;

namespace HappyMahjong.StreakBallSpace
{
    public class SuitItemTab : MonoBehaviour
    {
        private Image m_iconImage;
        private GameObject m_selectedObj;
        private GameObject m_downloadObj;
        private StreakBallUIView m_view;

        private UIClippable m_clip;

        private void Awake()
        {
            UIEventListener.Get(gameObject).onClick = (go) => OnClick(go, true);
            m_selectedObj = transform.Find("Selected").gameObject;
            m_iconImage = transform.Find("Image").GetComponent<Image>();
            m_downloadObj = transform.Find("Download").gameObject;

            UIEventListener.Get(gameObject).onClick = (go) => OnClickDownload();
            
            UnSelect();

            m_clip = gameObject.GetOrAddComponent<UIClippable>();
        }

        private void Start()
        {
            if (m_clip)
            {
                RectMask2D mask = GetComponentInParent<RectMask2D>();
                if (mask)
                {
                    m_clip.mask = mask.rectTransform;
                    m_clip.IsUseClipRect(true);
                }
            }
        }

        internal void OnClick(GameObject go, bool userClick = false)
        {
            if (!userClick)
            {
                m_downloadObj.gameObject.SetActive(false);
            }
            
            Select();
        }

        private void OnClickDownload()
        {
        }

        public void Select()
        {
            m_selectedObj.SetActive(true);
        }

        public void UnSelect()
        {
            m_selectedObj.SetActive(false);
        }

        internal void ScrollCellIndex(int idx)
        {
            StreakBallUIView view = GetUIView();
            if (view)
            {
            }
            
            if (m_clip != null)
            {
                m_clip.ResetData();
            }
        }

        private StreakBallUIView GetUIView()
        {
            if (m_view)
            {
                return m_view;
            }

            m_view = GetComponentInParent<StreakBallUIView>();

            if (m_view == null)
            {
                Log.Error("GetUIView is null", ModuleType.StreakBall);
            }

            return m_view;
        }

        internal void SetPlaceHolder()
        {
            gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = false;
            m_selectedObj.SetActive(false);
            m_iconImage.gameObject.SetActive(false);
            m_downloadObj.gameObject.SetActive(false);
        }
        
        private string GetFileSizeString(long fileSize)
        {
            if (fileSize < 1024)
                return $"{fileSize}B";
            if (fileSize < 1024 * 1024)
                return $"{fileSize / 1024f:0.#}K";
            if (fileSize < 1024L * 1024 * 1024)
                return $"{fileSize / 1024f / 1024f:0.#}M";
            if (fileSize < 1024L * 1024 * 1024 * 1024)
                return $"{fileSize / 1024f / 1024f / 1024f:0.#}G";

            return $"{fileSize / 1024f / 1024f / 1024f / 1024f:0.#}T";
        }
    }
}

