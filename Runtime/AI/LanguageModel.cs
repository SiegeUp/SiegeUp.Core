#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System.ComponentModel;
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}

namespace SiegeUp.Core.AI
{
    public enum MessageRole { System, User, Model, Tool }
    public enum FinishReason { Stop, Length, ToolCalls, Safety, Recitation, Other, Error }

    public record FunctionCallInfo(
        string Name,
        string Arguments // Serialized JSON arguments
    );

    public record ToolCall(
        string Id,
        FunctionCallInfo Function
    );

    public record ChatMessage
    {
        public MessageRole Role { get; init; }
        public string? Content { get; init; }
        public List<ToolCall>? ToolCalls { get; init; }
        public string? ToolCallId { get; init; }
        public ChatMessage(MessageRole role, string? content = null, List<ToolCall>? toolCalls = null, string? toolCallId = null)
        {
            Role = role;
            Content = content;
            ToolCalls = toolCalls;
            ToolCallId = toolCallId;

            if (role == MessageRole.Tool && string.IsNullOrWhiteSpace(toolCallId))
                throw new ArgumentException("Tool messages must have a ToolCallId.", nameof(toolCallId));
            if (role == MessageRole.Tool && string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Tool messages must have Content (the result).", nameof(content));
            if (role == MessageRole.Model && toolCalls != null && toolCalls.Count > 0 && !string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Model messages with ToolCalls should typically not have text Content.", nameof(content));
        }
    }

    public record FunctionParameterProperty(
        [property: JsonProperty("type")] string Type,
        [property: JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)] string? Description = null,
        [property: JsonProperty("enum", NullValueHandling = NullValueHandling.Ignore)] List<string>? EnumValues = null
    );

    public record FunctionParameters(
        [property: JsonProperty("type")] string Type = "object",
        [property: JsonProperty("properties")] Dictionary<string, FunctionParameterProperty> Properties = null,
        [property: JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)] List<string>? Required = null
    );

    public record FunctionDeclaration(
        string Name,
        string Description,
        FunctionParameters Parameters
    );

    public record Tool(
       FunctionDeclaration Function
    );

    public record GenerationConfig
    {
        public float? Temperature { get; init; }
        public float? TopP { get; init; }
        public int? TopK { get; init; }
        public int? MaxOutputTokens { get; init; }
        public List<string>? StopSequences { get; init; }
        public int? CandidateCount { get; init; }
    }

    public record ResponseChoice
    {
        public ChatMessage Message { get; init; } = new ChatMessage(MessageRole.Model);
        public FinishReason FinishReason { get; init; } = FinishReason.Other;
    }

    public record LanguageModelResponse
    {
        public List<ResponseChoice> Choices { get; init; } = new();
        public string? ErrorMessage { get; init; }
    }

    public abstract class LanguageModel : IDisposable
    {
        protected HttpClient HttpClient { get; }
        private bool _disposed = false;

        protected LanguageModel(HttpClient? httpClient = null)
        {
            HttpClient = httpClient ?? new HttpClient();
        }

        public abstract Task<LanguageModelResponse> GenerateContentAsync(
            IEnumerable<ChatMessage> promptMessages,
            GenerationConfig? config = null,
            IEnumerable<Tool>? tools = null,
            CancellationToken cancellationToken = default
        );

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing) { /* Dispose managed state if needed */ }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    
}