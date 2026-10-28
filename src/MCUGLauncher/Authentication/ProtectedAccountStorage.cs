using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using XboxAuthNet.Game.Accounts.JsonStorage;

namespace BlockLaunch;

public sealed class ProtectedAccountStorage(string file) : IJsonStorage
{
    static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BlockLaunch.Accounts.v1");

    public JsonNode? ReadAsJsonNode()
    {
        if (!File.Exists(file))
            return null;
        byte[]? plain = null;
        try
        {
            plain = ProtectedData.Unprotect(File.ReadAllBytes(file), Entropy, DataProtectionScope.CurrentUser);
            return JsonNode.Parse(plain) ?? throw new JsonException("Empty account data");
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            throw new AccountStorageException("账号缓存无法解密或已损坏。请在账号管理中清除本地登录，再重新登录。原文件已保留。", ex);
        }
        finally
        {
            if (plain != null)
                CryptographicOperations.ZeroMemory(plain);
        }
    }

    public void Write(JsonNode node, JsonSerializerOptions? serializerOptions)
    {
        var plain = Encoding.UTF8.GetBytes(node.ToJsonString(serializerOptions));
        byte[] encrypted;
        try
        {
            encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(file))!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".accounts-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(encrypted);
                stream.Flush(true);
            }

            File.Move(temporary, file, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    public void Forget()
    {
        Write(new JsonObject(), null);
    }
}

internal sealed class MemoryAccountStorage : IJsonStorage
{
    public JsonNode? Value
    {
        get;
        private set;
    }

    public JsonNode? ReadAsJsonNode() => Value?.DeepClone();
    public void Write(JsonNode node, JsonSerializerOptions? serializerOptions) => Value = node.DeepClone();
}
