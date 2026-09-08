using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LudoGame.TestClient1;

/// <summary>
/// Test Client 1: Rapid Request Sender
/// Sends multiple asynchronous requests in rapid succession to test server queuing and concurrency.
/// This addresses the assignment requirement for test clients that send simultaneous async requests.
/// </summary>
class Program
{
    private static readonly HttpClient _httpClient = new HttpClient
    {
        BaseAddress = new Uri("https://localhost:5001/api/")
    };
    
    private static readonly string ClientId = Guid.NewGuid().ToString();
    private static int _totalRequestsSent = 0;
    private static int _successfulRequests = 0;
    private static int _failedRequests = 0;
    private static readonly object _statsLock = new();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LUDO-T Test Client 1: Rapid Request Sender ===");
        Console.WriteLine($"Client ID: {ClientId}");
        Console.WriteLine("This client sends rapid asynchronous requests to test server concurrency.");
        Console.WriteLine();
        
        // Configure HttpClient to ignore SSL errors for development
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:5001/api/")
        };
        
        try
        {
            // Test 1: Basic connectivity
            Console.WriteLine("Test 1: Checking server connectivity...");
            await TestServerHealthAsync();
            Console.WriteLine();
            
            // Test 2: Single request
            Console.WriteLine("Test 2: Sending single request...");
            await SendSingleRequestAsync();
            Console.WriteLine();
            
            // Test 3: Rapid sequential requests
            Console.WriteLine("Test 3: Sending 20 rapid sequential requests...");
            await SendRapidSequentialRequestsAsync(20);
            Console.WriteLine();
            
            // Test 4: Concurrent parallel requests
            Console.WriteLine("Test 4: Sending 20 concurrent parallel requests...");
            await SendConcurrentParallelRequestsAsync(20);
            Console.WriteLine();
            
            // Test 5: Very rapid request burst
            Console.WriteLine("Test 5: Sending 100 very rapid requests (stress test)...");
            await SendVeryRapidRequestsAsync(100);
            Console.WriteLine();
            
            // Test 6: Check queue statistics
            Console.WriteLine("Test 6: Checking server queue statistics...");
            await CheckQueueStatisticsAsync();
            Console.WriteLine();
            
            // Display final statistics
            DisplayFinalStatistics();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    private static async Task TestServerHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("game/health");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Server health check: {response.StatusCode}");
            Console.WriteLine($"Response: {content}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check failed: {ex.Message}");
        }
    }
    
    private static async Task SendSingleRequestAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("game/step", null);
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Single request completed: {response.StatusCode}");
            Console.WriteLine($"Response: {content}");
            
            UpdateStats(response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Single request failed: {ex.Message}");
            UpdateStats(false);
        }
    }
    
    private static async Task SendRapidSequentialRequestsAsync(int count)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < count; i++)
        {
            try
            {
                var response = await _httpClient.PostAsync("game/step", null);
                UpdateStats(response.IsSuccessStatusCode);
                
                if (i % 5 == 0)
                {
                    Console.WriteLine($"Sent {i + 1}/{count} requests...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request {i + 1} failed: {ex.Message}");
                UpdateStats(false);
            }
        }
        
        stopwatch.Stop();
        Console.WriteLine($"Completed {count} sequential requests in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (double)count:F2}ms per request");
    }
    
    private static async Task SendConcurrentParallelRequestsAsync(int count)
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        
        Console.WriteLine($"Starting {count} concurrent requests...");
        
        for (int i = 0; i < count; i++)
        {
            int requestId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await _httpClient.PostAsync("game/step", null);
                    UpdateStats(response.IsSuccessStatusCode);
                    
                    if (requestId % 5 == 0)
                    {
                        Console.WriteLine($"Concurrent request {requestId + 1} completed");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Concurrent request {requestId + 1} failed: {ex.Message}");
                    UpdateStats(false);
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        stopwatch.Stop();
        Console.WriteLine($"Completed {count} concurrent requests in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Throughput: {count / (stopwatch.ElapsedMilliseconds / 1000.0):F2} requests/second");
    }
    
    private static async Task SendVeryRapidRequestsAsync(int count)
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        
        Console.WriteLine($"Starting stress test with {count} rapid requests...");
        
        // Send requests as fast as possible with minimal delay
        for (int i = 0; i < count; i++)
        {
            int requestId = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await _httpClient.PostAsync("game/step", null);
                    UpdateStats(response.IsSuccessStatusCode);
                }
                catch (Exception ex)
                {
                    UpdateStats(false);
                }
            }));
            
            // Minimal delay to prevent overwhelming the client
            if (i % 10 == 0)
            {
                await Task.Delay(1);
            }
        }
        
        await Task.WhenAll(tasks);
        
        stopwatch.Stop();
        Console.WriteLine($"Stress test completed: {count} requests in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Peak throughput: {count / (stopwatch.ElapsedMilliseconds / 1000.0):F2} requests/second");
    }
    
    private static async Task CheckQueueStatisticsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("game/queue/stats");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var stats = JsonSerializer.Deserialize<QueueStatistics>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                Console.WriteLine("Server Queue Statistics:");
                Console.WriteLine($"  Queue Depth: {stats?.QueueDepth ?? 0}");
                Console.WriteLine($"  Priority Queue Depth: {stats?.PriorityQueueDepth ?? 0}");
                Console.WriteLine($"  Total Requests Received: {stats?.TotalRequestsReceived ?? 0}");
                Console.WriteLine($"  Total Requests Processed: {stats?.TotalRequestsProcessed ?? 0}");
                Console.WriteLine($"  Total Requests Queued: {stats?.TotalRequestsQueued ?? 0}");
                Console.WriteLine($"  Total Requests Failed: {stats?.TotalRequestsFailed ?? 0}");
                Console.WriteLine($"  Success Rate: {stats?.SuccessRate ?? 0:F2}%");
            }
            else
            {
                Console.WriteLine($"Failed to get queue statistics: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting queue statistics: {ex.Message}");
        }
    }
    
    private static void UpdateStats(bool success)
    {
        lock (_statsLock)
        {
            _totalRequestsSent++;
            if (success)
                _successfulRequests++;
            else
                _failedRequests++;
        }
    }
    
    private static void DisplayFinalStatistics()
    {
        lock (_statsLock)
        {
            Console.WriteLine("=== Final Test Statistics ===");
            Console.WriteLine($"Total Requests Sent: {_totalRequestsSent}");
            Console.WriteLine($"Successful Requests: {_successfulRequests}");
            Console.WriteLine($"Failed Requests: {_failedRequests}");
            Console.WriteLine($"Success Rate: {_totalRequestsSent > 0 ? (_successfulRequests / (double)_totalRequestsSent * 100) : 0:F2}%");
        }
    }
}

// DTO for queue statistics
public class QueueStatistics
{
    public int QueueDepth { get; set; }
    public int PriorityQueueDepth { get; set; }
    public long TotalRequestsReceived { get; set; }
    public long TotalRequestsProcessed { get; set; }
    public long TotalRequestsQueued { get; set; }
    public long TotalRequestsFailed { get; set; }
    public double SuccessRate { get; set; }
}