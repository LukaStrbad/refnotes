using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ServerTests.Mocks;

public class MemoryCache() 
    : Microsoft.Extensions.Caching.Memory.MemoryCache(
        new OptionsManager<MemoryCacheOptions>(new OptionsFactory<MemoryCacheOptions>([], [])));