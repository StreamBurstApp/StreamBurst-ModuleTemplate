using Microsoft.Extensions.Logging;
using StreamBurst.Abstractions.Catalog;
using StreamBurst.Abstractions.Event;
using StreamBurst.Abstractions.EventBus;
using StreamBurst.Abstractions.Module;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DesignerPreview
{
    internal sealed class MockContext : IModuleContext
    {
        public IEventBus EventBus { get; } = new MockEventBus();
        public IEventCatalog EventCatalog { get; } = new MockCatalogRegistry();
        public ILogger Logger { get; } = new MockLogger();
        public IModuleStorage ModuleStorage { get; } = new MockModuleStorage();
        public IModuleCatalog ModuleCatalog => throw new NotImplementedException();
    }

    public class MockEventBus : IEventBus
    {
        private readonly Dictionary<string, ModuleEvent> _lastValues = new();
        private readonly List<Subscription> _subscriptions = new();

        private class Subscription : IDisposable
        {
            public string? SourceModuleId { get; }
            public string? EventType { get; }
            public Action<ModuleEvent> Handler { get; }
            private readonly MockEventBus _bus;

            public Subscription(MockEventBus bus, string? sourceModuleId, string? eventType, Action<ModuleEvent> handler)
            {
                _bus = bus;
                SourceModuleId = sourceModuleId;
                EventType = eventType;
                Handler = handler;
            }

            public void Dispose()
            {
                lock (_bus._subscriptions)
                {
                    _bus._subscriptions.Remove(this);
                }
            }
        }

        public void Publish(ModuleEvent moduleEvent)
        {
            if (moduleEvent.EventId == null) return;

            string key = $"{moduleEvent.SourceModuleId}:{moduleEvent.EventId}";
            lock (_lastValues)
            {
                _lastValues[key] = moduleEvent;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[MockEventBus] Published: '{moduleEvent.EventId}' from '{moduleEvent.SourceModuleId ?? "any"}'");
            Console.WriteLine($"   Value: {moduleEvent.Value ?? "null"} (Kind: {moduleEvent.ValueKind})");
            Console.WriteLine($"   Timestamp: {moduleEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
            Console.ResetColor();

            List<Subscription> targets;
            lock (_subscriptions)
            {
                targets = new List<Subscription>(_subscriptions);
            }

            foreach (var sub in targets)
            {
                if ((sub.SourceModuleId == null || sub.SourceModuleId == moduleEvent.SourceModuleId) &&
                    (sub.EventType == null || sub.EventType == moduleEvent.EventId))
                {
                    try
                    {
                        sub.Handler(moduleEvent);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MockEventBus] Error in subscriber handler: {ex.Message}");
                    }
                }
            }

            // Auto-respond with "debug_" prefix if not already a debug event
            if (!moduleEvent.EventId.StartsWith("debug_", StringComparison.OrdinalIgnoreCase))
            {
                var responseType = "debug_" + moduleEvent.EventId;
                var responseEvent = new ModuleEvent
                {
                    SourceModuleId = moduleEvent.SourceModuleId,
                    EventId = responseType,
                    ValueKind = moduleEvent.ValueKind,
                    Value = moduleEvent.Value,
                    Timestamp = DateTimeOffset.UtcNow
                };

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[MockEventBus] Auto-responding with event '{responseType}'");
                Console.ResetColor();

                Publish(responseEvent);
            }
        }

        public IDisposable Subscribe(string? sourceModuleId, string? eventType, Action<ModuleEvent> handler)
        {
            var sub = new Subscription(this, sourceModuleId, eventType, handler);
            lock (_subscriptions)
            {
                _subscriptions.Add(sub);
            }
            return sub;
        }

        public IDisposable SubscribeAsync(string? sourceModuleId, string? eventType, Func<ModuleEvent, CancellationToken, Task> handler)
        {
            return Subscribe(sourceModuleId, eventType, ev =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await handler(ev, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MockEventBus] Error in async subscriber handler: {ex.Message}");
                    }
                });
            });
        }

        public ModuleEvent? GetLastValue(string sourceModuleId, string eventType)
        {
            string key = $"{sourceModuleId}:{eventType}";
            lock (_lastValues)
            {
                return _lastValues.TryGetValue(key, out var val) ? val : null;
            }
        }
    }

    public class MockCatalogRegistry : IEventCatalog
    {
        public IReadOnlyList<ModuleGroupEvents> GetAllEvents() => Array.Empty<ModuleGroupEvents>();
        public IReadOnlyList<ModuleGroupActions> GetAllActions() => Array.Empty<ModuleGroupActions>();
        public IReadOnlyList<CategoryGroupEvents> GetEventsForModule(string moduleId) => Array.Empty<CategoryGroupEvents>();
        public IReadOnlyList<CategoryGroupActions> GetActionsForModule(string moduleId) => Array.Empty<CategoryGroupActions>();

        public event EventHandler<EventCatalogChangedArgs>? GlobalCatalogChanged
        {
            add { }
            remove { }
        }
    }

    public class MockLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        }
    }

    public class MockModuleStorage : IModuleStorage
    {
        private readonly Dictionary<string, object> _storage = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            if (_storage.TryGetValue(key, out var val) && val is T typedVal)
            {
                return Task.FromResult<T?>(typedVal);
            }
            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, CancellationToken ct = default)
        {
            if (value != null)
            {
                _storage[key] = value;
            }
            else
            {
                _storage.Remove(key);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            _storage.Remove(key);
            return Task.CompletedTask;
        }
    }
}
