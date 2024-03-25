using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using LinqKit;
using dcinc.api.entities;
using dcinc.json.converters;
using System.Text;

namespace dcinc.api.queries
{
    /// <summary>
    /// Web会議を取得する抽出条件パラメータを表す
    /// </summary>
    public class WebMeetingsQueryParameter
    {
        #region フィールド
        /// <summary>
        /// Web会議の日付範囲の開始日
        /// </summary>
        private DateTime? m_fromDate;

        #endregion

        #region プロパティ
        /// <summary>
        /// 一意とするID一覧（カンマ区切り）
        /// </summary>
        public string Ids { get; set; }
        /// <summary>
        /// 一意とするID一覧
        /// </summary>
        public IEnumerable<string> IdValues { get => Ids.Split(",").Select(id => id.Trim()); }

        /// <summary>
        /// Web会議の日付範囲の開始日（ISO8601形式の文字列）
        /// </summary>
        public string FromDate
        {
            get => m_fromDate.HasValue ? m_fromDate.Value.Date.ToString("O") : null;
            set
            {
                m_fromDate = null;
                if (!string.IsNullOrEmpty(value))
                {
                    DateTime fromDate;
                    if(DateTime.TryParse(value, out fromDate))
                    {
                        m_fromDate = fromDate;
                    }
                }; 
            }
        }

        /// <summary>
        /// Web会議の日付範囲の終了日
        /// </summary>
        private DateTime? m_toDate;
        /// <summary>
        /// Web会議の日付範囲の終了日（ISO8601形式の文字列）
        /// </summary>
        public string ToDate        {
            get => m_toDate.HasValue ? m_toDate.Value.Date.ToString("O") : null;
            set
            {
                m_toDate = null;
                if (!string.IsNullOrEmpty(value))
                {
                    DateTime toDate;
                    if(DateTime.TryParse(value, out toDate))
                    {
                        m_toDate = toDate;
                    }
                }; 
            }
        }
        /// <summary>
        /// 登録者
        /// </summary>
        public string RegisteredBy { get; set; }
        /// <summary>
        /// 通知先のSlackチャンネル
        /// </summary>
        public string SlackChannelId { get; set; }

        /// <summary>
        /// Id一覧が指定されているか
        /// </summary>
        public bool HasIds {
            get {
                return (!string.IsNullOrEmpty(Ids) && Ids.Split(",").Any());
            }
        }

        /// <summary>
        /// Web会議の日付範囲の開始日が指定されているか
        /// </summary>
        public bool HasFromDate {
            get {
                return (m_fromDate != null && m_fromDate.HasValue);
            }
        }

        /// <summary>
        /// Web会議の日付範囲のUTC開始日(未指定の場合はUNIXエポック)
        /// ※UNIXエポック：1970-01-01 00:00:00.0000000 UTC
        /// </summary>
        public DateTime FromDateUtcValue {
            get {
                return HasFromDate ? m_fromDate.Value.ToUniversalTime().Date : DateTime.UnixEpoch;
            }
        }

        /// <summary>
        /// Web会議の日付範囲の開始日(未指定の場合はUNIXエポック)
        /// ※UNIXエポック：1970-01-01 00:00:00.0000000 UTC
        /// </summary>
        public DateTime FromDateValue
        {
            get
            {
                return HasFromDate ? m_fromDate.Value.Date : DateTime.UnixEpoch;
            }
        }

        /// <summary>
        /// Web会議の日付範囲の終了日が指定されているか
        /// </summary>
        public bool HasToDate {
            get {
                return (m_toDate != null && m_toDate.HasValue);
            }
        }

        /// <summary>
        /// Web会議の日付範囲のUTC開始日(未指定の場合はDateTimeの最大値)
        /// </summary>
        public DateTime ToDateUtcValue {
            get {
                return HasToDate ? m_toDate.Value.ToUniversalTime().Date.AddDays(1).AddMilliseconds(-1) : DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Web会議の日付範囲の開始日(未指定の場合はDateTimeの最大値)
        /// </summary>
        public DateTime ToDateValue
        {
            get
            {
                return HasToDate ? m_toDate.Value.Date.AddDays(1).AddMilliseconds(-1) : DateTime.MaxValue;
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
        
        #endregion

        #region 公開サービス
        /// <summary>
        /// 抽出条件の式ツリーを取得する
        /// </summary>
        /// <returns>AND条件で結合した抽出条件の式ツリー</returns>
        public Expression<Func<WebMeeting, bool>> GetWhereExpression()
        {
            // パラメータに指定された項目をAND条件で結合する。
            Expression<Func<WebMeeting, bool>> expr = PredicateBuilder.New<WebMeeting>(true);
            var original = expr;
            if (this.HasRegisteredBy)
            {
                expr = expr.And(w => w.RegisteredBy == this.RegisteredBy);
            }
            if (this.HasSlackChannelId)
            {
                expr = expr.And(w => w.SlackChannelId == this.SlackChannelId);
            }
            if (this.HasFromDate)
            {
                expr = expr.And(w => this.FromDateValue <= w.StartDateTime);
            }
            if (this.HasToDate)
            {
                expr = expr.And(w => w.StartDateTime <= this.ToDateValue);
            }
            if (this.HasIds)
            {
                expr = expr.And(s => this.IdValues.Contains(s.Id));
            }
            if (expr == original)
            {
                expr = x => true;
            }

            return expr;
        }

        /// <summary>
        /// パラメータに指定された項目を{Key=Value}形式のカンマ区切りで結合する
        /// </summary>
        /// <returns>パラメータに指定された項目を{Key=Value}形式のカンマ区切りで結合した文字列</returns>
        public string ToStringParamaters()
        {
            // パラメータに指定された項目を{Key=Value}形式のカンマ区切りで結合する。
            var stringParamaters = new List<string>();
            if (this.HasRegisteredBy)
            {
                stringParamaters.Add($"[RegisteredBy={this.RegisteredBy}]");
            }
            if (this.HasSlackChannelId)
            {
                stringParamaters.Add($"[SlackChannelId={this.SlackChannelId}]");
            }
            if (this.HasFromDate)
            {
                stringParamaters.Add($"[FromDate={this.FromDate}([FromDateValue={this.FromDateValue}])]");
            }
            if (this.HasToDate)
            {
                stringParamaters.Add($"[ToDate={this.ToDate}([ToDateValue={this.ToDateValue}])]");
            }
            if (this.HasIds)
            {
                stringParamaters.Append($"[Ids={this.Ids}]([IdValues={this.IdValues}])");
            }

            return string.Join(',', stringParamaters);
        }
        #endregion
    }
}