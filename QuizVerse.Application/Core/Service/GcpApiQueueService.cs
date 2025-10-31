using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using QuizVerse.Application.Core.Interface;
using QuizVerse.Domain.Entities;
using QuizVerse.Infrastructure.Common;
using QuizVerse.Infrastructure.DTOs.RequestDTOs;
using QuizVerse.Infrastructure.DTOs.ResponseDTOs;
using QuizVerse.Infrastructure.Enums;
using QuizVerse.Infrastructure.Interface;

namespace QuizVerse.Application.Core.Service;

public class GcpApiQueueService : IGcpApiQueueService
{
    private readonly ILogger<GcpApiQueueService> _logger;
    private readonly IPerspectiveApiService _perspectiveApiService;
    private readonly IServiceScopeFactory _serviceScopeFactory; // Add this
    private readonly Channel<GcpApiReportDataDto> _channel;
    private readonly IMemoryCache _cache;
    private const int RATE_LIMIT_DELAY_MS = 1000;
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 2000;
    private const int CACHE_EXPIRATION_HOURS = 24;

    // Cache key prefixes
    private const string CACHE_KEY_PREFIX = "PerspectiveAPI";

    public GcpApiQueueService(
        ILogger<GcpApiQueueService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IPerspectiveApiService perspectiveApiService,
        IMemoryCache cache)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _perspectiveApiService = perspectiveApiService;
        _cache = cache;

