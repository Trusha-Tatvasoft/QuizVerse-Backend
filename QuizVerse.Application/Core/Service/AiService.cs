using OllamaSharp;
using Microsoft.Extensions.Options;
using System.Text;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.DTOs;
using AngleSharp.Dom;
using QuizVerse.Infrastructure.Common;

namespace QuizVerse.Application.Core.Service
{
    public class AiService : IAiService
    {
        private readonly OllamaApiClient _aiClient;

        public AiService(IOptions<AiServiceOptions> options)
        {
            AiServiceOptions opts = options?.Value ?? throw new AppException(Constants.OPTIONS_NOT_CONFIGURED);

            if (string.IsNullOrWhiteSpace(opts.BaseUrl))
                throw new AppException(Constants.BASE_URL_CANNOT_BE_EMPTY);
            if (string.IsNullOrWhiteSpace(opts.Model))
                throw new AppException(Constants.MODEL_CANNOT_BE_EMPTY);

            _aiClient = new OllamaApiClient(new Url(opts.BaseUrl))
            {
                SelectedModel = opts.Model
            };
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new AppException(Constants.PROMPT_CANNOT_BE_EMPTY);

            StringBuilder responseBuilder = new();

            await foreach (var stream in _aiClient.GenerateAsync(prompt))
            {
                if (!string.IsNullOrWhiteSpace(stream?.Response))
                    responseBuilder.Append(stream.Response);
            }

            return responseBuilder.ToString();
        }
    }
}
