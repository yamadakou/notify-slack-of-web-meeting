using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace dcinc.json.converters
{
    /// <summary>
    /// Unix エポック時間を使用するJSONコンバーター
    /// </summary>
    /// <remarks>
    /// 参考：https://learn.microsoft.com/en-us/dotnet/standard/datetime/system-text-json-support#use-unix-epoch-date-format
    /// </remarks>
    sealed class UnixEpochDateTimeConverter : JsonConverter<DateTime>
    {
        //static readonly DateTime s_epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        //static readonly Regex s_regex = new Regex("^/Date\\(([+-]*\\d+)\\)/$", RegexOptions.CultureInvariant);

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string formatted = reader.GetString()!;
            long unixTime = formatted.FromJsonFormat();
            //Match match = s_regex.Match(formatted);

            //if (
            //        !match.Success
            //        || !long.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime))
            //{
            //    throw new JsonException();
            //}


            // Unix エポック時間は UTC 時刻における1970年1月1日午前0時0分0秒（Unix Xエポック）からの経過秒数であり、秒単位で追加する。
            return DateTime.UnixEpoch.AddMicroseconds(unixTime);
            //return s_epoch.AddMilliseconds(unixTime);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            //long unixTime = Convert.ToInt64((value - s_epoch).TotalMilliseconds);

            // Cosm
            //string formatted = string.Create(CultureInfo.InvariantCulture, $"/Date({unixTime})/");
            string formatted = value.ToJsonFormat();
            writer.WriteStringValue(formatted);
        }
    }

    /// <summary>
    /// Unix エポック時間を使用する拡張メソッド
    /// </summary>
    public static class UnixEpochDateTimeExtensions
    {
        private static readonly Regex s_regex = new Regex("^/Date\\(([+-]*\\d+)\\)/$", RegexOptions.CultureInvariant);

        /// <summary>
        /// 日時をUnix エポック時間形式にします。
        /// </summary>
        /// <param name="dateTime">日時</param>
        /// <returns>Unix エポック時間</returns>
        public static long ToUnixTime(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 日時をUnix エポック時間のJSON形式にします。
        /// </summary>
        /// <param name="dateTime">日時</param>
        /// <returns>日時をUnix エポック時間のJSON形式にした文字列</returns>
        public static string ToJsonFormat(this DateTime dateTime)
        {
            return dateTime.ToUnixTime().ToJsonFormat();
        }

        /// <summary>
        /// Unix エポック時間をJSON形式にします。
        /// </summary>
        /// <param name="unixTime">Unix エポック</param>
        /// <returns>Unix エポック時間をJSON形式にした文字列</returns>
        public static string ToJsonFormat(this long unixTime)
        {
            return string.Create(CultureInfo.InvariantCulture, $"/Date({unixTime})/");
        }

        /// <summary>
        /// Unix エポック時間のJSON形式文字列からUnix エポック時間にします。
        /// </summary>
        /// <param name="unixTimeJsonString"></param>
        /// <returns>Unix エポック時間</returns>
        /// <exception cref="JsonException">無効な JSON テキストが検出された場合、定義された最大深度が渡された場合、または JSON テキストがオブジェクトのプロパティの型と互換性がない場合にスロー</exception>
        public static long FromJsonFormat(this string unixTimeJsonString)
        {
            Match match = s_regex.Match(unixTimeJsonString);

            if (
                    !match.Success
                    || !long.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out long unixTime))
            {
                throw new JsonException();
            }

            return unixTime;
        }
    }
}
