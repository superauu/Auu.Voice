using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using Speech2TextAssistant.Models;

namespace Speech2TextAssistant;

public class ChatGptService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private string _apiKey = null!;
    private string _model = null!;

    public ChatGptService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public void Initialize(string apiKey, string model = "gpt-3.5-turbo")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> ProcessTextAsync(string text, ChatGptPromptType promptType, string customPrompt = "")
    {
        try
        {
            var systemPrompt = promptType == ChatGptPromptType.CustomPrompt && !string.IsNullOrEmpty(customPrompt)
                ? customPrompt
                : PromptTemplates.GetSystemPrompt(promptType);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(responseJson);
                return document.RootElement
                    .GetProperty("choices")
                    .EnumerateArray()
                    .First()
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?.Trim() ?? "";
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API调用失败: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            throw new Exception($"ChatGPT处理失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}