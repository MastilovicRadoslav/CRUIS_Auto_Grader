using Common.DTOs;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;



namespace Common.Helpers
{
    public static class LlmClient
    {
        private static readonly string apiKey = "";
        private static readonly string groqUrl = "https://api.groq.com/openai/v1/chat/completions";
        private static readonly string model = "meta-llama/llama-4-scout-17b-16e-instruct";

        public static async Task<AnalysisResultDto> AnalyzeAsync(
            string content,
            string additionalInstructions = "",
            AdminAnalysisSettings? settings = null)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var rangeText = settings != null
                ? $"Grade MUST be an int in the range [{settings.MinGrade}-{settings.MaxGrade}]."
                : "Grade MUST be an int in the range [1-10].";

            var methodsText = (settings != null && settings.Methods?.Count > 0)
                ? $"Use the following analysis methods/criteria (adapt as applicable): {string.Join(", ", settings.Methods)}."
                : "Use your default best-practice analysis for the given content.";

            var instr = string.IsNullOrWhiteSpace(additionalInstructions) ? "" : $" Additional instructions: {additionalInstructions}";

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
                model = model,
                messages = new[]
                {
                new { role = "system", content = "You are an educational assistant that analyzes student submissions and gives structured JSON feedback." },
                new { role = "user", content = userContent }
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

            var cleanedAnswer = answer.Trim();
            if (cleanedAnswer.StartsWith("```"))
                cleanedAnswer = cleanedAnswer.Trim('`').Trim();

            try
            {
                var analysis = JsonConvert.DeserializeObject<AnalysisResultDto>(cleanedAnswer);
                if (analysis == null)
                    throw new Exception("JSON parse failed.");

                // Osiguraj range na klijentskoj strani (defenzivno)
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
            catch
            {
                // fallback
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
