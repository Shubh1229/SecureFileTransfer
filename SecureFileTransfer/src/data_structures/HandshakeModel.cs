using System.Text.Json;

namespace SecureFileTransfer.src.data_structures
{
    public class HandshakeModel
    {
        public string Type { get; set; } = "handshake";
        public string SenderName { get; set; } = "";
        public string SenderIPv4 { get; set; } = "";
        public string SenderIPv6 { get; set; } = "";

        public int SenderPort { get; set; } = 5000;

        public string ToJson()
        {
            return JsonSerializer.Serialize<HandshakeModel>(this);
        }
        public static HandshakeModel? FromJson(string json)
        {
            return JsonSerializer.Deserialize<HandshakeModel>(json);
        }
    }
}