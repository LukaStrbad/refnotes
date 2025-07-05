using System.Text;
using System.Text.Json.Serialization;

namespace Api;

public class AppConfiguration
{
    public string JwtPrivateKey { get; set; } = "";
    [JsonIgnore] public byte[] JwtPrivateKeyBytes => Encoding.UTF8.GetBytes(JwtPrivateKey);

    [JsonIgnore]
    public string BaseDir { set; get; } = ".";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string DataDir { get; set; } = "";
}
