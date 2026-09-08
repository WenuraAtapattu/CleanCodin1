using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LudoGame.TestClient2;

/// <summary>
/// Test Client 2: Concurrent Load Tester
/// Simulates multiple independent client sessions sending requests simultaneously.
/// This addresses the assignment requirement for multiple test clients with concurrent async requests.
/// </summary>
class Program
{
    private static readonly string ServerBaseUrl = "https://localhost:5001/api/";
    private static readonly string ClientId = Guid.NewGuid().ToString();
    private static int _totalRequestsSent = 0;
    private static int _successfulRequests = 0;
    private static int _failedRequests = 0;
    private static readonly object _statsLock = new();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LUDO-T Test Client 2: Concurrent Load Tester ===");
        Console.WriteLine($"Client ID: {ClientId}");
        Console.WriteLine("This client simulates multiple concurrent client sessions.");
        Console.WriteLine();
        
        // Configure HttpClient to ignore SSL errors for development
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        try
        {
            // Test 1: Single session baseline
            Console.WriteLine("Test 1: Single session baseline (10 requests)...");
            await RunSingleSessionAsync(10);
            Console.WriteLine();
            
            // Test 2: Multiple concurrent sessions
            Console.WriteLine("Test 2: 5 concurrent sessions (10 requests each)...");
            await RunMultipleConcurrentSessionsAsync(5, 10);
            Console.WriteLine();
            
            // Test 3: High concurrency load test
            Console.WriteLine("Test 3: 10 concurrent sessions (20 requests each)...");
            await RunMultipleConcurrentSessionsAsync(10, 20);
            Console.WriteLine();
            
            // Test 4: Very high concurrency stress test
            Console.WriteLine("Test 4: 20 concurrent sessions (15 requests each)...");
            await RunMultipleConcurrentSessionsAsync(20, 15);
            Console.WriteLine();
            
            // Test 5: Mixed request types
            Console.WriteLine("Test 5: Mixed request types (start, step, pause, resume)...");
            await RunMixedRequestTypesAsync();
            Console.WriteLine();
            
            // Test 6: Sustained load test
            Console.WriteLine("Test 6: Sustained load test (5 clients over 30 seconds)...");
            await RunSustainedLoadTestAsync(5, 30);
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
    
    private static async Task RunSingleSessionAsync(int requestCount)
    {
        var stopwatch = Stopwatch.StartNew();
        var client = CreateHttpClient();
        
        for (int i = 0; i < requestCount; i++)
        {
            try
            {
                var response = await client.PostAsync("game/step", null);
                UpdateStats(response.IsSuccessStatusCode);
                
                await Task.Delay(100); // 100ms between requests
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
                UpdateStats(false);
            }
        }
        
        stopwatch.Stop();
        Console.WriteLine($"Single session completed: {requestCount} requests in {stopwatch.ElapsedMilliseconds}ms");
    }
    
    private static async Task RunMultipleConcurrentSessionsAsync(int sessionCount, int requestsPerSession)
    {
        var stopwatch = Stopwatch.StartNew();
        var sessionTasks = new List<Task>();
        
        Console.WriteLine($"Starting {sessionCount} concurrent sessions with {requestsPerSession} requests each...");
        
        for (int i = 0; i < sessionCount; i++)
        {
            int sessionId = i;
            sessionTasks.Add(Task.Run(async () =>
            {
                await RunClientSessionAsync(sessionId, requestsPerSession);
            }));
        }
        
        await Task.WhenAll(sessionTasks);
        
        stopwatch.Stop();
        int totalRequests = sessionCount * requestsPerSession;
        Console.WriteLine($"All sessions completed: {totalRequests} total requests in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average throughput: {totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0):F2} requests/second");
    }
    
    private static async Task RunClientSessionAsync(int sessionId, int requestCount)
    {
        var client = CreateHttpClient();
        var sessionStopwatch = Stopwatch.StartNew();
        
        Console.WriteLine($"Session {sessionId} started with {requestCount} requests...");
        
        for (int i = 0; i < requestCount; i++)
        {
            try
            {
                var response = await client.PostAsync("game/step", null);
                UpdateStats(response.IsSuccessStatusCode);
                
                // Variable delay to simulate realistic client behavior
                var delay = Random.Shared.Next(50, 150);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Session {sessionId} request {i + 1} failed: {ex.Message}");
                UpdateStats(false);
            }
        }
        
        sessionStopwatch.Stop();
        Console.WriteLine($"Session {sessionId} completed: {requestCount} requests in {sessionStopwatch.ElapsedMilliseconds}ms");
    }
    
    private static async Task RunMixedRequestTypesAsync()
    {
        var client = CreateHttpClient();
        var requestTypes = new[] { "start", "step", "step", "pause", "resume", "step", "step", "reset" };
        
        Console.WriteLine("Sending mixed request types...");
        
        foreach (var requestType in requestTypes)
        {
            try
            {
                HttpResponseMessage response;
                string endpoint = requestType switch
                {
                    "start" => "game/start",
                    "step" => "game/step",
                    "pause" => "game/pause",
                    "resume" => "game/resume",
                    "reset" => "game/reset",
                    _ => "game/step"
                };
                
                response = await client.PostAsync(endpoint, null);
                UpdateStats(response.IsSuccessStatusCode);
                
                Console.WriteLine($"Sent {requestType} request: {response.StatusCode}");
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{requestType} request failed: {ex.Message}");
                UpdateStats(false);
            }
        }
        
        Console.WriteLine("Mixed request types test completed");
    }
    
    private static async Task RunSustainedLoadTestAsync(int clientCount, int durationSeconds)
    {
        var stopwatch = Stopwatch.StartNew();
        var cts = new CancellationTokenSource();
        var clientTasks = new List<Task>();
        
        Console.WriteLine($"Starting sustained load test: {clientCount} clients for {durationSeconds} seconds...");
        
        // Start multiple clients
        for (int i = 0; i < clientCount; i++)
        {
            int clientId = i;
            clientTasks.Add(Task.Run(async () =>
            {
                await RunContinuousClientSessionAsync(clientId, cts.Token);
            }));
        }
        
        // Run for specified duration
        await Task.Delay(durationSeconds * 1000);
        cts.Cancel();
        
        await Task.WhenAll(clientTasks);
        
        stopwatch.Stop();
        Console.WriteLine($"Sustained load test completed: Ran for {stopwatch.ElapsedMilliseconds}ms");
    }
    
    private static async Task RunContinuousClientSessionAsync(int clientId, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient();
        var requestCount = 0;
        
        Console.WriteLine($"Continuous client {clientId} started...");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await client.PostAsync("game/step", null);
                UpdateStats(response.IsSuccessStatusCode);
                requestCount++;
                
                if (requestCount % 10 == 0)
                {
                    Console.WriteLine($"Client {clientId}: Sent {requestCount} requests so far...");
                }
                
                await Task.Delay(200, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {clientId} request failed: {ex.Message}");
                UpdateStats(false);
            }
        }
        
        Console.WriteLine($"Continuous client {clientId} stopped after {requestCount} requests");
    }
    
    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(ServerBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
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