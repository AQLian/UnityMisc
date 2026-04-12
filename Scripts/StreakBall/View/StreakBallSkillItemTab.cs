using System;
using System.Collections;
using System.Collections.Generic;

using Configuration;

using HappyMahjong.Common;
using HappyMahjong.ShopAndBag;
using HappyMahjong.StreakBallSpace;

using TalentPavillion;

using TMPro;

using UGUIExtend;

using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{

    public class TalentSkillItemTab : LoadImageBase
    {
        private Image m_iconImage;
        public static int FirstSelect { get; set; } = -1;
        private static GameObject s_lastSelected;
        private GameObject m_selectedObj;
        private TextMeshProUGUI m_name;
        private GameObject m_bg;

        public GameObject stateExpired;
        public GameObject stateLocked;
        public GameObject stateUsing;
        public GameObject levelTag;
        public GameObject redDot;
        public float redDotUpdateInterval = 3f;

        public OrbInfo curItem { get; set; }

        protected override void Awake()
        {
            base.Awake();
            UIEventListener.Get(gameObject, ClickableTypeDef.SwitchSoundType).onClick = (go) => OnClick(go, true);
            m_selectedObj = transform.Find("Selected").gameObject;
            m_bg = transform.Find("BG").gameObject;
            m_iconImage = transform.Find("Image").GetComponent<Image>();
            m_name = transform.Find("Name").GetComponent<TextMeshProUGUI>();
            UnSelect();
            m_clip = gameObject.GetOrAddComponent<UIClippable>();
        }

        internal void OnClick(GameObject go, bool userClick = false)
        {
            Select();
        }

        private static string s_selectedBgName = "HBG";
        private static string s_unselectedBgName = "LBG";
        public void Select()
        {
            m_selectedObj.SetActive(true);
            UIUtil.SetImageSpriteSync(m_bg.GetComponent<Image>(), UIDef.StreakBallABPath, s_selectedBgName);

            if (s_lastSelected != m_selectedObj)
            {
                if(s_lastSelected != null)
                {
                    s_lastSelected.SetActive(false);
                    UIUtil.SetImageSpriteSync(s_lastSelected.transform.parent.Find("BG").GetComponent<Image>(), UIDef.StreakBallABPath, s_unselectedBgName);
                }

                s_lastSelected = m_selectedObj;
                var eventReceiver = GetComponentInParent<IScrollTabSelectEvent>();
                if (eventReceiver != null)
                {
                    eventReceiver.OnScrollTabSelect(curItem);
                }
            }
        }

        public void UnSelect()
        {
            m_selectedObj.SetActive(false);
        }

        internal void ScrollCellIndex(int idx)
        {
            var source = GetComponentInParent<IScrollSourceProvider>();
            if(source != null && source.DataSource is List<OrbInfo> orbs)
            {
                if(idx >= 0 && idx < orbs.Count)
                {
                    var orb = orbs[idx];
                    UpdateItem(orb, idx);
                    UpdateOrbState(orb);
                }
            }

            if (m_clip != null)
            {
                m_clip.ResetData();
            }

            if (FirstSelect > -1 && idx == FirstSelect)
            {
                FirstSelect = -1;
                Select();
            }
        }

        internal void SetPlaceHolder()
        {
            gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = false;
            m_selectedObj.SetActive(false);
            m_iconImage.gameObject.SetActive(false);
        }

        internal void UpdateOrbState(OrbInfo orbInfo)
        {
            orbInfo.GetOrbEquipState(out var equitState, out var skillState, out var _);
            var expired = skillState == OrbSkillState.Expired || skillState == OrbSkillState.ExceedLimit;
            stateExpired.SetActive(expired);
            if (!expired)
            {
                stateLocked.SetActive(equitState == OrbEquipState.NotOwned);    
            }
            else
            {
                stateLocked.SetActive(false);
            }
            stateUsing.SetActive(equitState == OrbEquipState.Equiped);
            redDot.SetActive(orbInfo.CanSelectableItem() == 0);

            StartTimer(redDotUpdateInterval, redDotUpdateInterval, () =>
            {
                if (this != null && redDot != null)
                {
                    redDot.SetActive(orbInfo.CanSelectableItem() == 0);
                }
            });
        }

        internal void UpdateItem(OrbInfo orbInfo, int loadKey)
        {
            this.loadKey = loadKey;
            curItem = orbInfo;
            gameObject.GetOrAddComponent<CanvasGroup>().blocksRaycasts = true;

            Transform iconTrans = m_iconImage.transform;
            if (iconTrans.childCount != 0)
            {
                Util.DestroyAllChildren(iconTrans);
            }
            
            m_iconImage.gameObject.SetActive(true);
            m_iconImage.enabled = true;
            
            var item = ShopDataHelper.GetInstance().GetItem(orbInfo.ItemId);
            var atlasName =  item?.atlasName;
            var spritename = item?.spriteName;
            var effectName = item?.effectName;
            
            LoadImage(m_iconImage, atlasName, spritename);
            LoadImageFx(m_iconImage, effectName);

            if (item != null && item.LevelNumber > 0 && item.LevelNumber - 1 < UIDef.IMAGE_LEVEL_LIST.Length)
            {
                var index = item.LevelNumber - 1;
                UIUtil.SetImageSpriteSync(levelTag.GetComponent<Image>(), UIDef.StreakBallABPath, UIDef.IMAGE_LEVEL_LIST[index]);

                var label = levelTag.GetComponentInChildren<TextMeshProUGUI>();
                label.text = UIDef.TEXT_LEVEL_LIST[index];
                ColorUtility.TryParseHtmlString(UIDef.SSRTextColor, out var ssrClr);
                label.color = item.LevelEnum == ItemLevel.SSR ? ssrClr : Color.white;
                levelTag.SetActive(true);
            }
            else
            {
                levelTag.SetActive(false);
            }
            if(orbInfo.TryGetTalentSkillItemConfig(out var c))
            {
                m_name.text = c.name;
            }
            else
            {
                m_name.text = "";
            }
        }
    }
}

