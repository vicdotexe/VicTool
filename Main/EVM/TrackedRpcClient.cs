using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Newtonsoft.Json;

namespace VicTool.Main.EVM
{
    public class TrackedRpcClient : RpcClient
    {
        public static int CountTotal { get; private set; }
        public static Dictionary<string, int> Counts = new();
        private string url;
        private static Stopwatch _stopwatchTotal;
        public static double TotalTime => _stopwatchTotal?.ElapsedMilliseconds ?? 0;

        public TrackedRpcClient(Uri baseUrl, AuthenticationHeaderValue authHeaderValue = null, JsonSerializerSettings jsonSerializerSettings = null, HttpClientHandler httpClientHandler = null, ILog log = null) : base(baseUrl, authHeaderValue, jsonSerializerSettings, httpClientHandler, log)
        {

            if (!Counts.ContainsKey(baseUrl.OriginalString))
            {
                Counts.Add(baseUrl.OriginalString, 0);
            }

            url = baseUrl.OriginalString;
        }

        public TrackedRpcClient(Uri baseUrl, HttpClient httpClient, AuthenticationHeaderValue authHeaderValue = null, JsonSerializerSettings jsonSerializerSettings = null, ILog log = null) : base(baseUrl, httpClient, authHeaderValue, jsonSerializerSettings, log)
        {
            if (!Counts.ContainsKey(baseUrl.OriginalString))
            {
                Counts.Add(baseUrl.OriginalString, 0);
            }
            url = baseUrl.OriginalString;
        }

        protected override Task<RpcResponseMessage> SendAsync(RpcRequestMessage request, string route = null)
        {
            if (_stopwatchTotal == null)
            {
                _stopwatchTotal = new Stopwatch();
                _stopwatchTotal.Start();
            }
            Counts[url] += 1;
            CountTotal += 1;
            return base.SendAsync(request, route);
        }
    }
}
