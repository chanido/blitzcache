# BlitzCache
BlitzCache is a thread safe cache for dotnet that ensures the cached function is executed only once during the cache period. Simplicity is the main objective (although making a threadsafe cache that works is also a nice to have)

## Why do I want it?
Usually to get a value from the cache you have to use several lines of code, check if the value was retrieved, if it wasn't call the method to get the value, update the cache etcetera. With BlitzCache's `BlitzGet` you will have a method to which you can pass a function and it will automatically get the result of the given function from the cache (best case scenario) or execute the function, update the cache with the result and return it all in one go (worst case scenario).

Using a non thread safe cache, if the call to the function is made while it is being executed it results in multiple simultaneous calls to the given (and slow) function. The slower the function, the more likely you are to call it more than once and the higher the impact.

BlitzCache solves that issue ensuring that only one call is made to the slow function during the timespan of the cache of that particular key.

Different keys can be requested in parallel and all different keys requests will be processed at once (as long as you have enough threads) but if the same key is requested more than once the second call will wait until the first is executed an then get the value from cache. (Locks are granular). This allows even better performance!

More info about why, how and stuff [here](http://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)

## Instalation instructions
`Install-Package BlitzCache -Version 1.0.0` as usual
You can visit the nuget.org page of the package here https://www.nuget.org/packages/BlitzCache/

## Include it in your project
### Via Dependency Injection
Look for your `ServiceProvider` in your application and add `.AddBlitzCache()` to them. Alternatively you can set a default timespan for the cache in milliseconds with `.AddBlitzCache(30000)`

### Creating a new class
You can directly create it as a new class with `var cache = new BlitzCache();` or, as before `cache = new BlitzCache(60000);`

#### Remember that BlitzCache is a singleton!
If you create two instances of BlitzCache in the same application and request a key, you will get the same result in both instances.

## How do I use it?
Lets imagine we have class with a synchronous method that takes forever to execute
```csharp
public class SlowClass
{
  public int ProcessQuickly(); //Takes 100ms to process
}
```

And another class with a nice asynchronous method to cache that is also slow
```csharp
public class SlowClassAsync
{
  public async Task<int> ProcessQuickly(); //Takes 100ms to process
}
```

### With all the parameters
We simply describe the name of the cache key we are going to use for and for how long we will have the value cached (in milliseconds)
#### Sync
```csharp
var slowClass = new SlowClass();
var result = cache.BlitzGet("CacheKey", slowClass.ProcessQuickly, 10000));
```
#### Async
```csharp
var slowClass = new SlowClassAsync();
var result = await cache.BlitzGet("CacheKey", slowClass.ProcessQuickly, 500);
```
### With timespan by default
We simply describe the name of the cache key and we fallback to the previously configured timespan (by default is 60s)
#### Sync
```csharp
var slowClass = new SlowClass();
var result = cache.BlitzGet("CacheKey", slowClass.ProcessQuickly));
```
#### Async
```csharp
var slowClass = new SlowClassAsync();
var result = await cache.BlitzGet("CacheKey", slowClass.ProcessQuickly);
```
### With timespan AND CacheKey by default!
The value will be stored in the cache using `the name of the method calling`+ `the file of the method calling` as a key. (This is as cool as weird, I know)
#### Sync
```csharp
var slowClass = new SlowClass();
var result = cache.BlitzGet(slowClass.ProcessQuickly));
```
#### Async
```csharp
var slowClass = new SlowClassAsync();
var result = await cache.BlitzGet(slowClass.ProcessQuickly);
```

### With variable timespan per key depending on the execution
```csharp
var slowClass = new SlowClassAsync();
cache.BlitzGet(GetKey(i), (n) => {
  bool? result = null;
  try { result = slowClass.FailIfZeroTrueIfEven(i); }
  catch { }

  switch (result)
  {
    case null: n.CacheRetention = 1000; break; //For nulls we know we have failed so we want to retry quick
    case true: n.CacheRetention = 2000; break; //For true we want to wait 2 seconds
    case false: n.CacheRetention = 3000; break;//For false we want to wait even longer
  }

  return result;
});
```

## Thanks for using it!
