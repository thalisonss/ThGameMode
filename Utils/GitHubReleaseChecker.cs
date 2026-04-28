using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Consulta a release mais recente do projeto no GitHub e compara com a versão em execução.
    /// </summary>
    internal sealed class GitHubReleaseChecker
    {
        private const string Owner = "thalisonss";
        private const string Repository = "ThGameMode";
        private static readonly Uri LatestReleaseApiUri = new($"https://api.github.com/repos/{Owner}/{Repository}/releases/latest");

        /// <summary>
        /// Busca a última release publicada e devolve um resultado pronto para a UI consumir.
        /// </summary>
        public async Task<ReleaseCheckResult> CheckForUpdateAsync(Version currentVersion, CancellationToken cancellationToken = default)
        {
            using var client = CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApiUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ReleaseCheckResult.NotAvailable(currentVersion, "Nenhuma release publicada foi encontrada no GitHub.");

            response.EnsureSuccessStatusCode();

            // A API do GitHub devolve muito mais dados do que usamos; desserializamos só o necessário.
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var release = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (release == null || string.IsNullOrWhiteSpace(release.TagName) || string.IsNullOrWhiteSpace(release.HtmlUrl))
                return ReleaseCheckResult.NotAvailable(currentVersion, "A resposta da release veio incompleta.");

            if (!TryParseVersion(release.TagName, out var latestVersion))
                return ReleaseCheckResult.NotAvailable(currentVersion, $"Nao foi possivel interpretar a versao da tag '{release.TagName}'.");

            return new ReleaseCheckResult(
                latestVersion > currentVersion,
                currentVersion,
                latestVersion,
                release.TagName,
                release.Name,
                release.HtmlUrl,
                release.Body,
                null);
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                // Timeout curto para não travar a experiência da bandeja ao consultar a internet.
                Timeout = TimeSpan.FromSeconds(8)
            };

            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"ThGameMode/{version}");
            return client;
        }

        /// <summary>
        /// Normaliza tags como "v1.2.3-beta" para um formato que <see cref="Version"/> consiga interpretar.
        /// </summary>
        private static bool TryParseVersion(string tagName, out Version version)
        {
            version = new Version(0, 0, 0, 0);

            if (string.IsNullOrWhiteSpace(tagName))
                return false;

            string normalized = tagName.Trim();
            if (normalized.StartsWith("v", true, CultureInfo.InvariantCulture))
                normalized = normalized[1..];

            int suffixIndex = normalized.IndexOfAny(['-', '+']);
            if (suffixIndex >= 0)
                normalized = normalized[..suffixIndex];

            if (!Version.TryParse(normalized, out var parsedVersion) || parsedVersion == null)
                return false;

            version = parsedVersion;
            return true;
        }

        /// <summary>
        /// Recorte mínimo do JSON da API de releases do GitHub.
        /// </summary>
        private sealed class GitHubReleaseResponse
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("html_url")]
            public string? HtmlUrl { get; set; }

            [JsonPropertyName("body")]
            public string? Body { get; set; }
        }
    }

    /// <summary>
    /// Resultado consolidado da verificação de atualização.
    /// </summary>
    internal sealed record ReleaseCheckResult(
        bool IsUpdateAvailable,
        Version CurrentVersion,
        Version? LatestVersion,
        string? LatestTag,
        string? ReleaseName,
        string? ReleaseUrl,
        string? ReleaseNotes,
        string? FailureReason)
    {
        /// <summary>
        /// Cria um resultado de falha controlada quando a release não pode ser validada.
        /// </summary>
        public static ReleaseCheckResult NotAvailable(Version currentVersion, string reason) =>
            new(false, currentVersion, null, null, null, null, null, reason);
    }
}
