using System.Security.Cryptography;

namespace SecureFileTransfer.src.security
{
    public static class KeyExchangeService
    {
        public static ECDiffieHellman CreateKeyPair()
        {
            return ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        }

        public static byte[] ExportPublicKey(ECDiffieHellman keyPair)
        {
            return keyPair.PublicKey.ExportSubjectPublicKeyInfo();
        }

        public static byte[] DeriveSharedKey(ECDiffieHellman keyPair, byte[] remotePublicKeyBytes)
        {
            using ECDiffieHellman remotePublicKey = ECDiffieHellman.Create();
            remotePublicKey.ImportSubjectPublicKeyInfo(remotePublicKeyBytes, out _);

            return keyPair.DeriveKeyFromHash(remotePublicKey.PublicKey, HashAlgorithmName.SHA256);
        }

        // SHA-256 of the DER-encoded SubjectPublicKeyInfo bytes, formatted as colon-separated hex pairs.
        // Example: "ab:12:cd:34:..."  (matches SSH fingerprint style)
        public static string ComputeFingerprint(byte[] publicKeyBytes)
        {
            byte[] hash = SHA256.HashData(publicKeyBytes);
            return string.Join(":", hash.Select(b => b.ToString("x2")));
        }
    }
}