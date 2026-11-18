using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Deskmates.Data.Live
{
    /// <summary>
    /// Talks to the Fretlings companion running on the user's computer, over the local network. The companion holds
    /// the account tokens and the coding-agent hooks; the headset only reads item lists and sends actions.
    /// </summary>
    public sealed class CompanionClient : IDisposable
    {
        const string Beacon = "FRETLINGS";

        readonly DataSettings settings;
        readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        string baseUrl;

        public CompanionClient(DataSettings settings)
        {
            this.settings = settings;
            if (!string.IsNullOrWhiteSpace(settings.companionHost))
            {
                baseUrl = $"http://{settings.companionHost.Trim()}:{settings.companionPort}";
            }
        }

        public bool IsConnected { get; private set; }

        public async Task<string> GetAsync(string path, CancellationToken ct)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Get, path, ct);
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> PostAsync(string path, CancellationToken ct)
        {
            HttpResponseMessage response = await SendAsync(HttpMethod.Post, path, ct);
            return response.IsSuccessStatusCode;
        }

        async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, CancellationToken ct)
        {
            if (baseUrl == null)
            {
                baseUrl = await DiscoverAsync(ct);
            }

            HttpRequestMessage request = new HttpRequestMessage(method, baseUrl + path);
            request.Headers.Add("X-Fretlings-Pairing", settings.pairingCode ?? "");
            try
            {
                HttpResponseMessage response = await http.SendAsync(request, ct);
                IsConnected = response.IsSuccessStatusCode;
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException)
            {
                IsConnected = false;
                if (string.IsNullOrWhiteSpace(settings.companionHost))
                {
                    // The computer may have changed address: look for it again next time.
                    baseUrl = null;
                }

                throw;
            }
        }

        // The companion broadcasts "FRETLINGS <port>" every few seconds on the local network.
        async Task<string> DiscoverAsync(CancellationToken ct)
        {
            using UdpClient listener = new UdpClient(settings.discoveryPort) { EnableBroadcast = true };
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            Task<UdpReceiveResult> receive = listener.ReceiveAsync();
            while (true)
            {
                Task finished = await Task.WhenAny(receive, Task.Delay(Timeout.Infinite, timeout.Token));
                if (finished != receive)
                {
                    throw new HttpRequestException("No Fretlings companion found on the local network.");
                }

                UdpReceiveResult result = receive.Result;
                string[] parts = Encoding.UTF8.GetString(result.Buffer).Split(' ');
                if (parts.Length >= 2 && parts[0] == Beacon && int.TryParse(parts[1], out int port))
                {
                    IPAddress address = result.RemoteEndPoint.Address;
                    Debug.Log($"[Deskmates] Companion found at {address}:{port}.");
                    return $"http://{address}:{port}";
                }

                receive = listener.ReceiveAsync();
            }
        }

        public void Dispose() => http.Dispose();
    }
}
