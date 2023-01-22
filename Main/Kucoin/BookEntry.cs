using System.ComponentModel;
using System.Runtime.CompilerServices;
using VicTool.Properties;

namespace VicTool.Main.Kucoin
{
    public class BookEntry : INotifyPropertyChanged
    {
        public decimal Price
        {
            get { return _price;}
            set {
                if (_price != value)
                {
                    _price = value; OnPropertyChanged();
                } }
        }
        private decimal _price;
        public decimal Quantity
        {
            get { return _quantity;}
            set {
                if (_quantity != value)
                {
                    _quantity = value; OnPropertyChanged();
                } }
        }
        private decimal _quantity;
        public decimal Total
        {
            get { return _total;}
            set {
                if (value != _total)
                {
                    _total = value; OnPropertyChanged();
                } }
        }
        private decimal _total;
        public event PropertyChangedEventHandler PropertyChanged;

        public void Set(decimal price, decimal quantity, decimal total)
        {
            Price = price;
            Quantity = quantity;
            Total = total;
        }

        public void SetQuantity(decimal amount)
        {
            Quantity = amount;
        }

        public void SetTotal(decimal total)
        {
            Total = total;
        }
        public void SetPrice(decimal price)
        {
            Price = price;
        }
        public void Clear()
        {
            noise = true;
            Price = 0;
            Quantity = 0;
            Total = 0;
            noise = false;
        }

        private bool noise;

        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (noise)
                return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
