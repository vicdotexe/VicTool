using System.IO;
using System.Net;
using Newtonsoft.Json;
using VicTool.Main.Misc;


/// <summary>
/// Tick Pancakeswap for token prices every 15 seconds from swapcontrol
/// </summary>

namespace VicTool.Main.Trash
{

    public class WebManager
    {
        public static string BSCApi = "EFF3RYJ9ZWU5KKA73U7HYFCD5YQSSZYIGC";
        public static WebClient Client { get; private set; } =
            Client = new WebClient();


        public static decimal BscLatestBnbPrice()
        {
            string url = "https://api.bscscan.com/api?module=stats&action=bnbprice&apikey=" + BSCApi;
            return decimal.Parse(GetObject<BnbPriceObject>(url).result.ethusd);
        }
        public static decimal BSCQueryPool(string lpContract, string tokenContract)
        {
            string url = "https://api.bscscan.com/api?module=account&action=tokenbalance&contractaddress="
                         + tokenContract + "&address=" + lpContract + "&tag=latest&apikey=" + BSCApi;
            var apiObject = GetObject<BSCLPQuery>(url);

            return apiObject.GetTokenTotal();
        }

        public static PSData PancakeTokenQuery(string tokenContract)
        {
            string url = "https://api.pancakeswap.info/api/v2/tokens/" + tokenContract;

            var apiObject = GetObject<PSApi>(url);
            return apiObject.data;
        }

        private static T GetObject<T>(string apiAddress) where T : IWebApiObject
        {
            string psString = null;
            using (Stream stream = Client.OpenRead(apiAddress))
            {
                StreamReader sr = new StreamReader(stream);
                psString = sr.ReadToEnd();
                sr.Close();
            }

            var psObj = JsonConvert.DeserializeObject<T>(psString);
            return psObj;
        }
    }
}
