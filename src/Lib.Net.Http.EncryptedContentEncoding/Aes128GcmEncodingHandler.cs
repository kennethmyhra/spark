using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Lib.Net.Http.EncryptedContentEncoding.Internals;

namespace Lib.Net.Http.EncryptedContentEncoding
{
    /// <summary>
    /// The <see cref="DelegatingHandler"/> which wraps incoming response content in <see cref="Aes128GcmDecodedContent"/> based on Content-Encoding header value and sets Accept-Encoding header for outgoing request.
    /// </summary>
    public sealed class Aes128GcmEncodingHandler : DelegatingHandler
    {
        private const string X_AES128GCM_KEYID = "X-aes128gcm-KeyId";

        #region Fields
        private readonly Func<string, byte[]> _keyLocator;
        #endregion

        #region Constructors
        /// <summary>
        /// Instantiates a new <see cref="Aes128GcmEncodingHandler"/>.
        /// </summary>
        /// <param name="keyLocator">The function which is able to locate the keying material based on the keying material identificator.</param>
        public Aes128GcmEncodingHandler(Func<string, byte[]> keyLocator)
        {
            _keyLocator = keyLocator;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sends the request asynchronously.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancelation token.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if(request.Content.Headers.ContentEncoding.Contains(Constants.ENCRYPTED_CONTENT_ENCODING))
            {
                request.Content = new Aes128GcmDecodedContent(request.Content, _keyLocator);
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (request.Headers.AcceptEncoding.Contains(new StringWithQualityHeaderValue(Constants.ENCRYPTED_CONTENT_ENCODING)))
            {
                if (request.Headers.TryGetValues(X_AES128GCM_KEYID, out IEnumerable<string> keyIds) && keyIds.Count() == 1)
                {
                    string keyId = keyIds.First();
                    response.Content = new Aes128GcmEncodedContent(response.Content, _keyLocator(keyId), keyId, 4096);
                    if (!response.Content.Headers.ContentEncoding.Contains(Constants.ENCRYPTED_CONTENT_ENCODING))
                    {
                        response.Content.Headers.ContentEncoding.Add(Constants.ENCRYPTED_CONTENT_ENCODING);
                    }
                }
            }

            return response;
        }
        #endregion
    }
}
