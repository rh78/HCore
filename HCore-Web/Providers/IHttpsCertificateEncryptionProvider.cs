namespace HCore.Web.Providers
{
    /// <summary>
    /// Encrypts and decrypts HTTPS certificates and their passwords
    /// </summary>
    public interface IHttpsCertificateEncryptionProvider
    {
        /// <summary>
        /// Encrypts an HTTPS certificate
        /// </summary>
        string EncryptHttpsCertificate(byte[] httpsCertificateBytes);

        /// <summary>
        /// Encrypts an HTTPS certificate password
        /// </summary>
        string EncryptHttpsCertificatePassword(string httpsCertificatePassword);

        /// <summary>
        /// Decrypts an encrypted HTTPS certificate
        /// </summary>
        byte[] DecryptHttpsCertificate(string encryptedHttpsCertificate);

        /// <summary>
        /// Decrypts an encrypted HTTPS certificate password
        /// </summary>
        string DecryptHttpsCertificatePassword(string encryptedHttpsCertificatePassword);
    }
}
