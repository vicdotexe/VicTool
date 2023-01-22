namespace VicTool.Main.Misc
{
    public interface IWebApiObject
    {

    }

    public class BnbPriceObject : IWebApiObject
    {
        public string status { get; set; }
        public string message { get; set; }
        public BnbPriceObjectData result { get; set; }
    }

    public class BnbPriceObjectData
    {
        public string ethbtc { get; set; }
        public string ethbtc_timestamp { get; set; }
        public string ethusd { get; set; }
        public string ethusd_timestamp { get; set; }
    }

    public class PSApi : IWebApiObject
    {
        public long updated_at { get; set; }
        public PSData data { get; set; }
    }

    public class PSData
    {
        public string name { get; set; }
        public string symbol { get; set; }
        public string price { get; set; }
        public string price_BNB { get; set; }
    }


    public class KCApi
    {
        public string code { get; set; }
        public KCData data { get; set; }
    }

    public class KCData
    {
        public long time { get; set; }
        public string sequence { get; set; }
        public string price { get; set; }
        public string size { get; set; }
        public string bestBid { get; set; }
        public string bestBidSize { get; set; }
        public string bestAsk { get; set; }
        public string bestAskSize { get; set; }
    }


    public class KCOrderBook
    {
        public string code { get; set; }
        public KCOrderBookData data { get; set; }
    }

    public class KCOrderBookData
    {
        public long time { get; set; }
        public string sequence { get; set; }
        public string[][] bids { get; set; }
        public string[][] asks { get; set; }
    }


    public class BSCLPQuery : IWebApiObject
    {
        public string status { get; set; }
        public string message { get; set; }
        public string result { get; set; }

        public decimal GetTokenTotal()
        {
            return decimal.Parse(result);
        }
    }

    public class BSCGetPairContract : IWebApiObject
    {
        public string address { get; set; }

        public bool GetValidity()
        {
            return address != "0x0000000000000000000000000000000000000000";
        }
    }

}
