using Google.Protobuf.WellKnownTypes;
using System;
using System.Text.Json.Serialization;

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

        #region プロパティ
        /// <summary>
        /// 一意とするID
        /// </summary>
        [JsonPropertyName("id")]
         public string Id { get; set; }
        /// <summary>
        /// Web会議名
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Web会議の開始日時
        /// </summary>
        [JsonPropertyName("startDateTime")]
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// Web会議の日付
        /// </summary>
        [JsonPropertyName("date")]
        [JsonConverter(typeof(dcinc.json.converters.UnixEpochDateTimeConverter))]
        public DateTime Date => StartDateTime.Date.ToUniversalTime();
        /// <summary>
        /// Web会議のURL
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }
        /// <summary>
        /// 登録者
        /// </summary>
        [JsonPropertyName("registeredBy")]
        public string RegisteredBy { get; set; }
        /// <summary>
        /// 登録日時（UTC）
        /// </summary>
        [JsonPropertyName("registeredAt")]
        public DateTime RegisteredAt { get; set; }
        /// <summary>
        /// 通知先のSlackチャンネル
        /// </summary>
        [JsonPropertyName("slackChannelId")]
        public string SlackChannelId { get; set; }

        #endregion
    }
}