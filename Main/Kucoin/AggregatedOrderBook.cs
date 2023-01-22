using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CryptoExchange.Net.Objects;
using Kucoin.Net.Objects;
using Kucoin.Net.SymbolOrderBooks;
using Microsoft.Extensions.Logging;
using VicTool.Main.Misc;

namespace VicTool.Main.Kucoin
{
    public class AggregatedOrderBook
    {

        public BindingList<BookEntry> Asks => askList;
        public BindingList<BookEntry> Bids => bidList;
        private BindingList<BookEntry> askList = new BindingList<BookEntry>();
        private BindingList<BookEntry> bidList = new BindingList<BookEntry>();

        private List<BookEntry> _askList = new List<BookEntry>();
        private List<BookEntry> _bidList = new List<BookEntry>();

        private KucoinSpotSymbolOrderBook _book;
        public KucoinSpotSymbolOrderBook Book => _book;
        private int _limit = 150;
        private int _bindingLimit = 20;

        public AggregatedOrderBook(string symbol)
        {
            
            for (int i = 0; i <= _limit+1; i++)
            {
                askList.Add(new BookEntry());
                bidList.Add(new BookEntry());
                _askList.Add(new BookEntry());
                _bidList.Add(new BookEntry());
            }
            
            SetPair(symbol);
        }

        public string Asset { get; private set; }
        public string Base { get; private set; }
        public void SetPair(string pair)
        {

            if (_book != null)
            {
                _book.StopAsync();
            }

            var options = new KucoinOrderBookOptions()
            {
                //Limit = 50,
                LogLevel = LogLevel.Trace
            };
            _book = new KucoinSpotSymbolOrderBook(pair, options);
            
            _book.StartAsync();
            var split = pair.Split('-');
            Asset = split[0];
            Base = split[1];
        }

        private void EnsureListSizes()
        {
            var askCount = _book.AskCount;
            var bidCount = _book.BidCount;
            while (_askList.Count <= askCount)
                _askList.Add(new BookEntry());
            while (askList.Count <= askCount)
                askList.AddNew();
            while (_bidList.Count <= bidCount)
                _bidList.Add(new BookEntry());
            while (bidList.Count <= bidCount)
                bidList.AddNew();
        }

        private int _lastDecimals;
        public bool Aggregate(int priceDecimals, int roundQuantityDecimals)
        {
            if (_lastDecimals != priceDecimals)
            {
                _askList.Clear();
                _bidList.Clear();
                askList.Clear();
                bidList.Clear();
            }
            _lastDecimals = priceDecimals;

            if (_book?.Status != OrderBookStatus.Synced)
                return false;
            EnsureListSizes();
            decimal total = 0;
            int index = 0;

            foreach (var ask in _askList)
            {
                ask.Clear();
            }

            var lastPrice = _book.Asks.First().Price.ToPositiveInfinity(priceDecimals);
            if (_askList.Count == 0)
                _askList.Add(new BookEntry());
            _askList[0].Price = lastPrice;
            int limitCounter = 0;
            foreach (var ask in _book.Asks)
            {
                if (limitCounter++ >= _limit)
                    break;
                var rounded = ask.Price.ToPositiveInfinity(priceDecimals);


                if (lastPrice != rounded)
                {
                    index++;
                    if (_askList.Count == index)
                        _askList.Add(new BookEntry());
                    _askList[index].Price = rounded;
                    lastPrice = rounded;
                }

                _askList[index].Quantity += ask.Quantity;

                total += ask.Quantity;
                _askList[index].Total = total;
            }

            total = 0;
            index = 0;
            foreach (var bid in _bidList)
            {
                bid.Clear();
            }
            if (_bidList.Count == 0)
                _bidList.Add(new BookEntry());
            lastPrice = _book.Bids.First().Price.ToNegativeInfinity(priceDecimals);
            _bidList[0].Price = lastPrice;
            limitCounter = 0;
            foreach (var bid in _book.Bids)
            {
                if (limitCounter++ >= _limit)
                    break;
                var rounded = bid.Price.ToNegativeInfinity(priceDecimals);

                if (lastPrice != rounded)
                {
                    index++;
                    if (_bidList.Count == index)
                        _bidList.Add(new BookEntry());
                    _bidList[index].Price = rounded;
                    lastPrice = rounded;
                }

                _bidList[index].Quantity += bid.Quantity;

                total += bid.Quantity;
                _bidList[index].Total = total;
            }

            while (askList.Count < Math.Min(_askList.Count,_bindingLimit))
            {
                askList.AddNew();
            }

            while (bidList.Count < Math.Min(_askList.Count, _bindingLimit))
            {
                bidList.AddNew();
            }

            for (int i = 0; i < askList.Count; i++)
            {
                var count = askList.Count - 1;

                if (i < _askList.Count)
                {
                    askList[count - i].Set(_askList[i].Price, _askList[i].Quantity.Round(roundQuantityDecimals),
                        _askList[i].Total.Round(roundQuantityDecimals));
                }
                else
                {
                    askList[count - i].Clear();
                }
            }
            for (int i = 0; i < bidList.Count; i++)
            {
                if (i < _bidList.Count)
                {
                    bidList[i].Set(_bidList[i].Price, _bidList[i].Quantity.Round(roundQuantityDecimals), _bidList[i].Total.Round(roundQuantityDecimals));
                }
                else
                {
                    bidList[i].Clear();
                }

            }

            return true;
        }

    }
}
