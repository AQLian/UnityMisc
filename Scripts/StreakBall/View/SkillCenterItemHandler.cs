using System;
using System.Collections;
using System.Collections.Generic;

using Configuration;

using HappyBridge.Util;

using HappyMahjong.ResHotUpdate;
using HappyMahjong.ShopAndBag;
using HappyMahjong.Tutorial;

using TalentPavillion;

using TMPro;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using static HappyMahjong.Common.UIEventListenerEx;

namespace HappyMahjong.StreakBallSpace
{
    public class SkillCenterItemHandler : LoadImageBase
    {
        public OrbInfo OrbInfo { get; internal set; }
        public TalentSkillItemConfig Config { get; internal set;  }
        public bool BtnCanJumpable { get; private set; }

        public Image baseImage;
        public TextMeshProUGUI skillName;
        public TextMeshProUGUI skillActiveSourceDesc;
        public GameObject btn;
        public TextMeshProUGUI btnText;
        public TextMeshProUGUI btnText_Gray;

        protected override void Awake()
        {
            base.Awake();
        }

        public void OrbInfoUpdate(OrbInfo OrbInfo, int slotId, StreakBallModel model)
        {
            this.OrbInfo = OrbInfo;
            var name = "";
            if(OrbInfo.TryGetTalentSkillItemConfig(out var cfg))
            {
                name = StreakBallUtil.AddNewLineAfterEachChar(cfg.name);
            }
            skillName.text = name;
            loadKey = OrbInfo.ItemId;
            string atlas = "";
            string spriteName = "";
            string effectName = "";
            ItemConfigExtensions.ParseItemIcon(cfg?.model, ref atlas, ref spriteName, ref effectName);
            LoadImage(baseImage, atlas, spriteName);
            LoadImageFx(baseImage, effectName);
            skillActiveSourceDesc.text = cfg?.skillActiveSourceDesc ?? "";

            btn.SetActive(true);
            UIEventListener.VoidDelegate SkillAction = null;
            var owned = OrbInfo.IsOwned == 1;
            var equiped = OrbInfo.IsEquipped == 1;
            var reportBtnState = 0;
            BtnCanJumpable = false;
            var grayBtn = false;
            StreakBallUtil.SetBtnGray(btn, grayBtn);
            btnText.gameObject.SetActive(!grayBtn);
            btnText_Gray.gameObject.SetActive(grayBtn);

            UIEventListener.Get(btn, Common.ClickableTypeDef.ClickSoundType).onClick = _  =>
            {
                SkillAction?.Invoke(_);

                PlayerStatistics.GetInstance().RecordMessage((int) SNSType.ButtonClick, (int) ReportButton.StreakBallDetailUICenterBtnClick, snsFLowReportStr:new List<string> { reportBtnState.ToString() });
            };
        }

        internal void SetBtnText(string text)
        {
            btnText.text = text;
            btnText_Gray.text = text;
        }

        internal void TryShowGuide()
        {
            if (BtnCanJumpable)
            {
                Tutorial.TutorialController.GetInstance().ShowNewPlayerGuide((int) FlagIndexNew.ShowTalentSkillCanJumpAct);
            }
        }
    }
}
