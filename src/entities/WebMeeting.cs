using System;

namespace dcinc.api.entities
{
    /// <summary>
    /// Web会議を表す
    /// </summary>
    public class WebMeeting
    {
        /// <summary>
        /// 既定のコンストラクタ
        /// </summary>
            public WebMeeting()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 一意とするID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Web会議名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Web会議の開始日時
        /// </summary>
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// Web会議の日付
        /// </summary>
        public DateTime Date => StartDateTime.Date;
        /// <summary>
        /// Web会議のURL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 登録者
        /// </summary>
        public string RegisteredBy { get; set; }
        /// <summary>
        /// 登録日時（UTC）
        /// </summary>
        public DateTime RegisteredAt { get; set; }
        /// <summary>
        /// 通知先のSlackチャンネル
        /// </summary>
        public string SlackChannelId { get; set; }
    }
}