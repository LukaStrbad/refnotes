using System.Text.Json.Serialization;

namespace Server.Model;

[JsonSerializable(typeof(UserFile))]
[JsonSerializable(typeof(UserDirectory))]
[JsonSerializable(typeof(User))]
internal partial class ModelJsonSerializerContext : JsonSerializerContext;
