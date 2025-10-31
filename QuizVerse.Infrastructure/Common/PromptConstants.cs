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

    #region Check if Category of the website
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