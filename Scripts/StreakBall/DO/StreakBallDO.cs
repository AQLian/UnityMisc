// Data Object
// 用来封装上下行的数据,可以减少Command数量
using System.Collections.Generic;
namespace HappyMahjong.StreakBallSpace
{
    #region 请求DO
    
    public enum ReqSSRCMD_ID
    {
        CMD_Query = 1,
        CMD_Upgrade = 2,
        CMD_Buy = 3,
    }
    
    // 请求服务器数据
    public struct BuyCoinVO
    {
        public int itemID;
        public int count;
    }
    
    public struct ReqStreakBallServiceDO
    {
        public ReqSSRCMD_ID cmd;
        public int itemID;
        public List<BuyCoinVO> buyCoinVo;
    }

    #endregion

    #region 回包DO
    
    // 服务器回包数据
    public struct RspStreakBallServiceDO
    {
        public int SvrId;
        // 这里会有多个协议数据,业务开发自行修改 or 添加
        public StreakBallOperationRsp OperationRsp;
    }
    
    // 回包的具体数据
    public struct StreakBallOperationRsp
    {
    }
    
    #endregion

    // 一般是后台异常了，TSDK超时回包用
    public struct RspStreakBallTSDKDO
    {
        public string ReqMethod;
        public int ErrorCode;
    }
}// 自动生成于：8/12/2025 3:37:51 PM
