using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scripts.NtpUtcNowSync
{
    public class BaiduUtc : MonoBehaviour
    {
        IEnumerator Start()
        {
            // 用百度/腾讯/阿里的HTTPS地址都行，稳定不易挂
            using (UnityWebRequest req = UnityWebRequest.Head("https://www.baidu.com"))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    // 读取响应头的 Date 字段
                    string dateStr = req.GetResponseHeader("Date");
                    var content = req.responseCode;
                    Debug.Log($"code: {content}");
                    if (DateTime.TryParseExact(dateStr, "r", null, System.Globalization.DateTimeStyles.None, out DateTime serverUtc))
                    {
                        Debug.Log($"获取到权威UTC时间：{serverUtc:o}");
                    }
                }
            }
        }
    }
}