# BlitzCache Automatic Statistics Logging

This extension provides automatic periodic logging of BlitzCache performance statistics using the standard .NET logging infrastructure with application identification for microservice environments.

## Quick Start

```csharp
using BlitzCacheCore.Extensions;  // Single namespace for all extensions

// Configure services
services.AddBlitzCacheInstance(enableStatistics: true); // Statistics must be enabled
services.AddBlitzCacheLogging(TimeSpan.FromHours(1));   // Log every hour with auto-detected app name

// Or with custom application identifier for microservices
services.AddBlitzCacheLogging(TimeSpan.FromHours(1), "UserService-API");
```

## Features

- **Automatic Logging**: Periodically logs cache performance metrics
- **Application Identification**: Auto-detects application name or uses custom identifier
- **Microservice Support**: Perfect for distinguishing logs in multi-service environments
- **Configurable Intervals**: Set any logging interval (default: 1 hour)
- **Standard Logging**: Uses `ILogger` infrastructure - works with any logging provider
- **Performance Metrics**: Logs hits, misses, hit ratio, entries, evictions, and more
- **Smart Detection**: Warns if statistics are disabled

## Example Log Output

**With Auto-Detection:**
```
[Information] [MyApplication] BlitzCache Statistics - Hits: 1547, Misses: 423, Hit Ratio: 78.53%, Entries: 342, Evictions: 156, Active Semaphores: 12, Total Operations: 1970
```

**With Custom Identifier:**
```
[Information] [UserService-API] BlitzCache Statistics - Hits: 892, Misses: 108, Hit Ratio: 89.20%, Entries: 245, Evictions: 67, Active Semaphores: 8, Total Operations: 1000
```

## Real-World Usage

### Basic Setup (Auto-Detection)
```csharp
using BlitzCacheCore.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    // Add BlitzCache with statistics enabled
    services.AddBlitzCacheInstance(
        defaultMilliseconds: 300000, // 5 minutes
        enableStatistics: true       // Required for logging
    );
    
    // Enable automatic statistics logging with auto-detected app name
    services.AddBlitzCacheLogging(TimeSpan.FromHours(1));
}
```

### Microservice Environment (Custom Identifiers)
```csharp
using BlitzCacheCore.Extensions;

// User Service
public void ConfigureServices(IServiceCollection services)
{
    services.AddBlitzCacheInstance(enableStatistics: true);
    services.AddBlitzCacheLogging(TimeSpan.FromMinutes(30), "UserService-API");
}

// Order Service  
public void ConfigureServices(IServiceCollection services)
{
    services.AddBlitzCacheInstance(enableStatistics: true);
    services.AddBlitzCacheLogging(TimeSpan.FromMinutes(30), "OrderService-API");
}

// Payment Service
public void ConfigureServices(IServiceCollection services)
{
    services.AddBlitzCacheInstance(enableStatistics: true);
    services.AddBlitzCacheLogging(TimeSpan.FromMinutes(30), "PaymentService-API");
}
```

## Application Detection

The system automatically detects the application name using multiple fallback methods:

1. **Entry Assembly Name** (most reliable)
2. **Process Name** (excludes common runtime names like "dotnet")
3. **Executable File Name**
4. **Calling Assembly Name**
5. **Fallback**: "Unknown-Application"

Common runtime names like "dotnet", "testhost" are automatically excluded to provide meaningful identifiers.

## Configuration Options

- **Default Interval**: 1 hour
- **Custom Intervals**: Any `TimeSpan` value
- **Development**: Use shorter intervals like `TimeSpan.FromMinutes(10)`
- **Production**: Use longer intervals like `TimeSpan.FromHours(6)`
- **Application Identifier**: Auto-detected or custom string

## File Organization

The logging functionality is organized as follows:

- **`Extensions/IServiceCollectionExtensions.cs`** - All service collection extension methods (including `AddBlitzCacheLogging`)
- **`Logging/BlitzCacheLoggingService.cs`** - Background service that handles periodic logging with application identification

## Requirements

- BlitzCache must be configured with `enableStatistics: true`
- Application must use `IHostedService` infrastructure (ASP.NET Core, Worker Services, etc.)
- Standard .NET logging must be configured

## Benefits

- **Monitoring**: Track cache performance over time per application/service
- **Microservice Identification**: Distinguish logs from different services in centralized logging
- **Optimization**: Identify cache hit ratio trends per service
- **Debugging**: Detect memory leaks or excessive cache growth in specific services
- **Alerting**: Integrate with monitoring systems using log aggregation with service context
