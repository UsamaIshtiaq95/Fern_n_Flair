//using Microsoft.AspNetCore.Http;
//using StackExchange.Redis;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace GatewayApi.Middleware
//{
//    public class RedisCacheMiddleware
//    {
//        private readonly RequestDelegate _next;
//        private readonly IDatabase _cacheDb;
//        private const string CachePrefix = "GatewayApiCache:";

//        public RedisCacheMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
//        {
//            _next = next;
//            _cacheDb = redis.GetDatabase();
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            if (!HttpMethods.IsGet(context.Request.Method))
//            {
//                await _next(context);
//                return;
//            }

//            var cacheKey = CachePrefix + context.Request.Path + context.Request.QueryString;
//            var cached = await _cacheDb.StringGetAsync(cacheKey);
//            if (cached.HasValue)
//            {
//                context.Response.ContentType = "application/json";
//                await context.Response.WriteAsync(cached);
//                return;
//            }

//            var originalBody = context.Response.Body;
//            using var memStream = new System.IO.MemoryStream();
//            context.Response.Body = memStream;

//            await _next(context);

//            if (context.Response.StatusCode == StatusCodes.Status200OK)
//            {
//                memStream.Seek(0, System.IO.SeekOrigin.Begin);
//                var responseBody = await new System.IO.StreamReader(memStream).ReadToEndAsync();
//                await _cacheDb.StringSetAsync(cacheKey, responseBody);
//                memStream.Seek(0, System.IO.SeekOrigin.Begin);
//                await memStream.CopyToAsync(originalBody);
//            }
//            else
//            {
//                memStream.Seek(0, System.IO.SeekOrigin.Begin);
//                await memStream.CopyToAsync(originalBody);
//            }
//            context.Response.Body = originalBody;
//        }
//    }
//}
