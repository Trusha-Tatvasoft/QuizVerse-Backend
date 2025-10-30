using QuizVerse.Infrastructure.Enums;

namespace QuizVerse.Infrastructure.Common.Helper;

public static class AiModelNameExtensions
{
    public static string ToModelString(this AiModelName model) => model switch
    {
        AiModelName.Llama3Point18BInstant => Constants.LLAMA_3_1_8B_INSTANT,
        AiModelName.Llama3Point370BVersatile => Constants.LLAMA_3_3_70B_VERSATILE,
        AiModelName.GroqCompound => Constants.GROQ_COMPOUND,
        AiModelName.MoonshotAiKimiK2Instruct => Constants.MOONSHOTAI_KIMI_K2_INSTRUCT,
        AiModelName.OpenAiGptOss20B => Constants.OPENAI_GPT_OSS_20B,

        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };
}
