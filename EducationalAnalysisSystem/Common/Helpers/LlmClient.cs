using Common.DTOs;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;



namespace Common.Helpers
{
    public static class LlmClient
    {
        private static readonly string apiKey = "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";
        private static readonly string groqUrl = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly string model = "meta-llama/llama-4-scout-17b-16e-instruct";

        public static async Task<AnalysisResultDto> AnalyzeAsync(string content, string additionalInstructions = "")
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are an educational assistant that analyzes student submissions and gives structured JSON feedback." },
                    new
                    {
                        role = "user",
                        content = string.IsNullOrWhiteSpace(additionalInstructions)
                            ? $"Return ONLY valid JSON with EXACTLY these fields: Grade (int 1-10), IdentifiedErrors (list of strings), ImprovementSuggestions (list of strings), FurtherRecommendations (list of strings). Analyze the following educational work:\n\n{content}"
                            : $"Return ONLY valid JSON with EXACTLY these fields: Grade (int 1-10), IdentifiedErrors (list of strings), ImprovementSuggestions (list of strings), FurtherRecommendations (list of strings). Additional instructions: {additionalInstructions}\n\nAnalyze the following educational work:\n\n{content}"
                                }
                            },
                temperature = 0.2
            };

            var response = await httpClient.PostAsJsonAsync(groqUrl, requestBody);

            if (!response.IsSuccessStatusCode)
                throw new Exception("❌ Groq API failed: " + response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            var completion = JsonConvert.DeserializeObject<GroqResponse>(json);
            var answer = completion?.choices?.FirstOrDefault()?.message?.content;

            if (string.IsNullOrWhiteSpace(answer))
                throw new Exception("❌ Odgovor je prazan.");

            // ✅ Očisti Markdown code block ako postoji
            var cleanedAnswer = answer.Trim();
            if (cleanedAnswer.StartsWith("```"))
            {
                cleanedAnswer = cleanedAnswer.Trim('`').Trim(); // uklanja ``` i whitespace
            }

            try
            {
                var analysis = JsonConvert.DeserializeObject<AnalysisResultDto>(cleanedAnswer);

                if (analysis == null || analysis.Grade == 0)
                    throw new Exception("✅ JSON je stigao, ali je struktura nepotpuna.");

                return analysis;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Groq response is not valid JSON:");
                Console.WriteLine(answer);
                Console.WriteLine("Greška: " + ex.Message);

                return new AnalysisResultDto
                {
                    Grade = 0,
                    IdentifiedErrors = new List<string> { "Analysis failed." },
                    ImprovementSuggestions = new List<string> { "Try again later." },
                    FurtherRecommendations = new List<string> { "Please consult a professor or assistant for review." }
                };
            }
        }

        private class GroqResponse
        {
            public List<Choice> choices { get; set; }

            public class Choice
            {
                public Message message { get; set; }
            }

            public class Message
            {
                public string content { get; set; }
            }
        }
    }
}
