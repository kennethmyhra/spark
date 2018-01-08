using System;
using System.Linq;
using System.Net.Http.Headers;

namespace Lib.Net.Http.EncryptedContentEncoding.Internals
{
    internal static class HttpContentHeadersExtensions
    {
        internal static void CopyTo(this HttpContentHeaders source, HttpContentHeaders destination)
        {
            foreach (var header in source)
            {
                if (header.Key == "Expires")
                {
                    if (header.Value.Count() > 0 && !DateTime.TryParse(header.Value.FirstOrDefault(), out DateTime result))
                        continue;
                }
                destination.Add(header.Key, header.Value);
            }
        }
    }
}
