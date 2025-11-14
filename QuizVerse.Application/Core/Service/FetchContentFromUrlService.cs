using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.Common.Exceptions;
using QuizVerse.Infrastructure.DTOs;

namespace QuizVerse.Application.Core.Service;

public class FetchContentFromUrlService(IHttpClientFactory httpClientFactory, IGeminiWebsiteSafetyClient _gemini) : IFetchContentFromUrlService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<string> FetchAndValidateAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new AppException(Constants.INVALID_URL_PROVIDED, StatusCodes.Status400BadRequest);
        }

        try
        {
            var reputation = await AnalyzeAsync(url);
            if (reputation.IsUnsafe)
            {
                var categories = string.Join(", ", reputation.Categories);
                throw new AppException(string.Format(Constants.URL_BLOCKED_SAFETY_CONCERNS, categories), StatusCodes.Status403Forbidden);
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            // Clean the HTML content
            var cleanText = CleanHtmlContent(html);

            return (cleanText);
        }
        catch (HttpRequestException ex)
        {
            throw new AppException(string.Format(Constants.HTTP_ERROR_FETCHING_URL, url), StatusCodes.Status403Forbidden);
        }
        catch (Exception ex)
        {
            throw new AppException(string.Format(Constants.UNEXPECTED_ERROR_FETCHING_URL, url, ex.Message), StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<UrlReputationResult> AnalyzeAsync(string url)
    {
        var result = new UrlReputationResult();

        // Validate URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new AppException(Constants.INVALID_URL_PROVIDED, StatusCodes.Status400BadRequest);
        }

        bool isGeminiUnsafe = false;
        string geminiMessage = string.Empty;
        bool geminiCheckSucceeded = false;

        try
        {
            var geminiResult = await CheckGeminiAsync(url);
            if (geminiResult.Success)
            {
                isGeminiUnsafe = geminiResult.IsUnsafe;
                geminiMessage = geminiResult.Message;
                geminiCheckSucceeded = true;
            }
        }
        catch (Exception ex)
        {
            throw new AppException(string.Format(Constants.ERROR_ANALYZING_URL_REPUTATION, url, ex.Message), StatusCodes.Status500InternalServerError);
        }

        // Ensure at least one check succeeded
        if (!geminiCheckSucceeded)
        {
            throw new AppException(string.Format(Constants.ALL_URL_SAFETY_CHECKS_FAILED, url), StatusCodes.Status503ServiceUnavailable);
        }

        if (geminiCheckSucceeded && isGeminiUnsafe)
        {
            result.IsUnsafe = true;
            result.Categories.Add(geminiMessage ?? Constants.UNSAFE_GEMINI_ANALYSIS);
        }

        return result;
    }

    private async Task<ServiceCheckResult> CheckGeminiAsync(string url)
    {
        try
        {
            var (isUnsafe, message) = await _gemini.IsUnsafeAsync(url);
            return new ServiceCheckResult
            {
                Success = true,
                IsUnsafe = isUnsafe,
                Message = message
            };
        }
        catch (Exception ex)
        {
            return new ServiceCheckResult
            {
                Success = false,
                IsUnsafe = false,
                Message = string.Format(Constants.GEMINI_CHECK_FAILED, ex.Message)
            };
        }
    }

    private string CleanHtmlContent(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove HTML comments first
        RemoveAllComments(doc);

        // Remove unwanted tags
        RemoveUnwantedTags(doc);

        // Extract and clean text
        var text = ExtractCleanText(doc);

        return text;
    }

    private void RemoveAllComments(HtmlDocument doc)
    {
        // Remove all comment nodes
        var comments = doc.DocumentNode.SelectNodes(Constants.COMMENT_FORMATE);
        if (comments != null)
        {
            foreach (var comment in comments)
            {
                comment.Remove();
            }
        }
    }

    private void RemoveUnwantedTags(HtmlDocument doc)
    {
        foreach (var tag in Constants.UnwantedTags)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    node.Remove();
                }
            }
        }

        // Also remove empty elements
        var emptyElements = doc.DocumentNode.Descendants()
            .Where(n => string.IsNullOrWhiteSpace(n.InnerText) && !n.HasChildNodes)
            .ToList();

        foreach (var element in emptyElements)
        {
            element.Remove();
        }
    }

    private string ExtractCleanText(HtmlDocument doc)
    {
        // Get text from the document
        var text = doc.DocumentNode.InnerText;

        // Clean the text
        text = CleanText(text);

        return text;
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove HTML entities and decode
        text = WebUtility.HtmlDecode(text);
        text = Uri.UnescapeDataString(text);

        // Remove emojis and special characters
        text = RemoveEmojis(text);

        // Remove extra whitespace
        text = Constants.MultipleWhitespace.Replace(text, " ").Trim();

        // Remove common unwanted patterns
        text = RemoveUnwantedPatterns(text);

        // Trim and limit length
        text = text.Trim();
        text = text.Substring(0, Math.Min(text.Length, Constants.WORD_LIMIT_TO_SEND_IN_PROMPT)).Trim();

        return text;
    }

    private string RemoveEmojis(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Step-by-step removal – each constant is a compiled Regex
        text = Constants.EmojiSymbolRanges.Replace(text, " ");
        text = Constants.EmojiSurrogatePairs.Replace(text, " ");
        text = Constants.EmojiModifiers.Replace(text, " ");

        // Normalise whitespace
        text = Constants.MultipleWhitespace.Replace(text, " ").Trim();

        return text;
    }

    private string RemoveUnwantedPatterns(string text)
    {
        // Remove URLs
        text = Constants.UrlPattern.Replace(text, " ");

        // Remove email addresses
        text = Constants.EmailPattern.Replace(text, " ");

        // Remove common unwanted phrases
        foreach (var pattern in Constants.UnwantedPhrases)
        {
            text = Regex.Replace(text, pattern, " ", RegexOptions.IgnoreCase);
        }

        return text;
    }
}
