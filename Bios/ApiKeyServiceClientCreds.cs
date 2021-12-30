

using Microsoft.Rest;

namespace Bios
{
    public class ApiKeyServiceClientCreds: ServiceClientCredentials
    {
        private readonly string subscriptionKey;

        public ApiKeyServiceClientCreds(string subscriptionKey)
        {
            this.subscriptionKey = subscriptionKey;
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(request == null)
            {
                throw new ArgumentException("request");
            }
            request.Headers.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);

            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }

}
