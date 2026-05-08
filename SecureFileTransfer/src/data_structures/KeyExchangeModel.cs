namespace SecureFileTransfer.src.data_structures
{
    public class KeyExchangeModel
    {
        public string Type { get; set; } = "key_exchange";
        public string PublicKeyBase64 { get; set; } = "";
        public string IdentityPublicKeyBase64 { get; set; } = "";
    }
}