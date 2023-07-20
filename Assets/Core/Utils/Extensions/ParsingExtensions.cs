using System;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace Core.Utils.Extensions
{
    public static class ParsingExtensions
    {
        private static readonly JsonSerializerSettings JsonSettings = new() { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented };
        
        [NotNull]
        public static string CompactFormat(this Resolution resolution) 
            => $"{resolution.width.ToString()}x{resolution.height.ToString()}@{resolution.refreshRate.ToString()}";

        public static Result<Resolution> ParseResolution(this string resolutionString)
        {
            if (string.IsNullOrEmpty(resolutionString))
                return Result<Resolution>.Error(new Exception("Resolution string is null or empty"));

            resolutionString = resolutionString.Trim();

            ReadOnlySpan<char> resolutionSpan = resolutionString.AsSpan();
            int xIndex = resolutionSpan.IndexOf(value: 'x');
            int arrobaIndex = resolutionSpan.IndexOf(value: '@');
            if (xIndex == -1 || arrobaIndex == -1)
                return Result<Resolution>.Error(new Exception($"Resolution string is not in the correct format: {resolutionString}"));
            
            ReadOnlySpan<char> widthSpan = resolutionSpan[..xIndex];
            Option<int> width = widthSpan.ParseInt();
            if (width.IsNone)
                return Result<Resolution>.Error(new Exception($"Could not parse width from resolution string: {resolutionString}"));
            
            ReadOnlySpan<char> heightSpan = resolutionSpan[(xIndex + 1)..arrobaIndex];
            Option<int> height = heightSpan.ParseInt();
            if (height.IsNone)
                return Result<Resolution>.Error(new Exception($"Could not parse height from resolution string: {resolutionString}"));
            
            ReadOnlySpan<char> refreshRateSpan = resolutionSpan[(arrobaIndex + 1)..];
            Option<int> refreshRate = refreshRateSpan.ParseInt();
            if (refreshRate.IsNone)
                return Result<Resolution>.Error(new Exception($"Could not parse refresh rate from resolution string: {resolutionString}"));

            return new Resolution
            {
                width = width.Value,
                height = height.Value,
                refreshRate = refreshRate.Value
            };
        }

        /*public static Result<T> ParseJsonClass<T>([NotNull] string jsonData, bool log)
        {
            if (string.IsNullOrEmpty(value: jsonData))
                return Result<T>.Error("Json data is null or empty");
            
            JsonSchemaGeneratorSettings settings = new() { AlwaysAllowAdditionalObjectProperties = true };
            JsonSchema schema = JsonSchema.FromType<T>(settings);
            schema.AllowAdditionalProperties = true;
            try
            {
                ICollection<ValidationError> errors = schema.Validate(jsonData);
                if (errors?.Count > 0 && log)
                    foreach (ValidationError error in errors)
                        Debug.Log($"Property: {error.Property} | Kind: {error.Kind}");
            }
            catch (JsonReaderException readerException)
            {
                Debug.Log($"Failed to read Json: {readerException.Path}");
                return Result<T>.Error(readerException);
            }
            
            return JsonConvert.DeserializeObject<T>(value: jsonData);
        }*/

        public static Option<string> CustomSerialize<T>(this T source, bool logErrors)
        {
            try
            {
                return Option<string>.Some(JsonConvert.SerializeObject(source, JsonSettings));
            }
            catch (Exception exception)
            {
                if (logErrors)
                    Debug.LogWarning($"Serialization failure: \n{exception}");
                
                return Option.None;
            }
        }

        public static Option<object> CustomDeserialize(this string source, bool logErrors)
        {
            try
            {
                return Option<object>.Some(JsonConvert.DeserializeObject(source, JsonSettings));
            }
            catch (Exception exception)
            {
                if (logErrors)
                    Debug.LogWarning($"Deserialization failure: \n{exception}");
                
                return Option.None;
            }
        }

        [NotNull]
        public static System.Random DeepClone(this System.Random source)
        {
            Option<string> serialized = source.CustomSerialize(logErrors: true);
            Option<object> deserialized = serialized.Value.CustomDeserialize(logErrors: true);
            if (deserialized.Value is System.Random random)
                return random;
            
            Debug.LogWarning($"Failed to deep clone random: {source}");
            return new System.Random();
        }

        [Pure] public static Option<int>ParseInt(this ReadOnlySpan<char> span) => int.TryParse(s: span, result: out int result) ? result : Option<int>.None;
        [Pure] public static Option<int> ParseInt(this string text) => int.TryParse(text, out var integer) ? Option<int>.Some(integer) : Option<int>.None;
        [Pure] public static Option<float> ParseFloat(this ReadOnlySpan<char> span) => float.TryParse(s: span, result: out float result) ? result : Option<float>.None;
        [Pure] public static Option<float> ParseFloat(this string text) => float.TryParse(text, out var floating) ? Option<float>.Some(floating) : Option<float>.None;
        [Pure] public static Option<T> ParseEnum<T>(this string text) where T : struct, Enum => Enum.TryParse(text, out T result) ? Option<T>.Some(result) : Option<T>.None;
        
        [Pure] public static Option<Guid> ParseGuid(this ReadOnlySpan<char> span) => Guid.TryParse(span, out var guid) ? Option<Guid>.Some(guid) : Option<Guid>.None;
        [Pure] public static Option<Guid> ParseGuid(this string text) => Guid.TryParse(text, out var guid) ? Option<Guid>.Some(guid) : Option<Guid>.None;
        [Pure] public static Option<bool> ParseBool(this ReadOnlySpan<char> span) => bool.TryParse(span, out var boolean) ? Option<bool>.Some(boolean) : Option<bool>.None;
        [Pure] public static Option<bool> ParseBool(this string text) => bool.TryParse(text, out var boolean) ? Option<bool>.Some(boolean) : Option<bool>.None;
    }
}