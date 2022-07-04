// from https://stackoverflow.com/questions/57805727/unknown-crypto-algorithm-http-www-w3-org-2000-09-xmldsigrsa-sha1-with-susta

using System;
using System.Security.Cryptography;

namespace HCore.Identity.Internal
{
    public class RSAPKCS1SHA1SignatureDescription : SignatureDescription
    {
        private readonly string _hashAlgorithm;

        public RSAPKCS1SHA1SignatureDescription()
        {
            KeyAlgorithm = typeof(RSACryptoServiceProvider).FullName;
            DigestAlgorithm = typeof(SHA1).FullName;
            FormatterAlgorithm = typeof(RSAPKCS1SignatureFormatter).FullName;
            DeformatterAlgorithm = typeof(RSAPKCS1SignatureDeformatter).FullName;
            _hashAlgorithm = "SHA1";
        }

        public override AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var deformatter = new RSAPKCS1SignatureDeformatter(key);
            deformatter.SetHashAlgorithm(_hashAlgorithm);
            return deformatter;
        }

        public override AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var formatter = new RSAPKCS1SignatureFormatter(key);
            formatter.SetHashAlgorithm(_hashAlgorithm);
            return formatter;
        }
    }
}
