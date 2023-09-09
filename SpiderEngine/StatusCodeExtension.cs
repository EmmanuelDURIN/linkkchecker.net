using System.Net;

namespace SpiderEngine
{
    public static class StatusCodeExtension
    {
        public static bool IsSuccess(this HttpStatusCode statusCode)
        {
            return 200 <= (int)statusCode && (int)statusCode < 300;
        }
    }
}
