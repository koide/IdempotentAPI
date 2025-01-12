using System.Net;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Cache.FusionCache.Extensions.DependencyInjection;
using IdempotentAPI.DistributedAccessLock.MadelsonDistributedLock.Extensions.DependencyInjection;
using IdempotentAPI.DistributedAccessLock.RedLockNet.Extensions.DependencyInjection;
using IdempotentAPI.Extensions.DependencyInjection;
using IdempotentAPI.TestWebAPIs.Extensions;
using Medallion.Threading;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace IdempotentAPI.TestWebAPIs
{
    /// <summary>
    /// - dotnet run Caching=MemoryCache DALock=RedLockNet
    /// - dotnet run Caching=MemoryCache DALock=MadelsonDistLock
    /// - dotnet run Caching=FusionCache DALock=RedLockNet
    /// - dotnet run Caching=FusionCache DALock=MadelsonDistLock
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            // Add services to the container.
            services.AddApiVersioningConfigured();


            // Register the IdempotentAPI Core services.
            services.AddIdempotentAPI();

            services.AddControllers();


            // Register the Caching Method:
            var caching = Configuration.GetValue<string>("Caching");
            switch (caching)
            {
                case "MemoryCache":
                    services.AddDistributedMemoryCache();
                    services.AddIdempotentAPIUsingDistributedCache();
                    break;
                // Caching: FusionCache(via Redis)
                case "FusionCache":
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = "localhost:6379";
                    });
                    services.AddFusionCacheNewtonsoftJsonSerializer();
                    services.AddIdempotentAPIUsingFusionCache();
                    break;
                default:
                    Console.WriteLine($"Caching method '{caching}' is not recognized. Options: MemoryCache, FusionCache.");
                    Environment.Exit(0);
                    break;
            }
            Console.WriteLine($"Caching method: {caching}");



            // Register the Distributed Access Lock Method:
            var distributedAccessLock = Configuration.GetValue<string>("DALock");
            switch (distributedAccessLock)
            {
                // RedLock.Net
                case "RedLockNet":
                    List<DnsEndPoint> redisEndpoints = new List<DnsEndPoint>()
                    {
                        new DnsEndPoint("localhost", 6379)
                    };
                    services.AddRedLockNetDistributedAccessLock(redisEndpoints);
                    break;
                // Madelson/DistributedLock (via Redis)
                case "MadelsonDistLock":
                    var redicConnection = ConnectionMultiplexer.Connect("localhost:6379");
                    services.AddSingleton<IDistributedLockProvider>(_ => new RedisDistributedSynchronizationProvider(redicConnection.GetDatabase()));
                    services.AddMadelsonDistributedAccessLock();
                    break;
                default:
                    Console.WriteLine($"Distributed Access Lock Method '{distributedAccessLock}' is not recognized. Options: RedLockNet, MadelsonDistLock.");
                    Environment.Exit(0);
                    break;
            }
            Console.WriteLine($"Distributed Access Lock Method: {distributedAccessLock}");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            //app.UseHttpsRedirection();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
