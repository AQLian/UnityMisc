using System;
using System.Collections.Generic;

using Google.Protobuf;

using HappyMahjong.Common;

using PostSvr;
using strange.extensions.context.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using UnityEngine.Pool;
using UnityEngine;
using HappyMahjong.ActivityActivityBindPhone;
using TSDK4CSharp;
using MahjongRoundRecord;
using System.Runtime.InteropServices;
using Google.Protobuf.WellKnownTypes;

#if !compatible_758
using CgiRspInfo = HappyMahjong.TSDKMessage.CgiRspInfo;
#endif
using ErrorCode = MJWinStreakBallActivity.ErrorCode;
using System.Reflection;
using System.Collections.Concurrent;
using static HappyMahjong.StreakBallSpace.StreakBallService;
using MJWinStreakBallActivity;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallService : IStreakBallService, ITSDKEventListener
    {
        [Inject(ContextKeys.CONTEXT_DISPATCHER)]
        public IEventDispatcher Dispatcher { get; set; }

        // 天赋阁grpc
        private static readonly string GRPCPrefix = "/MJWinStreakBallActivity.MJWinStreakBallActivityService/";
        public static readonly string GRPC_REQ_StreakBall_GETDETAILREQ = $"{GRPCPrefix}GetDetail";
        public static readonly string GRPC_REQ_StreakBall_CLAIMWINSTREAKBALLREQ = $"{GRPCPrefix}ClaimWinStreakBall";
        public static readonly string GRPC_REQ_StreakBall_EXCHANGEREQ = $"{GRPCPrefix}Exchange";
        public static readonly string GRPC_REQ_StreakBall_ADSCALLBACKREQ = $"{GRPCPrefix}AdsCallBack";
        private Dictionary<string, Action<byte[], int>> m_grpcHandlers = new Dictionary<string, Action<byte[], int>>();


        public StreakBallService()
        {
            ModuleRegistry.GetInstance().InjectGRPCService(GRPC_REQ_StreakBall_GETDETAILREQ, this);
            ModuleRegistry.GetInstance().InjectGRPCService(GRPC_REQ_StreakBall_CLAIMWINSTREAKBALLREQ, this);
            ModuleRegistry.GetInstance().InjectGRPCService(GRPC_REQ_StreakBall_EXCHANGEREQ, this);
            ModuleRegistry.GetInstance().InjectGRPCService(GRPC_REQ_StreakBall_ADSCALLBACKREQ, this);
            RegisterMethodHandler<GetDetailReq, GetDetailRes>(StreakBallEvent.RspGetDetail);
            RegisterMethodHandler<ClaimWinStreakBallReq, ClaimWinStreakBallRes>(StreakBallEvent.RspClaimWinStreakBall);
            RegisterMethodHandler<ExchangeReq, ExchangeRes>(StreakBallEvent.RspExchange);
            RegisterMethodHandler<AdsCallBackReq, AdsCallBackRes>(StreakBallEvent.RspAdsCallBack);
        }

        #region 错误码
        private const string MDefaultTIp = "服务器开小差，请稍后重试";
        /// <summary>
        /// 错误码到描述的映射表
        /// </summary>
        private static readonly Dictionary<int, string> ErrorCodeMessages = new Dictionary<int, string>
        {
            { (int)ErrorCode.ParamError, "参数错误" },
            { (int)ErrorCode.ConfigNotFound, "活动配置没有找到" },
            { (int)ErrorCode.AmsParamRequire, "ams param require" },
            { (int)ErrorCode.OpenIdRequire, "open id require" },
            { (int)ErrorCode.ExpressItemFailed, "发货失败" },
            { (int)ErrorCode.ActivityEnd, "活动已经过期" },
            { (int)ErrorCode.InvalidConfig, "配置文件无效" },
            { (int)ErrorCode.SaveDataFailed, "数据保存失败" },
            { (int)ErrorCode.QueryDataFailed, "数据库查询失败" },
            { (int)ErrorCode.RewardIsReceived, "奖励已经领取" },
            { (int)ErrorCode.WhiteListAuthFailed, "白名单授权失败" },
            { (int)ErrorCode.NoQualifier, "没有资格参加活动" },
            { (int)ErrorCode.RewardUnachieved, "天赋阁未开启" },
            { (int)ErrorCode.RewardTimeout, "奖励已经过期" },
            { (int)ErrorCode.RewardNotFound, "奖励没有找到" },
            { (int)ErrorCode.SystemError, MDefaultTIp },
        };

        /// <summary>
        /// 检查并处理错误码，如果是错误则弹窗并返回 true
        /// </summary>
        private bool CheckAndHandleErrorCode(int retCode, string methodName)
        {
            // retCode == 0 表示成功
            if (retCode == 0)
            {
                return false;
            }

            // 查找错误描述
            if (ErrorCodeMessages.TryGetValue(retCode, out var errorMessage))
            {
                Log.Info($"[{methodName}] Error code {retCode}: {errorMessage}", ModuleType.StreakBall);
                //DialogManager.GetInstance().ShowDialogUgui(DialogManager.BTN_STYLE_ONE, null, errorMessage, LanguageKey.SURE);
            }
            else
            {
                // 未知错误码，显示通用错误信息
                Log.Info($"[{methodName}] Unknown error code: {retCode}", ModuleType.StreakBall);
                //DialogManager.GetInstance().ShowDialogUgui(DialogManager.BTN_STYLE_ONE, null, $"请求失败 (错误码: {retCode})", LanguageKey.SURE);
            }

            return true; // 表示有错误
        }
        #endregion

        #region 请求

        /// <summary>
        /// 请求活动数据
        /// </summary>
        public void GetDetailReq(List<uint> othersUin = null)
        {
            Log.Info("GetDetailReq", ModuleType.StreakBall);
            var req = new GetDetailReq();
            var playerBaseInfo = TSDK4CSharp.TSDKService.GetSelfInfoData();
            req.ActId = StreakBallConfig.ActivityId;
            req.OpenKey = playerBaseInfo.strOpenId;
            req.PfKey = playerBaseInfo.strPfKey;
            req.Pf = playerBaseInfo.strPf;
            req.PayToken= playerBaseInfo.strPayToken;
            if (othersUin != null)
            {
                req.OtherUins = new uint[othersUin.Count];
                for(var i =0; i < othersUin.Count; i++)
                {
                    req.OtherUins[i] = othersUin[i];
                }
            }

            SendGrpc<GetDetailReq>(GRPC_REQ_StreakBall_GETDETAILREQ, req);
        }

        /// <summary>
        /// 请求领取
        /// </summary>
        public void ClaimWinStreakBallReq()
        {
            Log.Info("ClaimWinStreakBallReq", ModuleType.StreakBall);
            var req = new ClaimWinStreakBallReq();
            var playerBaseInfo = TSDK4CSharp.TSDKService.GetSelfInfoData();
            req.ActId = StreakBallConfig.ActivityId;
            req.OpenKey = playerBaseInfo.strOpenId;
            req.PfKey = playerBaseInfo.strPfKey;
            req.Pf = playerBaseInfo.strPf;
            req.PayToken = playerBaseInfo.strPayToken;

            SendGrpc<ClaimWinStreakBallReq>(GRPC_REQ_StreakBall_CLAIMWINSTREAKBALLREQ, req);
        }

        /// <summary>
        /// 请求兑换
        /// </summary>
        /// <param name="id"></param>
        public void ExchangeReq(int id)
        {
            Log.Info("ExchangeReq", ModuleType.StreakBall);
            var req = new ExchangeReq();
            var playerBaseInfo = TSDK4CSharp.TSDKService.GetSelfInfoData();
            req.ActId = StreakBallConfig.ActivityId;
            req.OpenKey = playerBaseInfo.strOpenId;
            req.PfKey = playerBaseInfo.strPfKey;
            req.Pf = playerBaseInfo.strPf;
            req.PayToken = playerBaseInfo.strPayToken;
            req.Id = id;

            SendGrpc<ExchangeReq>(GRPC_REQ_StreakBall_EXCHANGEREQ, req);
        }

        /// <summary>
        /// 请求广告
        /// </summary>
        /// <param name="orbId"></param>
        /// <param name="skillType"></param>
        /// <param name="skillId"></param>
        /// <param name="itemInfo"></param>
        public void AdsCallBackReq()
        {
            Log.Info("AdsCallBackReq", ModuleType.StreakBall);
            var req = new AdsCallBackReq();
            var playerBaseInfo = TSDK4CSharp.TSDKService.GetSelfInfoData();
            req.ActId = StreakBallConfig.ActivityId;
            req.OpenKey = playerBaseInfo.strOpenId;
            req.PfKey = playerBaseInfo.strPfKey;
            req.Pf = playerBaseInfo.strPf;
            req.PayToken = playerBaseInfo.strPayToken;

            SendGrpc<AdsCallBackReq>(GRPC_REQ_StreakBall_ADSCALLBACKREQ, req);
        }
        #endregion

        #region 回包

        public void OnReceiveGRPCData(string reqPath, byte[] buffer, long len)
        {
            Log.Info("[StreakBallOnReceiveGRPCData] " + reqPath, ModuleType.StreakBall);
            if(m_grpcHandlers.TryGetValue(reqPath, out var handler))
            {
                handler?.Invoke(buffer, (int) len);
            }
            else
            {
                Log.Info($"No handler registered for {reqPath}", ModuleType.StreakBall);
            }
        }

        public void OnGPRCMsgFailed(string reqMethod, int errorCode)
        {
            var rsp = new RspStreakBallTSDKDO();
            rsp.ReqMethod = reqMethod;
            rsp.ErrorCode = errorCode;

            Log.Info($"StreakBall OnRecieveMsgFailed {reqMethod} {errorCode}", ModuleType.StreakBall);
            Dispatcher.Dispatch(StreakBallEvent.RspStreakBallFail, rsp);
        }

        private string GetMetaData()
        {
            var tsdkBaseInfo = TSDK4CSharp.TSDKService.GetTSDKGameData();
            var playerBaseInfo = TSDK4CSharp.TSDKService.GetSelfInfoData();

            return string.Format("uin:{0};game_id:{1};account_type:{2};client_type:{3};open_id{4}",
                playerBaseInfo.unGameUin,
                tsdkBaseInfo.shGameID,
                (int) playerBaseInfo.eAccountType,
                tsdkBaseInfo.nClientType,
                playerBaseInfo.strOpenId);
        }

        private void RegisterMethodHandler<Req, Rsp>(StreakBallEvent onSuccessEvt)
            where Req : global::ProtoBuf.IExtensible, new()
            where Rsp : global::ProtoBuf.IExtensible, new()
        {
            var reqName = typeof(Req).Name.Replace("Req", "");
            var key = $"{GRPCPrefix}{reqName}";
            if (!m_grpcHandlers.TryGetValue(key, out var handler))
            {
                m_grpcHandlers.Add(key, (buffer,len) =>
                {
                    var rsp = Util.DeserializeProtoBufferMsg<Rsp>(buffer, len);
                    var getter = PropertyAccessors<Rsp>.CreateGetter<int>("retCode");
                    var retCode = getter(rsp);
                    if (CheckAndHandleErrorCode(retCode, $"Handle{reqName}Rsp"))
                    {
                        OnGPRCMsgFailed(reqName, retCode);
                        return;
                    }

#if !LIVE_BUILD
                    Log.Info($"[GRPC] {reqName} response:{rsp.GetType().Name} data: {LitJson.JsonMapper.ToJson(rsp)}", ModuleType.StreakBall);
#endif
                    Dispatcher.Dispatch(onSuccessEvt, rsp);
                });
            }
        }

        private void SendGrpc<T>(string strMethodName, T req) where T : global::ProtoBuf.IExtensible, new()
        {
            var metaStr = GetMetaData();
            using var ms = new System.IO.MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, req);
            var buffer = ms.GetBuffer();
            var usedSize = (int) ms.Length;
            Util.SendGRPC(strMethodName, buffer, usedSize, metaStr);
        }
        #endregion

        #region ITSDKEventListener Implementation (Unused)

        void ITSDKEventListener.OnRecieveData(int svrMsgID, byte[] buffer, int bufferLen)
        {
            // Not used in this service
        }

        void ITSDKEventListener.OnRecieveMsgFailed(int svrID, int errorCode)
        {
            // Not used in this service
        }

        void ITSDKEventListener.OnRecieveCgiData(int cgiId, CgiRspInfo info)
        {
            // Not used in this service
        }

        void ITSDKEventListener.OnRspPostSvrNotifyMsg(TMsg tMsg)
        {
            // Not used in this service
        }

        #endregion
    }
}// 自动生成于：8/12/2025 3:37:51 PM
