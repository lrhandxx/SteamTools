﻿using MessagePack;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using static Newtonsoft.Json.JsonConvert;
using static System.Serializable;
using SJsonSerializer = System.Text.Json.JsonSerializer;
using SJsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace System
{
    /// <summary>
    /// 序列化、反序列化，模型类可继承此类
    /// </summary>
    [Serializable]
    public abstract partial class Serializable
    {
        /// <summary>
        /// 序列化程式实现种类
        /// </summary>
        public enum ImplType
        {
            /// <summary>
            /// Newtonsoft.Json
            /// <para>https://github.com/JamesNK/Newtonsoft.Json</para>
            /// </summary>
            NewtonsoftJson,

            /// <summary>
            /// MessagePack is an efficient binary serialization format. It lets you exchange data among multiple languages like JSON. But it's faster and smaller. Small integers are encoded into a single byte, and typical short strings require only one extra byte in addition to the strings themselves.
            /// <para>https://msgpack.org/</para>
            /// <para>https://github.com/neuecc/MessagePack-CSharp</para>
            /// </summary>
            MessagePack,

            /// <summary>
            /// System.Text.Json
            /// <para>仅用于 .Net Core 3+ / Web，因 Emoji 字符被转义</para>
            /// <para>https://github.com/dotnet/corefx/tree/v3.1.5/src/System.Text.Json</para>
            /// <para>https://github.com/dotnet/runtime/tree/v5.0.0-preview.6.20305.6/src/libraries/System.Text.Json</para>
            /// </summary>
            SystemTextJson,
        }

        /// <summary>
        /// JSON 序列化程式实现种类
        /// </summary>
        public enum JsonImplType
        {
            /// <inheritdoc cref="ImplType.NewtonsoftJson"/>
            NewtonsoftJson = ImplType.NewtonsoftJson,

            /// <inheritdoc cref="ImplType.SystemTextJson"/>
            SystemTextJson = ImplType.SystemTextJson,
        }

        #region DefaultJsonImplType

        /// <summary>
        /// JSON 序列化程式 实现，可设置使用 Newtonsoft.Json 或 System.Text.Json
        /// </summary>
        public static JsonImplType DefaultJsonImplType { get; set; } = GetDefaultJsonImplType();

        static JsonImplType GetDefaultJsonImplType()
        {
            if (DI.Platform == Platform.Android ||
                (DI.Platform == Platform.Apple && DI.DeviceIdiom != DeviceIdiom.Desktop))
            {
                return JsonImplType.NewtonsoftJson;
            }
            return JsonImplType.SystemTextJson;
        }

        #endregion

        #region Serialize(序列化)

        /// <summary>
        /// (Serialize)JSON 序列化
        /// </summary>
        /// <param name="implType"></param>
        /// <param name="value"></param>
        /// <param name="inputType"></param>
        /// <param name="writeIndented"></param>
        /// <param name="ignoreNullValues"></param>
        /// <returns></returns>
        public static string SJSON(JsonImplType implType, object? value, Type? inputType = null, bool writeIndented = false, bool ignoreNullValues = false)
        {
            switch (implType)
            {
                case JsonImplType.SystemTextJson:
                    var options = new SJsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = writeIndented,
                        IgnoreNullValues = ignoreNullValues
                    };
                    return SJsonSerializer.Serialize(value, inputType ?? value?.GetType() ?? typeof(object), options);
                default:
                    var formatting = writeIndented ? Formatting.Indented : Formatting.None;
                    var settings = ignoreNullValues ? new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    } : null;
                    return SerializeObject(value, inputType, formatting, settings);
            }
        }

        /// <summary>
        /// (Serialize)JSON 序列化
        /// </summary>
        /// <param name="value"></param>
        /// <param name="inputType"></param>
        /// <param name="writeIndented"></param>
        /// <returns></returns>
        public static string SJSON(object? value, Type? inputType = null, bool writeIndented = false, bool ignoreNullValues = false)
            => SJSON(DefaultJsonImplType, value, inputType, writeIndented, ignoreNullValues);

        /// <summary>
        /// (Serialize)MessagePack 序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] SMP<T>(T value, CancellationToken cancellationToken = default)
            => MessagePackSerializer.Serialize(value, cancellationToken: cancellationToken);

        /// <summary>
        /// (Serialize)MessagePack 序列化 + Base64Url Encode
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static string? SMPB64U<T>(T value, CancellationToken cancellationToken = default)
        {
            if (value == null) return null;
            var byteArray = SMP(value, cancellationToken);
            return byteArray.Base64UrlEncode_Nullable();
        }

        #endregion

        #region Deserialize(反序列化)

        /// <summary>
        /// (Deserialize)JSON 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="implType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [return: MaybeNull]
        public static T DJSON<T>(JsonImplType implType, string value)
        {
            return implType switch
            {
                JsonImplType.SystemTextJson => SJsonSerializer.Deserialize<T>(value),
                _ => DeserializeObject<T>(value),
            };
        }

        /// <summary>
        /// (Deserialize)JSON 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [return: MaybeNull]
        public static T DJSON<T>(string value) => DJSON<T>(DefaultJsonImplType, value);

        /// <summary>
        /// (Deserialize)MessagePack 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="buffer"></param>
        /// <returns></returns>
        [return: MaybeNull]
        public static T DMP<T>(byte[] buffer, CancellationToken cancellationToken = default)
            => MessagePackSerializer.Deserialize<T>(buffer, cancellationToken: cancellationToken);

        /// <summary>
        /// (Deserialize)MessagePack 反序列化 + Base64Url Decode
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [return: MaybeNull]
        public static T DMPB64U<T>(string? value, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                try
                {
                    var buffer = value.Base64UrlDecodeToByteArray();
                    return DMP<T>(buffer, cancellationToken);
                }
                catch
                {
                }
            }
            return default;
        }

        #endregion

        /// <summary>
        /// 使用 MessagePack 序列化将对象克隆一份新的对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T Clone<T>(T obj)
        {
            var bytes = MessagePackSerializer.Serialize(obj);
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }

    public static class SerializableExtensions
    {
        /// <inheritdoc cref="Serializable.Clone{T}(T)"/>
        public static T Clone<T>(this T obj) where T : Serializable => Serializable.Clone(obj);

        /// <summary>
        /// 将 [序列化程式实现种类] 转换为 [JSON 序列化程式实现种类]
        /// </summary>
        /// <param name="enum"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryConvert(this ImplType @enum, out JsonImplType value)
        {
            switch (@enum)
            {
                case ImplType.NewtonsoftJson:
                    value = JsonImplType.NewtonsoftJson;
                    return true;

                case ImplType.SystemTextJson:
                    value = JsonImplType.SystemTextJson;
                    return true;

                default:
                    value = default;
                    return false;
            }
        }

        /// <summary>
        /// 将 [JSON 序列化程式实现种类] 转换为 [序列化程式实现种类]
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ImplType Convert(this JsonImplType value)
        {
            return value switch
            {
                JsonImplType.SystemTextJson => ImplType.SystemTextJson,
                _ => ImplType.NewtonsoftJson,
            };
        }
    }
}