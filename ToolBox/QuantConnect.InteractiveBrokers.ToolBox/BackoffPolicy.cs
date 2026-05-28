using System.Net.Sockets;

namespace QuantConnect.InteractiveBrokers.ToolBox;

/// <summary>
/// Exponential backoff policy with jitter for handling IB rate limiting
/// </summary>
public class BackoffPolicy
{
    private readonly int _baseDelayMs;
    private readonly int _maxDelayMs;
    private readonly int _maxRetries;
    private readonly Random _random;
    private readonly ILogger? _logger;

    public BackoffPolicy(
        int baseDelayMs = 1000,
        int maxDelayMs = 30000,
        int maxRetries = 5,
        ILogger? logger = null)
    {
        _baseDelayMs = baseDelayMs;
        _maxDelayMs = maxDelayMs;
        _maxRetries = maxRetries;
        _random = new Random();
        _logger = logger;
    }

    /// <summary>
    /// Execute an operation with exponential backoff and jitter
    /// </summary>
    public async Task<T> ExecuteWithBackoff<T>(
        Func<Task<T>> operation,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _maxRetries)
        {
            try
            {
                var result = await operation();
                
                if (attempt > 0)
                {
                    _logger?.LogInfo($"Operation succeeded after {attempt} retries");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt >= _maxRetries || !shouldRetry(ex))
                {
                    _logger?.LogError($"Operation failed after {attempt} attempts: {ex.Message}");
                    throw;
                }

                var delay = CalculateDelay(attempt);
                _logger?.LogInfo($"Operation failed (attempt {attempt + 1}/{_maxRetries + 1}), retrying in {delay}ms: {ex.Message}");
                
                await Task.Delay(delay, cancellationToken);
                attempt++;
            }
        }

        // This should never be reached, but compiler requires it
        throw lastException ?? new InvalidOperationException("Unexpected error in backoff policy");
    }

    /// <summary>
    /// Execute an operation with exponential backoff (void return)
    /// </summary>
    public async Task ExecuteWithBackoff(
        Func<Task> operation,
        Func<Exception, bool> shouldRetry,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithBackoff(async () =>
        {
            await operation();
            return true; // Dummy return value
        }, shouldRetry, cancellationToken);
    }

    /// <summary>
    /// Calculate delay with exponential backoff and jitter
    /// </summary>
    private int CalculateDelay(int attempt)
    {
        // Exponential backoff: baseDelay * 2^attempt
        var exponentialDelay = _baseDelayMs * Math.Pow(2, attempt);
        
        // Cap at maximum delay
        var cappedDelay = Math.Min(exponentialDelay, _maxDelayMs);
        
        // Add jitter (±25% of the delay)
        var jitterRange = cappedDelay * 0.25;
        var jitter = (_random.NextDouble() - 0.5) * 2 * jitterRange;
        
        var finalDelay = cappedDelay + jitter;
        
        return Math.Max((int)finalDelay, _baseDelayMs);
    }

    /// <summary>
    /// Determine if an exception indicates a retryable error (e.g., IB pacing violations)
    /// </summary>
    public static bool IsRetryableException(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        
        // Common IB pacing violation patterns
        if (message.Contains("pacing") || 
            message.Contains("rate limit") ||
            message.Contains("too many requests") ||
            message.Contains("throttle"))
        {
            return true;
        }

        // Network-related errors that might be transient
        if (ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is SocketException)
        {
            return true;
        }

        // Timeout errors
        if (message.Contains("timeout"))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Helper extensions for common backoff scenarios
/// </summary>
public static class BackoffExtensions
{
    /// <summary>
    /// Execute an IB API call with standard retry logic
    /// </summary>
    public static async Task<T> ExecuteIbCall<T>(
        this BackoffPolicy policy,
        Func<Task<T>> ibOperation,
        CancellationToken cancellationToken = default)
    {
        return await policy.ExecuteWithBackoff(
            ibOperation,
            BackoffPolicy.IsRetryableException,
            cancellationToken);
    }

    /// <summary>
    /// Execute an IB API call with standard retry logic (void return)
    /// </summary>
    public static async Task ExecuteIbCall(
        this BackoffPolicy policy,
        Func<Task> ibOperation,
        CancellationToken cancellationToken = default)
    {
        await policy.ExecuteWithBackoff(
            ibOperation,
            BackoffPolicy.IsRetryableException,
            cancellationToken);
    }
}