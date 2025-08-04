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
                prompt = $"Return ONLY valid JSON with EXACTLY these fields: " +
                         "Grade (int 1-10), IdentifiedErrors (list of strings), ImprovementSuggestions (list of strings), FurtherRecommendations (list of strings). " +
                         $"Analyze the following educational work:\n\n{content}",
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
                    IdentifiedErrors = new List<string> { "Analysis failed." },
                    ImprovementSuggestions = new List<string> { "Try again later." },
                    FurtherRecommendations = new List<string> { "Please consult a professor or teaching assistant for manual review." }
                };
            }
        }


        private class OllamaResponse
        {
            public string response { get; set; } = string.Empty;
        }
    }
}
