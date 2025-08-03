using Common.DTOs;
using Newtonsoft.Json;
using System.Net.Http.Json;



namespace Common.Helpers
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
                throw new Exception("LLM analysis failed with status: " + response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();

            if (string.IsNullOrWhiteSpace(result?.response))
                throw new Exception("LLM response is empty or null.");

            try
            {
                var analysis = JsonConvert.DeserializeObject<AnalysisResultDto>(result.response);

                if (analysis == null || analysis.Grade == 0)
                    throw new Exception("Deserialization succeeded but result is incomplete or invalid.");

                return analysis;
            }
            catch (Exception ex)
            {
                // Loguj sirovi odgovor
                Console.WriteLine("❌ LLM returned invalid or incomplete JSON:");
                Console.WriteLine(result.response);
                Console.WriteLine("Error: " + ex.Message);

                // Vrati fallback rezultat
                return new AnalysisResultDto
                {
                    Grade = 0,
                    Issues = new List<string> { "LLM response could not be parsed." },
                    Suggestions = new List<string> { "Please review manually." },
                    Summary = "LLM returned unexpected or malformed output."
                };
            }
        }


        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
