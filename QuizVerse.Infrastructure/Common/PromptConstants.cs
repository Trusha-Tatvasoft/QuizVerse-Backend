namespace QuizVerse.Infrastructure.Common;

public static class PromptConstants
{
    #region Check Answer Prompt
    public const string CHECK_ANSWER_PROMPT = @"
        Evaluate the answer for a quiz question.

        Question: {0}

        Expected Answer: {1}
        Provided Answer: {2}

        Instructions:
        - Consider the context of the question.
        - If the provided answer correctly answers the question and conveys the same meaning as the expected answer (even with different wording), return TRUE.
        - If the provided answer is incorrect, incomplete, or unrelated to the question, return FALSE.
        - Respond only with TRUE or FALSE.";
    #endregion

    #region Get Explanation Prompt
    public const string GET_EXPLANATION_PROMPT = @"
        You are an AI quiz evaluator. Evaluate the given answer and generate a clear, concise explanation.

        Question: {0}
        Correct Answer: {1}
        Answer Answer: {2}

        Instructions:
        - Provide a very short and simple explanation (1-2 sentences).
        - Do not use quotes around answers.
        - Do not add extra commentary or greetings.
        - If the answer is correct, explain simply why.
        - If the answer is incorrect or missing, explain the correct answer clearly.
        - Do not include the word 'user' or 'user’s answer'.
        - Respond only with plain text, nothing else.";
    #endregion

    #region Validate Quiz Question Generation Prompt
    public const string VALIDATE_AI_SYSYTEM_INSTRUCTIONS = @"You are a content validator. Analyze if content matches the requested category and is appropriate for quiz generation. Respond ONLY with valid JSON.";

    public const string VALIDATE_GENERATE_QUESTION_PROMPT = @"
        Analyze the following content and determine:
        1. Is it appropriate for educational quiz generation? (no harmful, offensive, or inappropriate content)
        2. Does it match the category: '{0}'?
        3. What is the actual category/topic of this content?

        Content to analyze:
        {1}{2}
        Respond with ONLY this JSON format:
        {{
        ""isValid"": true/false,
        ""isMatch"": true/false,
        ""detectedCategory"": ""actual category here"",
        ""reason"": ""brief explanation""
        }}";
    #endregion

    #region Quiz Question Generation Prompt
    public const string GENERATE_AI_SYATEM_INSTRUCTIONS = @"You are a quiz generator. Return ONLY a JSON array of quiz questions. Do not wrap in any object. Start with [ and end with ].";
    public const string DIFFICULTY_GUIDLINES = @"
        DIFFICULTY GUIDELINES:
        EASY: Straightforward questions testing basic recall and understanding. Simple, direct language.
        MEDIUM: Questions requiring understanding and application of concepts. Moderate complexity.
        HARD: Complex questions testing deep understanding, analysis, and critical thinking. May include edge cases.";
    public const string QUIZ_QUESTION_GENERATION_PROMPT = @"
        You are a professional quiz generator AI. Based on the following content, generate EXACTLY {0} quiz questions with the following specifications:
        {1}
        {2}

        IMPORTANT RULES:
        1. Generate exactly {0} questions in total.
        2. Each question MUST follow this JSON format exactly.
        3. Do not include any text outside the JSON array.
        4. Ensure queTypeName and queDifficultyName match the specification exactly.
        5. Use 'key': 'option' for answer choices and 'key': 'answer' for the correct answer. Only one correct answer per question.

        OUTPUT FORMAT EXAMPLES:

        Multiple Choice (mcq):
        {{
            ""queText"": ""What is the chemical symbol for water?"",
            ""queTypeName"": ""Multiple Choice"",
            ""queDifficultyName"": ""Easy"",
            ""queOptionsAns"": [
                {{ ""key"": ""option"", ""value"": ""O2"" }},
                {{ ""key"": ""option"", ""value"": ""H2O"" }},
                {{ ""key"": ""option"", ""value"": ""CO2"" }},
                {{ ""key"": ""option"", ""value"": ""HO2"" }},
                {{ ""key"": ""answer"", ""value"": ""H2O"" }}
            ]
        }}

        Fill in the Blank (fill):
        {{
            ""queText"": ""The capital of France is ____."",
            ""queTypeName"": ""Fill in the Blank"",
            ""queDifficultyName"": ""Medium"",
            ""queOptionsAns"": [
                {{ ""key"": ""answer"", ""value"": ""Paris"" }}
            ]
        }}

        True/False (truefalse):
        {{
            ""queText"": ""The Earth is flat."",
            ""queTypeName"": ""True/False"",
            ""queDifficultyName"": ""Medium"",
            ""queOptionsAns"": [
                {{ ""key"": ""answer"", ""value"": ""False"" }}
            ]
        }}

        Short Answer (short):
        {{
            ""queText"": ""Explain the concept in 2-3 sentences."",
            ""queTypeName"": ""Short Answer"",
            ""queDifficultyName"": ""Medium"",
            ""queOptionsAns"": [
                {{ ""key"": ""answer"", ""value"": ""Expected answer here"" }}
            ]
        }}

        Content to create questions from:{3}
        Now generate the {0} questions as a JSON array following the specifications exactly:";
    #endregion // 0:- totalQuestions, 1:- specificationsText, 2:- difficultyGuidelines, 3:- inputText

    #region Check Category of the website
    public const string CHECK_WEBSITE_CATEGORY_PROMPT = @"You are a website safety classifier. Given the input website URL: {0}, 
        determine if it falls into any of the following unsafe categories based on its domain name, content, or purpose:
        - Adult entertainment or explicit material
        - Online gambling, betting, or casino-related content
        - Extremist, radical, or hate-promoting content
        - Torrent, piracy, or illegal software download platforms
        - Darknet marketplaces or illicit goods trading
        - Scams, phishing, or fraudulent websites
        - Malware or virus distribution sites

        Return a single sentence stating 'The website is unsafe due to [category].' if it matches any unsafe category, 
        otherwise return 'The website is safe.' Do not include additional text or explanation.";
    #endregion
}