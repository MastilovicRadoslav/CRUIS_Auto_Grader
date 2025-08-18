using Common.DTOs;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebApi.Helpers
{
    public static class LlmClient
    {
        private static readonly string apiKey = "";
        private static readonly string model = "gemini-1.5-flash-latest";
        private static readonly string geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        public static async Task<AnalysisResultDto> AnalyzeAsync(
            string content,
            string additionalInstructions = "",
            AdminAnalysisSettings? settings = null)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var rangeText = settings != null
                ? $"Grade MUST be an int in the range [{settings.MinGrade}-{settings.MaxGrade}]."
                : "Grade MUST be an int in the range [1-10].";

            var methodsText = (settings != null && settings.Methods?.Count > 0)
                ? $"Use the following analysis methods/criteria (adapt as applicable): {string.Join(", ", settings.Methods)}."
                : "Use your default best-practice analysis for the given content.";

            var instr = string.IsNullOrWhiteSpace(additionalInstructions)
                ? ""
                : $" Additional instructions: {additionalInstructions}";

            var userContent = $@"
            Respond ONLY with valid JSON.
            DO NOT include any text before or after the JSON.
            DO NOT wrap in code fences.
            The JSON MUST contain EXACTLY these fields:
            - Grade (int)
            - IdentifiedErrors (list of strings)
            - ImprovementSuggestions (list of strings)
            - FurtherRecommendations (list of strings)
            {rangeText}
            {methodsText}{instr}

            Analyze the following educational work:
            {content}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userContent }
                        }
                    }
                }
            };

            using var response = await httpClient.PostAsJsonAsync($"{geminiUrl}?key={apiKey}", requestBody);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("❌ Gemini API failed: " + raw);

            try
            {
                // 1. Parsiramo root odgovor
                var parsed = JsonConvert.DeserializeObject<GeminiResponse>(raw);

                // 2. Vadimo text iz parts[0].text
                var innerJson = parsed?.candidates?.FirstOrDefault()
                    ?.content?.parts?.FirstOrDefault()?.text;

                if (string.IsNullOrWhiteSpace(innerJson))
                    throw new Exception("❌ Gemini vratio prazan sadržaj.");

                // 3. Deserijalizujemo u našu klasu
                var analysis = JsonConvert.DeserializeObject<AnalysisResultDto>(innerJson);

                if (analysis == null)
                    throw new Exception("❌ Neuspjela deserijalizacija AnalysisResultDto.");

                // 4. Defenzivna kontrola ocjene
                if (settings != null)
                {
                    analysis.Grade = Math.Min(settings.MaxGrade, Math.Max(settings.MinGrade, analysis.Grade));
                }
                else
                {
                    analysis.Grade = Math.Min(10, Math.Max(1, analysis.Grade));
                }

                return analysis;
            }
            catch (Exception ex)
            {
                return new AnalysisResultDto
                {
                    Grade = 0,
                    IdentifiedErrors = new List<string> { "Analysis failed: " + ex.Message },
                    ImprovementSuggestions = new List<string> { "Try again later." },
                    FurtherRecommendations = new List<string> { "Please consult a professor or assistant for review." }
                };
            }
        }

        // Model za mapiranje root Gemini odgovora
        private class GeminiResponse
        {
            public List<Candidate> candidates { get; set; }

            public class Candidate
            {
                public Content content { get; set; }
            }

            public class Content
            {
                public List<Part> parts { get; set; }
            }

            public class Part
            {
                public string text { get; set; }
            }
        }
    }
}
