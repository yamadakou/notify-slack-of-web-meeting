using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace dcinc.json.helpers
{
    public class JsonSerializerHelper
    {
        /// <summary>
        /// JsonSerializer で使用されるオプションを取得します。
        /// </summary>
        /// <returns>JsonSerializer で使用されるオプションを返します。</returns>
        /// <remarks
        /// 参考：https://learn.microsoft.com/ja-jp/dotnet/api/system.text.json.jsonserializeroptions?view=net-8.0
        /// </remarks>
        public static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                // キーの名前を、camel 形式に変換する
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // プロパティの名前が大文字と小文字を区別しない
                PropertyNameCaseInsensitive = true,
                // コメントと末尾のコンマを許可する
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                // 引用符で囲まれた数値を許可または記述する（Webの規定値と同様）
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                WriteIndented = true
            };
        }
    }
}
