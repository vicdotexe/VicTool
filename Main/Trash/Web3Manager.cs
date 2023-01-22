namespace VicTool.Main.Trash
{

    
    /*
    public class Web3Manager
    {
        public static Web3 Web3 { get; private set; }
        public static decimal EthPriceUsd { get; private set; }
        public static Token TokenIn { get; private set; }
        public static Token TokenOut { get; private set; }
        private BackgroundWorker _worker;
        private bool _aTokenIsBeingAwaited;

        public static decimal TokenInBalance { get; private set; }
        public static decimal TokenOutBalance { get; private set; }

        private static Dictionary<string, Token> _watchList = new Dictionary<string, Token>();

        public static void Flip()
        {
            (TokenIn, TokenOut) = (TokenOut, TokenIn);
        }

        public Web3Manager()
        {
            Connect(Global.Addresses.BSCMainnet, Global.Addresses.BotTestWalletPrivate);
        }

        public void Initialize(Dispatcher dispatcher)
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += RefreshLoop;
            _worker.RunWorkerAsync();
        }

        public async void SetTokenIn(string contract)
        {
            //_aTokenIsBeingAwaited = true;
            //TokenIn = await Token.Generate(contract);
            //await TokenIn.GenerateEthPairContract();
            //_aTokenIsBeingAwaited = false;
        }

        public async void SetTokenOut(string contract)
        {
            //_aTokenIsBeingAwaited = true;
            //TokenOut = await Token.Generate(contract);
            //await TokenOut.GenerateEthPairContract();
            //_aTokenIsBeingAwaited = false;
        }

        public async void RefreshLoop(object sender, DoWorkEventArgs args)
        {
            while (true)
            {
                await RefreshLoop(null);
                await Task.Delay(5000);
            }
            
        }
        public async Task RefreshLoop(decimal? customEthPrice)
        {
            EthPriceUsd = customEthPrice ?? WebManager.BscLatestBnbPrice();
            if (TokenIn != null)
            {
                if (TokenIn.IsEth)
                {
                    var balance =
                        await Web3Manager.Web3.Eth.GetBalance.SendRequestAsync("");
                TokenInBalance = Web3.Convert.FromWei(balance);
                }
                else
                {
                    var balance = await Web3Manager.Web3.Eth.ERC20.GetContractService(TokenIn.Contract)
                        .BalanceOfQueryAsync("");
                    TokenInBalance = Web3.Convert.FromWei(balance, TokenIn.Decimals);
                }

                await TokenIn.Update(Web3);
            }

            if (TokenOut != null)
            {
                if (TokenOut.IsEth)
                {
                    var balance =
                        await Web3Manager.Web3.Eth.GetBalance.SendRequestAsync("");
                    TokenOutBalance = Web3.Convert.FromWei(balance, TokenOut.Decimals);
                }
                else
                {
                    var balance = await Web3Manager.Web3.Eth.ERC20.GetContractService(TokenOut.Contract)
                        .BalanceOfQueryAsync("");
                    TokenOutBalance = Web3.Convert.FromWei(balance, TokenOut.Decimals);
                }

                await TokenOut.Update(Web3);

            }
            foreach (var token in _watchList.Values)
            {
                await token.Update(Web3);
            }
        }

        public void Connect(string networkUrl, string privateKey = null)
        {
            if (privateKey == null)
                Web3 = new Web3(networkUrl);
            else
                Web3 = new Web3(new Account(privateKey), networkUrl);
        }

        public static async Task<Token> AddToWatchList(string contract)
        {
            var check = _watchList.Values.FirstOrDefault(o => o.Contract == contract);
            if (check != null)
                return check;
            var newToken = await Web3.GenerateToken(contract);
            return newToken;
        }

        async public Task<TransactionReceipt> SwapExactEthForTokens(decimal amountInEth, decimal amountOutMin, Pair pair, string toAddress, double deadlineInMinutes)
        {
            var swapHandler = Web3.Eth.GetContractTransactionHandler<SwapExactETHForTokensFunction>();
            var swap = new SwapExactETHForTokensFunction()
            {
                AmountToSend = Web3.Convert.ToWei(amountInEth),
                AmountOutMin = Web3.Convert.ToWei(amountOutMin, 8),
                Path = pair,
                To = toAddress,
                Deadline = DateTimeOffset.Now.AddMinutes(5).ToUnixTimeSeconds()
            };

            var gasPrice = Web3.Convert.ToWei(10, UnitConversion.EthUnit.Gwei);
            var estimate = await swapHandler.EstimateGasAsync(Global.Addresses.PancakeRouterMainnet, swap);
            var nonce = await Web3.TransactionManager.Account.NonceService.GetNextNonceAsync();

            swap.Nonce = nonce;
            swap.GasPrice = gasPrice;
            swap.Gas = estimate.Value;

            Console.WriteLine("Signing Transaction - Nonce:" + swap.Nonce);
            await swapHandler.SignTransactionAsync(Global.Addresses.PancakeRouterMainnet, swap);

            Console.WriteLine("Sending Transaction - Nonce:" + swap.Nonce);
            var receipt =
                await swapHandler.SendRequestAndWaitForReceiptAsync(Global.Addresses.PancakeRouterMainnet, swap);


            return receipt;
        }

        #region Static Uniswap Functions
        public static async Task<decimal> GetAccountBnbBalance(string publicAddress)
        {

            Console.WriteLine("Fetching Balance");
            var balance = await Web3.Eth.GetBalance.SendRequestAsync(publicAddress);
            Console.WriteLine($"Balance in Wei: {balance.Value}");

            var etherAmount = Web3.Convert.FromWei(balance.Value);
            Console.WriteLine($"Balance in BNB: {etherAmount}");
            return etherAmount;
        }

        public static async Task<string> GetPairContract(string tokenA, string tokenB)
        {
            var getPair = new GetPairFunction()
            {
                TokenA = tokenA,
                TokenB = tokenB
            };
            var getPairHandler = Web3.Eth.GetContractQueryHandler<GetPairFunction>();

            var result = await getPairHandler.QueryAsync<string>(Global.Addresses.PanckeFactoryMainnet, getPair);
            return result;
        }

        public static async Task<TokenData> GetTokenData(string contract)
        {
            var decimals = await Web3.Eth.ERC20.GetContractService(contract).DecimalsQueryAsync();
            var name = await Web3.Eth.ERC20.GetContractService(contract).NameQueryAsync();
            var ticker = await Web3.Eth.ERC20.GetContractService(contract).SymbolQueryAsync();
            return new TokenData() { Name = name, Ticker = ticker, Decimals = decimals, Contract = contract};
        }

        public static async Task<decimal> GetAmountsOut(decimal amountIn, string tokenIn, string tokenOut, bool routeThroughEth = false)
        {
            if (routeThroughEth)
            {
                var ethIn = await GetAmountsOut(amountIn, tokenIn, Global.Contracts.MainNet.WBNB);

                return await GetAmountsOut(ethIn, Global.Contracts.MainNet.WBNB, tokenOut);
            }
            var decimalsIn = await Web3.Eth.ERC20.GetContractService(tokenIn).DecimalsQueryAsync();
            var decimalsOut = await Web3.Eth.ERC20.GetContractService(tokenOut).DecimalsQueryAsync();

            var message = new GetAmountsOutFunction()
            {
                AmountIn = Web3.Convert.ToWei(amountIn, decimalsIn),
                Path = new List<string> { tokenIn, tokenOut }
            };
            
            var handler = Web3.Eth.GetContractQueryHandler<GetAmountsOutFunction>();
            var amounts = await handler.QueryAsync<List<BigInteger>>(Global.Addresses.PancakeRouterMainnet, message);

            return Web3.Convert.FromWei(amounts.Last(), decimalsOut);
        }


        #endregion

    }
    */
}