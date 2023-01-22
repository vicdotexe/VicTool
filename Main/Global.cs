using System;
using System.Collections.Generic;
using System.Numerics;
using Kucoin.Net.Objects;
using Newtonsoft.Json;
using VicTool.Controls;
using VicTool.Main.Eth;
using VicTool.Main.EVM;
using VicTool.Main.Misc;

namespace VicTool.Main
{
    public static class Global
    {
        public static readonly string NullAddress = "0x0000000000000000000000000000000000000000";
        public static class Paths
        {
            public static string AccountsPath = "Files/accountlist.json";
            public static string NetworksPath = "Files/networklist.json";
            public static string TokensPath = "Files/tokens.json";
            public static string DexsPath = "Files/dexs.json";

            public static List<Web3Network> GetNetworksFromFile()
            {
                FileHelper.DeserializeJsonFile(NetworksPath, out List<Web3Network> networks);
                return networks;
            }

            public static List<EvmNetwork> GetNetworksFromFileEVM()
            {
                FileHelper.DeserializeJsonFile(NetworksPath, out List<EvmNetwork> networks);
                return networks;
            }

            public static List<VicAccount> GetAccountsFromFile()
            {
                FileHelper.DeserializeJsonFile(AccountsPath, out List<VicAccount> accounts);
                return accounts;
            }
            public static Dictionary<string, List<Web3Token>> GetTokensFromFile()
            {
                FileHelper.DeserializeJsonFile(TokensPath, out Dictionary<string, List<Web3Token>> tokens);
                return tokens;
            }

            public static Dictionary<string, List<Web3DexInfo>> GetDexsFromFile()
            {
                FileHelper.DeserializeJsonFile(DexsPath, out Dictionary<string, List<Web3DexInfo>> dexs);
                return dexs;
            }

            public static List<Web3DexInfo> GetDexsFromFileByNetwork(Web3Network network)
            {
                var dict = GetDexsFromFile();
                if (!dict.ContainsKey(network.ChainId))
                    dict.Add(network.ChainId, new List<Web3DexInfo>());
                return dict[network.ChainId];
            }
            public static List<Web3DexInfo> GetDexsFromFileByNetwork(string chainId)
            {
                var dict = GetDexsFromFile();
                if (!dict.ContainsKey(chainId))
                    dict.Add(chainId, new List<Web3DexInfo>());
                return dict[chainId];
            }
            public static List<Web3Token> GetTokensFromFileByNetwork(Web3Network network)
            {
                var dict =  GetTokensFromFile();
                if (!dict.ContainsKey(network.ChainId))
                    dict.Add(network.ChainId,new List<Web3Token>());
                return dict[network.ChainId];
            }
            public static List<Web3Token> GetTokensFromFileByNetwork(string chainId)
            {
                var dict = GetTokensFromFile();
                if (!dict.ContainsKey(chainId))
                    dict.Add(chainId, new List<Web3Token>());
                return dict[chainId];
            }
            public static List<EvmToken> GetTokensFromFileByNetworkEVM(string chainId)
            {
                FileHelper.DeserializeJsonFile(TokensPath, out Dictionary<string, List<EvmToken>> tokens);
                if (!tokens.ContainsKey(chainId))
                    tokens.Add(chainId, new List<EvmToken>());
                return tokens[chainId];
            }
        }
        public static decimal ToPositiveInfinity(this decimal value, int decimals) { var decimalPlaces = Convert.ToDecimal(Math.Pow(10, decimals)); return Math.Ceiling(value * decimalPlaces) / decimalPlaces; }
        public static decimal ToNegativeInfinity(this decimal value, int decimals) { var decimalPlaces = Convert.ToDecimal(Math.Pow(10, decimals)); return Math.Floor(value * decimalPlaces) / decimalPlaces; }
        public static class Addresses
        {
            public const string BSCTestNetAddress = "https://data-seed-prebsc-1-s1.binance.org:8545";
            public const string BSCMainnet = "https://bsc-dataseed.binance.org/";
            public const string PancakeRouterTestnet = "0xD99D1c33F9fC3444f8101754aBC46c52416550D1";
            public const string PancakeRouterTestnet2 = "0x9Ac64Cc6e4415144C455BD8E4837Fea55603e5c3";
            public const string PancakeRouterMainnet = "0x10ED43C718714eb63d5aA57B78B54704E256024E";
            public const string PanckeFactoryMainnet = "0xcA143Ce32Fe78f1f7019d7d551a6402fC5350c73";
            public const string BotTestWalletPrivate = "";
            public const string BotTestWalletPublic = "";
            public const string BSCTestWalletPublic = "";
            public static BigInteger BSCTestNetChainId = new BigInteger(97);

        }

        public static class Contracts
        {
            public static class TestNet
            {
                public const string WBNB = "0xae13d989daC2f0dEbFf460aC112a837C89BAa7cd";
                public const string Rex10 = "0x6664ff504EAEBFe1E61DB4e1D3F73170c8210370";
                public const string USDT = "0x337610d27c682E347C9cD60BD4b3b107C9d34dDd";
                public const string USDT2 = "0x7ef95a0fee0dd31b22626fa2e10ee6a223f8a684";
            }

            public static class MainNet
            {
                public const string WBNB = "0xbb4CdB9CBd36B01bD1cBaEBF2De08d9173bc095c";
                public const string BUSD = "0xe9e7CEA3DedcA5984780Bafc599bD69ADd087D56";
                public const string SOUL = "0x298Eff8af1ecEbbB2c034eaA3b9a5d0Cc56c59CD";
                public const string LPSOUL = "0xEEf898a863b4689EfABEFE35eBb9231e27f443a5";
            }

            public static List<string> CreatePair(string tokenA, string tokenB)
            {
                return new List<string>() { tokenA, tokenB };
            }
        }

        public static KucoinApiCredentials GetKucoinCredentials()
        {
            string apiSecret = "";
            string apiKey = "";
            string apiPassphrase = "";
            return new KucoinApiCredentials(apiKey, apiSecret, apiPassphrase);
        }
    }
}
