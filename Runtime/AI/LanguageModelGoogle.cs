using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace SiegeUp.Core.AI
{
    public class LanguageModelGoogle : LanguageModel
    {
        private readonly string apiKey;
        private readonly string modelName;
        private readonly JsonSerializerSettings jsonSettings;

        #region Google API DTOs

        private record GoogleSafetySetting(
            string Category,
            string Threshold
        );

        private record GoogleFunctionDeclaration(
            string Name,
            string Description,
            FunctionParameters Parameters
        );

        private record GoogleTool(
            [JsonProperty("functionDeclarations")] List<GoogleFunctionDeclaration> FunctionDeclarations
        );

        private record GoogleGenerationConfig(
            float? Temperature,
            float? TopP,
            int? TopK,
            int? CandidateCount,
            int? MaxOutputTokens,
            [JsonProperty("response_mime_type")] string? ResponseMimeType,
            List<string>? StopSequences
        );

        private record GooglePart
        {
            public string? Text { get; init; }
            public GoogleFunctionCall? FunctionCall { get; init; }
            public GoogleFunctionResponse? FunctionResponse { get; init; }
        }

        private record GoogleFunctionCall(
            string Name,
            JToken Args
        );

        private record GoogleFunctionResponse(
            string Name,
            JToken Response
        );

        private record GoogleContent(
            string Role,
            List<GooglePart> Parts
        );

        private record GoogleGenerateContentRequest(
            List<GoogleContent> Contents,
            List<GoogleTool>? Tools = null,
            GoogleGenerationConfig? GenerationConfig = null
        );

        private record GoogleGenerateContentResponseCandidate(
            GoogleContent Content,
            string FinishReason,
            int Index
        );

        private record GoogleGenerateContentResponse(
            List<GoogleGenerateContentResponseCandidate> Candidates
        );
        #endregion

        public LanguageModelGoogle(string apiKey, string modelName = "gemini-2.0-flash", HttpClient? httpClient = null)
            : base(httpClient)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));
            if (string.IsNullOrWhiteSpace(modelName))
                throw new ArgumentNullException(nameof(modelName));

            this.apiKey = apiKey;
            this.modelName = modelName;

            jsonSettings = new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public override async Task<LanguageModelResponse> GenerateContentAsync(
            IEnumerable<ChatMessage> promptMessages,
            GenerationConfig? config = null,
            IEnumerable<Tool>? tools = null,
            CancellationToken cancellationToken = default)
        {
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            try
            {
                var googleRequest = MapToGoogleRequest(promptMessages, config, tools);
                string jsonRequest = JsonConvert.SerializeObject(googleRequest, jsonSettings);
                using var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = content };
                using var httpResponse = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                string jsonResponse = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return new LanguageModelResponse {
                        ErrorMessage = $"API Error: {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}. Response: {jsonResponse}"
                    };
                }

                var googleResponse = JsonConvert.DeserializeObject<GoogleGenerateContentResponse>(jsonResponse, jsonSettings);
                return MapFromGoogleResponse(googleResponse);

            }
            catch (JsonException ex)
            {
                return new LanguageModelResponse { ErrorMessage = $"JSON Error: {ex.Message}" };
            }
            catch (HttpRequestException ex)
            {
                return new LanguageModelResponse { ErrorMessage = $"HTTP Request Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new LanguageModelResponse { ErrorMessage = $"Unexpected Error: {ex.Message}" };
            }
        }

        private GoogleGenerateContentRequest MapToGoogleRequest(IEnumerable<ChatMessage> messages, GenerationConfig? config, IEnumerable<Tool>? tools)
        {
            var googleContents = new List<GoogleContent>();
            var messageList = messages.ToList();
            if (messageList.Count > 0 && messageList[0].Role == MessageRole.System)
            {
                messageList = messageList.Skip(1).ToList();
            }

            foreach (var msg in messageList)
            {
                string role = msg.Role switch {
                    MessageRole.User => "user",
                    MessageRole.Model => "model",
                    MessageRole.Tool => "user",
                    _ => throw new ArgumentException($"Unsupported message role for Gemini: {msg.Role}")
                };
                var parts = new List<GooglePart>();

                if (msg.Role == MessageRole.Tool)
                {
                    if (string.IsNullOrWhiteSpace(msg.ToolCallId) || string.IsNullOrWhiteSpace(msg.Content))
                        throw new InvalidOperationException("Tool message requires ToolCallId and Content.");

                    JToken resultJson;
                    try
                    {
                        resultJson = JToken.Parse(msg.Content);
                    }
                    catch (JsonReaderException jsonEx)
                    {
                        throw new ArgumentException($"Tool message content for tool '{msg.ToolCallId}' must be valid JSON. Error: {jsonEx.Message}", nameof(messages));
                    }

                    parts.Add(new GooglePart {
                        FunctionResponse = new GoogleFunctionResponse(
                             Name: msg.ToolCallId,
                             Response: new JObject { ["content"] = resultJson }
                         )
                    });
                    role = "user";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(msg.Content))
                    {
                        parts.Add(new GooglePart { Text = msg.Content });
                    }

                    if (msg.Role == MessageRole.Model && msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                    {
                        foreach (var toolCall in msg.ToolCalls)
                        {
                            JToken argsJson;
                            try
                            {
                                argsJson = JToken.Parse(toolCall.Function.Arguments);
                            }
                            catch (JsonReaderException)
                            {
                                argsJson = new JObject();
                            }

                            parts.Add(new GooglePart {
                                FunctionCall = new GoogleFunctionCall(
                                   Name: toolCall.Function.Name,
                                   Args: argsJson
                               )
                            });
                        }
                        role = "model";
                    }
                }

                if (parts.Any())
                {
                    googleContents.Add(new GoogleContent(Role: role, Parts: parts));
                }
            }

            GoogleGenerationConfig? googleConfig = null;
            if (config != null)
            {
                googleConfig = new GoogleGenerationConfig(
                    Temperature: config.Temperature, TopP: config.TopP, TopK: config.TopK,
                    CandidateCount: config.CandidateCount, MaxOutputTokens: config.MaxOutputTokens,
                    ResponseMimeType: config.StructuredResponse ? "application/json" : null,
                    StopSequences: config.StopSequences
                );
            }

            List<GoogleTool>? googleTools = null;
            if (tools != null && tools.Any())
            {
                googleTools = new List<GoogleTool> {
                    new GoogleTool(
                        FunctionDeclarations: tools.Select(t => new GoogleFunctionDeclaration(
                            Name: t.Function.Name, Description: t.Function.Description,
                            Parameters: t.Function.Parameters
                        )).ToList()
                    )
                };
            }

            return new GoogleGenerateContentRequest(
                Contents: googleContents,
                GenerationConfig: googleConfig,
                Tools: googleTools
            );
        }

        private LanguageModelResponse MapFromGoogleResponse(GoogleGenerateContentResponse? googleResponse)
        {
            if (googleResponse?.Candidates == null || !googleResponse.Candidates.Any())
            {
                return new LanguageModelResponse { ErrorMessage = "API returned no candidates." };
            }

            var choices = new List<ResponseChoice>();
            foreach (var candidate in googleResponse.Candidates)
            {
                string? textContent = null;
                List<ToolCall>? toolCalls = null;

                foreach (var part in candidate.Content.Parts)
                {
                    if (part.Text != null) { textContent = (textContent ?? "") + part.Text; }
                    else if (part.FunctionCall != null)
                    {
                        toolCalls ??= new List<ToolCall>();
                        toolCalls.Add(new ToolCall(
                            Id: $"{part.FunctionCall.Name}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                            Function: new FunctionCallInfo(
                                Name: part.FunctionCall.Name,
                                Arguments: part.FunctionCall.Args.ToString(Formatting.None)
                            )
                        ));
                    }
                }

                var message = new ChatMessage(MessageRole.Model, textContent, toolCalls);
                var finishReason = MapFinishReason(candidate.FinishReason, toolCalls);

                choices.Add(new ResponseChoice { Message = message, FinishReason = finishReason });
            }
            return new LanguageModelResponse { Choices = choices };
        }

        private FinishReason MapFinishReason(string? reasonString, List<ToolCall>? parsedToolCalls)
        {
            bool hasToolCalls = parsedToolCalls != null && parsedToolCalls.Count > 0;

            var reason = reasonString switch {
                "STOP" => FinishReason.Stop,
                "MAX_TOKENS" => FinishReason.Length,
                "SAFETY" => FinishReason.Safety,
                "RECITATION" => FinishReason.Recitation,
                _ when hasToolCalls => FinishReason.ToolCalls,
                var fr when fr != null && fr.ToUpperInvariant().Contains("TOOL_CALL") => FinishReason.ToolCalls,
                _ => FinishReason.Other
            };

            return hasToolCalls ? FinishReason.ToolCalls : reason;
        }
    }
}