        // Unbounded channel for queue processing
        _channel = Channel.CreateUnbounded<GcpApiReportDataDto>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    }

    public async Task EnqueueAsync(GcpApiReportDataDto data)
    {
        // Check cache before enqueuing
        string cacheKey = GenerateCacheKey(data.ReportComment, data.ReportType);
        data.CacheKey = cacheKey;

        if (_cache.TryGetValue<CachedPerspectiveResult>(cacheKey, out var cachedResult) && cachedResult != null)
        {
            _logger.LogInformation(
                "Cache HIT for {ReportType} ID: {ReportId}. Skipping queue and applying cached result directly.",
                data.ReportType, data.ReportId);

            // Apply cached result directly without queuing
            await ApplyCachedResultAsync(data, cachedResult);
            return;
        }

        _logger.LogInformation(
            "Cache MISS for {ReportType} ID: {ReportId}. Adding to queue.",
            data.ReportType, data.ReportId);

        // If not in cache, enqueue for processing
        await _channel.Writer.WriteAsync(data);

        _logger.LogInformation("Enqueued {ReportType} ID: {ReportId}.",
            data.ReportType, data.ReportId);
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Perspective API Queue Processor with 1 QPS rate limit");

        // Load pending items from database on startup (crash recovery)
        await LoadPendingItemsFromDatabaseAsync();

        // Start continuous processing
        await ProcessQueueAsync(cancellationToken);
    }

    private async Task LoadPendingItemsFromDatabaseAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var sqlQueryRepository = scope.ServiceProvider.GetRequiredService<ISqlQueryRepository>();

        string query = string.Format(
            SqlConstants.GET_PENDING_GCP_REPORTS_TEMPLATE,
            SqlConstants.GET_PENDING_GCP_REPORTS_FUNCTION
        );

        var parameters = new NpgsqlParameter[]
        {
            new("p_quiz_rating_feedback_type", NpgsqlDbType.Integer) { Value = (int)ReportType.QuizRatingFeedback },
            new("p_question_issue_report_type", NpgsqlDbType.Integer) { Value = (int)ReportType.QuestionIssueReport },
            new("p_quiz_issue_report_type", NpgsqlDbType.Integer) { Value = (int)ReportType.QuizIssueReport },
        };

        var result = await sqlQueryRepository.SqlQueryListAsync<GcpApiReportAllDataDto>(query, parameters);

        if (result == null || result.Count == 0)
        {
            _logger.LogInformation("No pending GCP reports found in database.");
            return;
        }

        foreach (var record in result)
        {
            var dto = new GcpApiReportDataDto
            {
                ReportId = record.ReportId,
                ReportType = (ReportType)record.ReportType,
                ReportComment = record.ReportComment
            };

            await EnqueueAsync(dto);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Queue processor started");

        await foreach (var item in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessItemWithRetryAsync(item, cancellationToken);

                // Rate limiting: 1 request per second (1 QPS)
                await Task.Delay(RATE_LIMIT_DELAY_MS, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Queue processing cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item: {ReportType} ID: {ReportId}",
                    item.ReportType, item.ReportId);
            }
        }
    }

    private async Task ProcessItemWithRetryAsync(GcpApiReportDataDto item, CancellationToken cancellationToken)
    {
        // Double-check cache before API call (in case it was cached while in queue)
        string cacheKey = item.CacheKey ?? GenerateCacheKey(item.ReportComment, item.ReportType);

        if (_cache.TryGetValue<CachedPerspectiveResult>(cacheKey, out var cachedResult) && cachedResult != null)
        {
            _logger.LogInformation(
                "Cache HIT during processing for {ReportType} ID: {ReportId}. Using cached result.",
                item.ReportType, item.ReportId);

            await ApplyCachedResultAsync(item, cachedResult);
            return;
        }

        int retryCount = 0;
        bool success = false;

        while (retryCount < MAX_RETRY_ATTEMPTS && !success && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Start Processing {ReportType} ID: {ReportId} (Attempt {Attempt}/{MaxAttempts})",
                    item.ReportType, item.ReportId, retryCount + 1, MAX_RETRY_ATTEMPTS);

                // Call Perspective API
                PerspectiveAnalysisResult result = await _perspectiveApiService.AnalyzeTextAsync(item);

                if (result.IsSuccess)
                {
                    // Cache the result before updating the database
                    CacheAnalysisResult(item.ReportComment, item.ReportType, result);

                    // Update respective table based on report type
                    await UpdateReportTableAsync(result);
                    success = true;

                    _logger.LogInformation(
                        "Successfully request processed {ReportType} ID: {ReportId}, Severity: {Severity}, Score: {Score}",
                        item.ReportType, item.ReportId, result.Severity, result.AttributeScore);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to process {ReportType} ID: {ReportId}, Error: {Error}",
                        item.ReportType, item.ReportId, result.ErrorMessage);

                    retryCount++;
                    if (retryCount < MAX_RETRY_ATTEMPTS)
                    {
                        await Task.Delay(RETRY_DELAY_MS, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {ReportType} ID: {ReportId}",
                    item.ReportType, item.ReportId);

                retryCount++;
                if (retryCount < MAX_RETRY_ATTEMPTS)
                {
                    await Task.Delay(RETRY_DELAY_MS, cancellationToken);
                }
            }
        }

        if (!success)
        {
            _logger.LogCritical(
                "CRITICAL: Failed to process {ReportType} ID: {ReportId} after {MaxAttempts} attempts. Using default values.",
                item.ReportType, item.ReportId, MAX_RETRY_ATTEMPTS);
        }
    }

    private static string GenerateCacheKey(string comment, ReportType reportType)
    {
        // Normalize the comment (trim, lowercase) for consistent cache keys
        string normalizedComment = comment?.Trim().ToLowerInvariant() ?? string.Empty;

        // Generate hash for the comment to keep cache key length manageable
        string commentHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(normalizedComment)
            )
        ).Substring(0, 16); // Use first 16 chars of hash

        return $"{CACHE_KEY_PREFIX}:{reportType}:{commentHash}";
    }

    private void CacheAnalysisResult(string comment, ReportType reportType, PerspectiveAnalysisResult result)
    {
        string cacheKey = GenerateCacheKey(comment, reportType);

        var cachedResult = new CachedPerspectiveResult
        {
            Severity = result.Severity,
            IsFlagged = result.IsFlagged,
            FlagReason = result.FlagReason,
            AttributeScore = result.AttributeScore,
            ReportType = reportType,
            CachedAt = DateTime.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CACHE_EXPIRATION_HOURS),
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, cachedResult, cacheOptions);

        _logger.LogInformation(
            "Save Cached analysis result for comment: {Comment} hash with Severity: {Severity}, IsFlagged: {IsFlagged}",
            comment, result.Severity, result.IsFlagged);
    }

    private async Task ApplyCachedResultAsync(GcpApiReportDataDto item, CachedPerspectiveResult cachedResult)
    {
        try
        {
            // Create a PerspectiveAnalysisResult from cached data
            var result = new PerspectiveAnalysisResult
            {
                ReportId = item.ReportId,
                ReportType = item.ReportType,
                Severity = cachedResult.Severity,
                IsFlagged = cachedResult.IsFlagged,
                FlagReason = cachedResult.FlagReason,
                AttributeScore = cachedResult.AttributeScore,
                IsSuccess = true
            };

            await UpdateReportTableAsync(result);

            _logger.LogInformation(
                "Successfully Applied cached result in DB for {ReportType} ID: {ReportId}, Severity: {Severity}",
                item.ReportType, item.ReportId, cachedResult.Severity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error applying cached result for {ReportType} ID: {ReportId}",
                item.ReportType, item.ReportId);
        }
    }

    private async Task UpdateReportTableAsync(PerspectiveAnalysisResult result)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        switch (result.ReportType)
        {
            case ReportType.QuizRatingFeedback:
                var quizRatingRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<QuizRating>>();
                var rating = await quizRatingRepo.GetAsync(r => r.Id == result.ReportId);

                if (rating != null)
                {
                    rating.IsFlagged = result.IsFlagged;
                    rating.Status = rating.IsFlagged
                        ? (int)QuizRatingStatus.UnderReview
                        : (int)QuizRatingStatus.Ignore;

                    if (rating.IsFlagged && !string.IsNullOrEmpty(result.FlagReason))
                    {
                        rating.Reason = result.FlagReason;
                    }
                    else
                    {
                        rating.Reason = null;
                    }

                    rating.ModifiedDate = DateTime.UtcNow;
                    await quizRatingRepo.UpdateAsync(rating);
                }
                break;

            case ReportType.QuestionIssueReport:
                var questionIssueReportRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<QuestionIssueReport>>();
                var questionIssue = await questionIssueReportRepo
                    .GetAsync(q => q.Id == result.ReportId);

                if (questionIssue != null)
                {
                    questionIssue.Severity = (int)result.Severity;
                    questionIssue.Status = (int)QuestionOrQuizIssueReportStatus.Pending;
                    questionIssue.ModifiedDate = DateTime.UtcNow;
                    await questionIssueReportRepo.UpdateAsync(questionIssue);
                }
                break;

            case ReportType.QuizIssueReport:
                var quizIssueReportRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<QuizIssueReport>>();
                var quizIssue = await quizIssueReportRepo.GetAsync(q => q.Id == result.ReportId);

                if (quizIssue != null)
                {
                    quizIssue.Severity = (int)result.Severity;
                    quizIssue.Status = (int)QuestionOrQuizIssueReportStatus.Pending;
                    quizIssue.ModifiedDate = DateTime.UtcNow;
                    await quizIssueReportRepo.UpdateAsync(quizIssue);
                }
                break;
        }
    }
}
