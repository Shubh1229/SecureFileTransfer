namespace SecureFileTransfer.src.security
{
    public class SessionKeyModel
    {
        public byte[] Key { get; set; } = Array.Empty<byte>();

        public string RemotePublicKeyFingerprint { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsEstablished => Key.Length > 0;
        private long _nonceCounter = 0;

        public byte[] NextNonce()
        {
            long counter = System.Threading.Interlocked.Increment(ref _nonceCounter);

            byte[] nonce = new byte[12];
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(
                nonce.AsSpan(4), counter);

            return nonce;
        }
    }
}