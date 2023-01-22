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
    public class AggregatedOrderBookBaseConvert
    {

        public BindingList<BookEntry> Asks => askList;
        public BindingList<BookEntry> Bids => bidList;
        private BindingList<BookEntry> askList = new BindingList<BookEntry>();
        private BindingList<BookEntry> bidList = new BindingList<BookEntry>();

        private List<BookEntry> _askList = new List<BookEntry>();
        private List<BookEntry> _bidList = new List<BookEntry>();

        private KucoinSpotSymbolOrderBook _book;
        public KucoinSpotSymbolOrderBook Book => _book;
        

        public EventHandler<AggregatedOrderBook> OnBookSetup;

        private KucoinSpotSymbolOrderBook _baseConversionBook;

        private string _baseConvertionSymbol = "BTC-USDT";
        private bool _useConversion = false;

        private decimal GetConversion(decimal basePrice, OrderBookEntryType orderType)
        {
            var bestOffer = orderType == OrderBookEntryType.Ask
                ? _baseConversionBook.BestOffers.Ask.Price
                : _baseConversionBook.BestBid.Price;
            return basePrice * bestOffer;
        }

        public void SetBaseConversion(string symbol)
        {
            _book?.StopAsync();
            _baseConvertionSymbol = symbol;
            _baseConversionBook =
                new KucoinSpotSymbolOrderBook(symbol, new KucoinOrderBookOptions() { Limit = 50 });
            _baseConversionBook.StartAsync();
            _useConversion = true;
        }

        public AggregatedOrderBookBaseConvert(string symbol, string baseConversionSymbol)
        {
            if (baseConversionSymbol != null)
            {
                _baseConversionBook =
                    new KucoinSpotSymbolOrderBook(baseConversionSymbol, new KucoinOrderBookOptions() { Limit = 50 });
                _baseConversionBook.StartAsync();
                _useConversion = true;
            }

            

            for (int i = 0; i <= 51; i++)
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
                Limit = 50,
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
        
        public bool Aggregate(int priceDecimals, int roundQuantityDecimals)
        {
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
            _askList[0].Price = lastPrice;
            foreach (var ask in _book.Asks)
            {
                var rounded = ask.Price.ToPositiveInfinity(priceDecimals);


                if (lastPrice != rounded)
                {
                    index++;
                    EnsureListSizes();
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

            lastPrice = _book.Bids.First().Price.ToNegativeInfinity(priceDecimals);
            _bidList[0].Price = lastPrice;
            foreach (var bid in _book.Bids)
            {
                var rounded = bid.Price.ToNegativeInfinity(priceDecimals);

                if (lastPrice != rounded)
                {
                    index++;
                    EnsureListSizes();
                    _bidList[index].Price = rounded;
                    lastPrice = rounded;
                }

                _bidList[index].Quantity += bid.Quantity;

                total += bid.Quantity;
                _bidList[index].Total = total;
            }


            for (int i = 0; i < 20; i++)
            {
                var count = _askList.Count - 1;

                if (i < _askList.Count)
                {
                    var price = _useConversion
                        ? GetConversion(_askList[i].Price, OrderBookEntryType.Ask)
                        : _askList[i].Price;
                    askList[count-i].Set(price, _askList[i].Quantity.Round(roundQuantityDecimals), _askList[i].Total.Round(roundQuantityDecimals));
                }
                else
                {
                    askList[count-i].Clear();
                }

                if (i < _bidList.Count)
                {
                    var price = _useConversion
                        ? GetConversion(_bidList[i].Price, OrderBookEntryType.Bid)
                        : _askList[i].Price;
                    bidList[i].Set(price, _bidList[i].Quantity.Round(roundQuantityDecimals), _bidList[i].Total.Round(roundQuantityDecimals));
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
