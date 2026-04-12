// View Object
// 用来封装逻辑层传递到UI的数据
using System;
using System.Collections.Generic;
using System.Linq;

using Configuration;

using HappyMahjong.Common;
using HappyMahjong.ReturningActivity;
using HappyMahjong.ShopAndBag;

using TalentPavillion;

using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public struct StreakBallVO
    {
        // 业务UI数据
    }

    //这个代币的我先留着吧，万一后面有升级的需求
    public struct UpgradeCoin
    {
        public int itemID;
        public int cost;     // 所需数量
        public int price;    // 单个代币钻石数
        public int buyLimit; // 购买数量限制
    }

    public class ShowStreakBallDetailVO
    {
        public int ItemId { get; set; }                 // 直接显示某个技能
        public bool TryShowGuide { get; set; } = true;  // 尝试显示引导
        public int SlotId { get; set; }                 // 尝试装备到这个指定槽位,否则就是第一个空槽位装备
        public bool ExitCloseAll { get; set; } = true;  // 详情也返回连同主界面都关闭
    }

    public class StreakBallReviveWithDiamondVO
    {
        public MainArenaServiceDataJson Data { get; set; } // 复活来源
        public int Diamond { get; set; }
        public int PackageId { get; set; }
    }

}// 自动生成于：8/12/2025 3:37:51 PM
