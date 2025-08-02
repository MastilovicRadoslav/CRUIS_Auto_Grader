using Common.DTOs;
using Newtonsoft.Json;

namespace WebApi.Helpers
{
    public static class LlmClient
    {
        public static async Task<AnalysisResultDto> AnalyzeAsync(string content)
        {
            using var httpClient = new HttpClient();

            var request = new
            {
                model = "gemma:2b",
                prompt = $"Analyze the following educational work and return JSON with fields: Grade (1-10), Issues (list), Suggestions (list), Summary (text):\n\n{content}",
                stream = false
            };

            var response = await httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", request);

            if (!response.IsSuccessStatusCode)
                throw new Exception("LLM analysis failed.");

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();

            // 🧠 Parsiramo response kao JSON u AnalysisResultDto
            return JsonConvert.DeserializeObject<AnalysisResultDto>(result.response);
        }

        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
