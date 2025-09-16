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
}