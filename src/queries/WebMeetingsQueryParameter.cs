using System;

namespace dcinc.api.queries
{
    /// <summary>
    /// Web会議を取得する抽出条件パラメータを表す
    /// </summary>
    public class WebMeetingsQueryParameter
    {
        /// <summary>
        /// Web会議の日付範囲の開始日
        /// </summary>
        public DateTime? FromDate { get; set; }
        /// <summary>
        /// Web会議の日付範囲の終了日
        /// </summary>
        public DateTime? ToDate { get; set; }
        /// <summary>
        /// 登録者
        /// </summary>
        public string RegisteredBy { get; set; }
        /// <summary>
        /// 通知先のSlackチャンネル
        /// </summary>
        public string SlackChannelId { get; set; }

        /// <summary>
        /// Web会議の日付範囲の開始日が指定されているか
        /// </summary>
        public bool HasFromDate {
            get {
                return (FromDate != null && FromDate.HasValue);
            }
        }

        /// <summary>
        /// Web会議の日付範囲の開始日(未指定の場合はUNIXエポック)
        /// ※UNIXエポック：1970-01-01 00:00:00.0000000 UTC
        /// </summary>
        public DateTime FromDateUtcValue {
            get {
                return HasFromDate ? FromDate.Value.Date.ToUniversalTime() : DateTime.UnixEpoch;
            }
        }

        /// <summary>
        /// Web会議の日付範囲の終了日が指定されているか
        /// </summary>
        public bool HasToDate {
            get {
                return (ToDate != null && ToDate.HasValue);
            }
        }

        /// <summary>
        /// Web会議の日付範囲の開始日(未指定の場合はDateTimeの最大値)
        /// </summary>
        public DateTime ToDateUtcValue {
            get {
                return HasToDate ? ToDate.Value.Date.ToUniversalTime().AddDays(1).AddMilliseconds(-1) : DateTime.MaxValue;
            }
        }

        /// <summary>
        /// 登録者が指定されているか
        /// </summary>
        public bool HasRegisteredBy {
            get {
                return !string.IsNullOrEmpty(RegisteredBy);
            }
        }

        /// <summary>
        /// 通知先のSlackチャンネルが指定されているか
        /// </summary>
        public bool HasSlackChannelId {
            get {
                return !string.IsNullOrEmpty(SlackChannelId);
            }
        }
    }
}