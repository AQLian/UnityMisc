using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;
using TalentPavillion;
using HappyBridge.Util;
using System.Collections.Generic;
using HappyMahjong.ShopAndBag;
using Configuration;
using static HappyMahjong.Common.UIEventListenerEx;
using HappyMahjong.ResHotUpdate;

namespace HappyMahjong.StreakBallSpace
{
    public class SkillRightItemDetailHandler : LoadImageBase
    {
        public OrbInfo OrbInfo { get; internal set; }
        public TalentSkillItemConfig Config { get; internal set; }

        public Image introImage;
        public TextMeshProUGUI skillDesc;
        public TextMeshProUGUI skillBgDesc;
        public GameObject btn;
        public TextMeshProUGUI btnText;
        public TextMeshProUGUI btnText_Gray;
        public TextMeshProUGUI btnCooldownText;

        protected override void Awake()
        {
            base.Awake();
        }

        public void OrbInfoUpdate(OrbInfo OrbInfo)
        {
        }

        internal void SetBtnText(string text)
        {
            btnText.text = text;
            btnText_Gray.text = text;
        }
    }
}
