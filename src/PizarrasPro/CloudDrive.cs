using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.Identity.Client;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Upload;
using System.Security.Cryptography;

namespace PizarrasPro;

public sealed record CloudFolder(string Id, string Name)
{
    public override string ToString() => Name;
}

// Explicitly memory-only: a USB never carries account tokens to another computer.
internal sealed class MemoryTokenStore : IDataStore
{
    private readonly Dictionary<string, object> values = [];
    public Task StoreAsync<T>(string key, T value) { values[key] = value!; return Task.CompletedTask; }
    public Task DeleteAsync<T>(string key) { values.Remove(key); return Task.CompletedTask; }
    public Task<T> GetAsync<T>(string key) => Task.FromResult(values.TryGetValue(key, out var value) ? (T)value : default!);
    public Task ClearAsync() { values.Clear(); return Task.CompletedTask; }
}

public sealed class CloudDrive(string service) : IDisposable
{
    public string Service { get; } = service;
    public string Account { get; private set; } = "Sin conectar";
    public CloudFolder? Folder { get; set; }
    public bool Connected => microsoft is not null || google is not null;
    private IPublicClientApplication? microsoft;
    private DriveService? google;
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(3) };
    private static readonly string[] Scopes = ["Files.ReadWrite"];
    public async Task Connect(string clientId, string? googleConfiguration, CancellationToken cancellation)
    {
        Disconnect();
        if (Service == "OneDrive")
        {
            if (!Guid.TryParse(clientId, out _)) throw new InvalidDataException("Introduce el identificador de aplicación de Microsoft Entra.");
            var client = PublicClientApplicationBuilder.Create(clientId).WithAuthority(AzureCloudInstance.AzurePublic, AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount).WithRedirectUri("http://localhost").Build();
            await client.AcquireTokenInteractive(Scopes).WithUseEmbeddedWebView(false).ExecuteAsync(cancellation);
            microsoft = client;
            try { var info = await Graph(HttpMethod.Get, "/me/drive", null, cancellation); Account = info["owner"]?["user"]?["displayName"]?.GetValue<string>() ?? "OneDrive"; }
            catch { Disconnect(); throw; }
        }
        else
        {
            var json = JsonNode.Parse(googleConfiguration ?? "{}");
            if (json?["installed"] is not JsonObject installed) throw new InvalidDataException("El JSON OAuth de Google debe ser de tipo Escritorio (installed).");
            var secrets = new ClientSecrets { ClientId = installed["client_id"]!.GetValue<string>(), ClientSecret = installed["client_secret"]!.GetValue<string>() };
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, [DriveService.Scope.Drive], "sesion", cancellation, new MemoryTokenStore());
            var client = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential, ApplicationName = AppVersion.Name });
            try { var request = client.About.Get(); request.Fields = "user(displayName,emailAddress)"; var info = await request.ExecuteAsync(cancellation); Account = info.User.EmailAddress ?? info.User.DisplayName; google = client; }
            catch { client.Dispose(); throw; }
        }
    }
    private async Task<JsonObject> Graph(HttpMethod method, string path, HttpContent? content, CancellationToken cancellation)
    {
        if (microsoft is null) throw new InvalidOperationException("Conecta OneDrive primero.");
        var account = (await microsoft.GetAccountsAsync()).FirstOrDefault();
        var token = await microsoft.AcquireTokenSilent(Scopes, account).ExecuteAsync(cancellation);
        var url = path.StartsWith('/') ? "https://graph.microsoft.com/v1.0" + path : path;
        if (!url.StartsWith("https://graph.microsoft.com/v1.0/", StringComparison.Ordinal)) throw new InvalidDataException("Respuesta de paginación no válida.");
        using var request = new HttpRequestMessage(method, url) { Content = content }; request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await http.SendAsync(request, cancellation);
        if (!response.IsSuccessStatusCode) throw new IOException($"OneDrive respondió {(int)response.StatusCode}. Revisa permisos, conexión y espacio. El original se conserva.");
        return JsonNode.Parse(await response.Content.ReadAsStringAsync(cancellation))!.AsObject();
    }
    public async Task<List<CloudFolder>> Folders(string parent, CancellationToken cancellation)
    {
        var result = new List<CloudFolder>();
        if (Service == "OneDrive")
        {
            string? path = parent == "root" ? "/me/drive/root/children" : "/me/drive/items/" + Uri.EscapeDataString(parent) + "/children";
            while (path is not null)
            {
                var response = await Graph(HttpMethod.Get, path, null, cancellation);
                foreach (var item in response["value"]!.AsArray()) if (item?["folder"] is not null) result.Add(new(item["id"]!.GetValue<string>(), item["name"]!.GetValue<string>()));
                path = response["@odata.nextLink"]?.GetValue<string>();
            }
        }
        else
        {
            if (google is null) throw new InvalidOperationException("Conecta Google Drive primero.");
            string? token = null;
            do
            {
                var request = google.Files.List(); request.Q = $"'{parent.Replace("\\", "\\\\").Replace("'", "\\'")}' in parents and mimeType = 'application/vnd.google-apps.folder' and trashed = false";
                request.Fields = "nextPageToken,files(id,name)"; request.PageSize = 1000; request.PageToken = token;
                var response = await request.ExecuteAsync(cancellation); result.AddRange(response.Files.Select(f => new CloudFolder(f.Id, f.Name))); token = response.NextPageToken;
            } while (token is not null);
        }
        return result.OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
    public async Task Upload(string path, CancellationToken cancellation)
    {
        if (Folder is null) throw new InvalidOperationException("Elige una carpeta remota.");
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var size = input.Length;
        if (Service == "OneDrive")
        {
            if (size > 250L * 1024 * 1024) throw new IOException("La subida simple de OneDrive admite hasta 250 MB. Se conserva el original.");
            var target = Folder.Id == "root" ? "/me/drive/root" : "/me/drive/items/" + Uri.EscapeDataString(Folder.Id);
            // A conflict must fail instead of overwriting an existing classroom document.
            var item = await Graph(HttpMethod.Put, target + ":/" + Uri.EscapeDataString(Path.GetFileName(path)) + ":/content?%40microsoft.graph.conflictBehavior=fail", new StreamContent(input), cancellation);
            var id = item["id"]?.GetValue<string>() ?? throw new IOException("No se recibió el identificador remoto.");
            var verified = await Graph(HttpMethod.Get, "/me/drive/items/" + Uri.EscapeDataString(id), null, cancellation);
            if (verified["size"]?.GetValue<long>() != size) throw new IOException("No se confirmó el tamaño remoto. Se conserva el original.");
        }
        else
        {
            if (google is null) throw new InvalidOperationException("Conecta Google Drive primero.");
            var md5 = Convert.ToHexString(await MD5.HashDataAsync(input, cancellation)).ToLowerInvariant(); input.Position = 0;
            var metadata = new Google.Apis.Drive.v3.Data.File { Name = Path.GetFileName(path), Parents = new[] { Folder.Id } };
            var upload = google.Files.Create(metadata, input, "application/pdf"); upload.Fields = "id,size,md5Checksum";
            var progress = await upload.UploadAsync(cancellation);
            if (progress.Status != UploadStatus.Completed || upload.ResponseBody?.Id is null) throw new IOException("No se completó la subida. Se conserva el original.");
            var request = google.Files.Get(upload.ResponseBody.Id); request.Fields = "id,size,md5Checksum"; var verified = await request.ExecuteAsync(cancellation);
            if (verified.Size != size || verified.Md5Checksum != md5) throw new IOException("La copia remota no coincide. Se conserva el original.");
        }
    }
    public void Disconnect() { microsoft = null; google?.Dispose(); google = null; Folder = null; Account = "Sin conectar"; }
    public void Dispose() { Disconnect(); http.Dispose(); }
}
