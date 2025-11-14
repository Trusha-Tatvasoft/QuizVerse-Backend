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
        AiModelName.Gemini2Point5FlashLite => Constants.GEMINI_2_POINT_5_FLASH_LITE,
        AiModelName.Gemini2Point5Flash => Constants.GEMINI_2_POINT_5_FLASH,
        AiModelName.Gemini2Point0FlashLite => Constants.GEMINI_2_POINT_0_FLASH_LITE,
        AiModelName.Gemini2Point0Flash => Constants.GEMINI_2_POINT_0_FLASH,
        AiModelName.Gemini2Point5Pro => Constants.GEMINI_2_POINT_5_PRO,
        AiModelName.Gemini2Point0FlashExp => Constants.GEMINI_2_POINT_0_FLASH_EXP,

        _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
    };
}
