using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace vIRC.Events
{
    /// <summary>
    /// Event data for <see cref="vIRC.IrcClient.RemoteCertificateValidation"/>.
    /// </summary>
    public class RemoteCertificateValidationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the certificate is accepted or not.
        /// </summary>
        public bool? Accept { get; set; }

        /// <summary>
        /// Gets the certificate presented by the server.
        /// </summary>
        public X509Certificate Certificate { get; private set; }

        /// <summary>
        /// Gets the chain of the certificate presented by the server.
        /// </summary>
        public X509Chain Chain { get; private set; }

        /// <summary>
        /// Gets the SSL policy errors encountered.
        /// </summary>
        public SslPolicyErrors SslPolicyErrors { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="vIRC.Events.RemoteCertificateValidationEventArgs"/> class with the given certificate, chain and SSL policy errors.
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        public RemoteCertificateValidationEventArgs(X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            this.Certificate = certificate;
            this.Chain = chain;
            this.SslPolicyErrors = sslPolicyErrors;

            this.Accept = null;
        }
    }
}
