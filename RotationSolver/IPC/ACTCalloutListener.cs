using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ECommons.Logging;
using RotationSolver.Basic.Data;

namespace RotationSolver.IPC;

/// <summary>
/// WebSocket server that listens for mechanic callouts from ACT/Triggernometry.
/// Provides predictive mitigation by receiving mechanic warnings before they occur.
/// </summary>
internal class ACTCalloutListener : IDisposable
{
    private HttpListener? _httpListener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    /// <summary>
    /// Event raised when a mechanic event is received from ACT.
    /// </summary>
    public event Action<ACTMechanicEvent>? OnMechanicReceived;

    /// <summary>
    /// Gets whether the listener is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// The port the WebSocket server listens on.
    /// </summary>
    public int Port { get; set; } = 29292;

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
        {
            PluginLog.Information("[ParseLord3] ACT Callout Listener already running");
            return;
        }

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{Port}/");
            _httpListener.Start();

            IsRunning = true;
            _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));

            PluginLog.Information($"[ParseLord3] ACT Callout Listener started on port {Port}");
        }
        catch (Exception ex)
        {
            PluginLog.Error($"[ParseLord3] Failed to start ACT Callout Listener: {ex.Message}");
            Stop();
        }
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        _cancellationTokenSource?.Cancel();

        try
        {
            _httpListener?.Stop();
            _httpListener?.Close();
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"[ParseLord3] Error stopping listener: {ex.Message}");
        }

        _listenerTask?.Wait(TimeSpan.FromSeconds(2));
        PluginLog.Information("[ParseLord3] ACT Callout Listener stopped");
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && IsRunning)
        {
            try
            {
                if (_httpListener == null)
                    break;

                var context = await _httpListener.GetContextAsync();
                _ = Task.Run(() => HandleConnectionAsync(context, cancellationToken), cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                PluginLog.Warning($"[ParseLord3] Listener error: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task HandleConnectionAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!context.Request.IsWebSocketRequest)
            {
                // Handle HTTP fallback for simple POST requests
                if (context.Request.HttpMethod == "POST")
                {
                    await HandleHttpPostAsync(context);
                    return;
                }

                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            var wsContext = await context.AcceptWebSocketAsync(null);
            await HandleWebSocketAsync(wsContext.WebSocket, cancellationToken);
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"[ParseLord3] Connection handling error: {ex.Message}");
            context.Response.Close();
        }
    }

    private async Task HandleWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var messageBuilder = new StringBuilder();

        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    messageBuilder.Append(chunk);

                    if (result.EndOfMessage)
                    {
                        var message = messageBuilder.ToString();
                        messageBuilder.Clear();
                        ProcessMessage(message);
                    }
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            PluginLog.Warning($"[ParseLord3] WebSocket error: {ex.Message}");
        }
        finally
        {
            webSocket?.Dispose();
        }
    }

    private async Task HandleHttpPostAsync(HttpListenerContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
            var body = await reader.ReadToEndAsync();

            ProcessMessage(body);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            var responseBytes = Encoding.UTF8.GetBytes("{\"status\":\"received\"}");
            await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"[ParseLord3] HTTP POST error: {ex.Message}");
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            // Try to parse as a single event
            var mechanicEvent = System.Text.Json.JsonSerializer.Deserialize<ACTMechanicEvent>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (mechanicEvent != null && IsValidEvent(mechanicEvent))
            {
                OnMechanicReceived?.Invoke(mechanicEvent);

                if (Service.Config.EnableDebugTrace)
                {
                    Service.LogDebug($"[ParseLord3] ACT Event: {mechanicEvent.Type} - {mechanicEvent.Name} ({mechanicEvent.RemainingTime}s)");
                }
                return;
            }

            // Try to parse as an array of events
            var events = System.Text.Json.JsonSerializer.Deserialize<ACTMechanicEvent[]>(message, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (events != null)
            {
                foreach (var evt in events)
                {
                    if (IsValidEvent(evt))
                    {
                        OnMechanicReceived?.Invoke(evt);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Service.Config.EnableDebugTrace)
            {
                Service.LogDebug($"[ParseLord3] Failed to parse ACT message: {ex.Message}");
            }
        }
    }

    private static bool IsValidEvent(ACTMechanicEvent evt)
    {
        return !string.IsNullOrEmpty(evt.Name) &&
               evt.TimeUntilImpact > 0 &&
               evt.TimeUntilImpact < 60; // Sanity check: mechanics don't telegraph for more than 60s
    }

    public void Dispose()
    {
        Stop();
        _cancellationTokenSource?.Dispose();
        _httpListener?.Close();
    }
}
