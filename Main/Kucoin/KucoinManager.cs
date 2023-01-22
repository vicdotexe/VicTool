using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CryptoExchange.Net.Sockets;
using Kucoin.Net.Clients;
using Kucoin.Net.Enums;
using Kucoin.Net.Objects;
using Kucoin.Net.Objects.Models.Spot;
using Kucoin.Net.Objects.Models.Spot.Socket;
using VicTool.Annotations;
using VicTool.Controls;

namespace VicTool.Main.Kucoin
{

    public class KucoinManager : INotifyPropertyChanged
    {
        
        
        public ArbData _arbData;
        private static List<Arbitrage> _arbs = new List<Arbitrage>();
        
        #region DataBindings



        private decimal _mainBalance;

        public decimal MainBalance
        {
            get => _mainBalance;
            set
            {
                _mainBalance = value;
                OnPropertyChanged();
            }
        }

        private decimal _tradingBalance;

        public decimal TradingBalance
        {
            get => _tradingBalance;
            set
            {
                _tradingBalance = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public static void HandleArbitrage(Arbitrage arb)
        {
            _arbs.Add(arb);
            Com.WriteLine("Watching Kucoin for deposit of BNB...");
        }
        public KucoinManager()
        {
            Core.SoldFromPancake += SoldFromPancake;
        }

        private void SoldFromPancake(object sender, ArbData e)
        {
            _arbData = e;
            Com.WriteLine("arbData Set.. watching for balance update");
        }


        public void Initialize()
        {
            Core.Kucoin.SocketClient.SpotStreams.SubscribeToBalanceUpdatesAsync(OnBalanceUpdate);
            Core.Kucoin.SocketClient.SpotStreams.SubscribeToOrderUpdatesAsync(OnOrderUpdate,OnTradeData);

            //Task.Factory.StartNew(GetBalances);
        }

        private void OnOrderUpdate(DataEvent<KucoinStreamOrderBaseUpdate> baseUpdate )
        {

            for (int i = 0; i < _arbs.Count; i++)
            {
                if (_arbs[i].IsKucoinActive && !_arbs[i].Failed)
                    _arbs[i].OnKucoinOrderUpdate(baseUpdate);
            }
        }



        private void OnTradeData(DataEvent<KucoinStreamOrderMatchUpdate> matchUpdate)
        {
            for (int i = 0; i < _arbs.Count; i++)
            {
                if (_arbs[i].IsKucoinActive && !_arbs[i].Failed)
                    _arbs[i].OnKucoinMatchUpdate(matchUpdate);
            }
        }

        private void OnBalanceUpdate(DataEvent<KucoinBalanceUpdate> update)
        {
            for (int i = 0; i < _arbs.Count; i++)
            {
                if (_arbs[i].IsKucoinActive && !_arbs[i].Failed)
                    _arbs[i].OnKucoinBalanceUpdate(update);
            }
        }

        /*
        private async Task GetBalances()
        {
            while (true)
            {
                var asset = CurrentPair.Split('-')[0];
                var result = await Core.Kucoin.RestClient.SpotApi.Account.GetAccountsAsync(asset);
                
                if(result.Success)
                    foreach (var element in result.Data)
                    {
                        if (element.Type == AccountType.Main)
                            MainBalance = element.Total;
                        if (element.Type == AccountType.Trade)
                            TradingBalance = element.Total;
                    }

                await Task.Delay(1000);
            }
            
        }
        */
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


}
