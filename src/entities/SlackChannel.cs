using System;
using System.Text.Json.Serialization;

namespace dcinc.api.entities
{
    /// <summary>
    /// Slackチャンネルを表す
    /// </summary>
    public class SlackChannel
    {
        /// <summary>
        /// 既定のコンストラクタ
        /// </summary>
            public SlackChannel()
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
        /// Slackチャンネル名
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// SlackチャンネルのWebhook URL
        /// </summary>
        [JsonPropertyName("webhookUrl")]
        public string WebhookUrl { get; set; }
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

        #endregion
    }
}