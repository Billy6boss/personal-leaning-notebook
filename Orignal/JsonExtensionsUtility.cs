using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Nine1.Utility.Middleware.Common.Utils;

public static class JsonExtensions
{
    /// <summary>
    /// 預設的JsonSerializerOptions
    /// </summary>
    public static readonly JsonSerializerOptions DefaultSerializerOptions = CreateDefaultSerializerOptions();

    /// <summary>
    /// 建立預設的 JsonSerializerOptions，並確保 Converters 設定完成後才回傳，
    /// 避免其他執行緒透過 DefaultSerializerOptions 讀到尚未完全組裝好的實例。
    /// </summary>
    private static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    /// <summary>
    /// 轉換成Mall2的JsonFormat
    /// </summary>
    /// <param name="self">要轉換的物件</param>
    /// <returns>json string</returns>
    public static string ToJson(
        this object self)
    {
        var result = JsonSerializer.Serialize(
            self,
            DefaultSerializerOptions);

        return result;
    }

    /// <summary>
    /// 要轉換成物件的字串
    /// </summary>
    /// <param name="self">要轉換的物件</param>
    /// <typeparam name="T">目標Type</typeparam>
    /// <returns>轉換結果</returns>
    public static T? ParseToType<T>(
        this string self)
    {
        var result = JsonSerializer.Deserialize<T>(
            self,
            DefaultSerializerOptions);

        return result;
    }
}