namespace HappyMahjong.StreakBallSpace
{
    public class UIDef
    {
        // 配置key，用来获取配置 例如 var isOpen = DynamicConfig.GetInstance().GetBool(UIDef.ConfigKey, "IsOpen");
        public static string ConfigKey = "StreakBall";
        // 用来指定获取ab的路径
        public static string StreakBallABPath = "RawResources_New/StreakBall_ab";

        // 用于修改标签
        public static string[] IMAGE_LEVEL_LIST = new string[]
        {
            "Shop_Img_Item_Level_Green",
            "Shop_Img_Item_Level_Blue",
            "Shop_Img_Item_Level_Purple",
            "Shop_Img_Item_Level_Gold",
            "Shop_Img_Item_Level_Diamond"
        };

        public static string[] TEXT_LEVEL_LIST = new string[]
        {
            "普通",// c 1
            "稀有",// b 2
            "极品",// a 3
            "传说",// s 4
            "至臻" // ssr 5
        };

        public static string SSRTextColor => "#5038c0";

        public static string LastSelectedSlotIdPrefKey => $"{ConfigKey}-LastSlotId";

        public const bool TestMode = true;
    }

    public class StreakBallConfig
    {
        // 活动的入口按钮名
        public static string ActivityIconName = "StreakBall";
        // 活动的面板名
        public static string ActivityPanelName = "StreakBall";
        // 活动id
        public static int ActivityId = 0;
        // 代币id
        public static int CoinId = 0;
        public static int BankruptSourceType = 3;  // 主域：  结算时正常弹出 InBalanceNormal = BankruptScene.POP_WINDOW_BALAN
    }

    public class StreakBallCommonProtocolKey
    {
        public const string JumpToStreakBall = "JumpTo" + UIDef.ConfigKey;
        public const string PopupStreakBall = "Popup" + UIDef.ConfigKey;
    }

    public class ModuleType
    {
        // 模块名，一般用来打log
        public const string StreakBall = "StreakBall";
    }

    //自动还是手动装备
    public enum EquipType
    {
        AutoEquip = 1,   // 自动配装
        ManualEquip = 2, // 手动配装
    }

    //灵珠装备状态
    public enum OrbEquipState
    {
        NotFind = -1,      // 未找到对应灵珠

        NotOwned = 0,      // 未拥有
        NotEquiped = 1,    // 已拥有，未装备
        Equiped = 2,       // 已拥有，已装备
    }

    //灵珠对应的技能状态
    public enum OrbSkillState
    {
        CanUse  = 0,       // 技能正常可用
        Expired = 1,       // 已过期
        ExceedLimit = 2,   // 超出上限
        Cooldown = 3,      // 冷却中
        CooldownPaused = 4,// 灵珠未装备，冷却暂停中（装备后服务端会重新计算该值）
    }

    public class LangKeys
    {
        public static string activityNotOpen = "活动已下线";

        // 领取外显(固定周期内可以开启灵珠天赋技能）
        public static string skillCooldownRemainLong = "{0:#0}天{1:#0}时";
        public static string skillCooldownRemainMiddle = "{0:#0}时{1:#0}分";
        public static string skillCooldownRemainShort = "{0:#0}分{1:#0}秒";

        public static string tempNotOpen = "暂未开启敬请期待";
        public static string reviveSuccess = "复活成功~";
    }

    public struct CoinBuyInfo
    {
        public int boughtNum;
        public int buyLimit;
    }

    public enum ReportEvent
    {
        StreakBallMainUIExposed = 10072,                 //连胜球首页曝光
        StreakBallDetailUIExposed = 10073,               //连胜球兑换界面曝光

        StreakBallMainUISelectableGetSuccess = 10074,    //天赋阁首页入场卡 成功领取曝光
        StreakBallDetailUISelectableGetSuccess = 10075,      //天赋阁详情入场卡 成功领取曝光

        StreakBallNormalBtnExposed, //连胜球普通入口曝光
        StreakBallTopRightBtnExposed, //连胜球右上角入口曝光
    }

    public enum ReportButton
    {
        StreakBallMainUIAddbtnClick = 10072,        //天赋阁首页点击“+”按钮点击
        StreakBallMainUIExchangeClick = 10073,       //点击兑换按钮
        StreakBallMainUIDetailClick = 10074,        //天赋阁首页点击“详情”按钮
        StreakBallMainUISelectableClick = 10075,    //天赋阁首页入场卡领取按钮
        StreakBallDetailUISelectableClick = 10076,  //天赋阁详情入场卡领取按钮
        StreakBallDetailUICenterBtnClick = 10077,   //天赋阁详情按钮(区分装备、卸下、前往获取、失效等不同状态)


        StreakBallNormalBtn, //连胜球普通入口点击
        StreakBallTopRightBtn, //连胜球右上角入口点击
    }
}// 自动生成于：8/12/2025 3:37:51 PM
