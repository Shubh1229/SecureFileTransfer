using System.Security.Cryptography;

namespace SecureFileTransfer.src.security
{
    public static class EncryptionService
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public static byte[] Encrypt(byte[] plainBytes, SessionKeyModel sessionKey)
        {
            byte[] nonce = sessionKey.NextNonce();
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            using AesGcm aes = new(sessionKey.Key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            return nonce.Concat(tag).Concat(cipherBytes).ToArray();
        }

        public static byte[] Decrypt(byte[] encryptedBytes, byte[] key)
        {
            byte[] nonce = encryptedBytes[..NonceSize];
            byte[] tag = encryptedBytes[NonceSize..(NonceSize + TagSize)];
            byte[] cipherBytes = encryptedBytes[(NonceSize + TagSize)..];

            byte[] plainBytes = new byte[cipherBytes.Length];

            using AesGcm aes = new(key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return plainBytes;
        }
    }
}