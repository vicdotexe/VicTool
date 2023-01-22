using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace VicTool.Main.Misc
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _notificationSupressed = false;
        private bool _supressNotification = false;
        public bool SupressNotification
        {
            get
            {
                return _supressNotification;
            }
            set
            {
                _supressNotification = value;
                if (_supressNotification == false && _notificationSupressed)
                {
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    _notificationSupressed = false;
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SupressNotification)
            {
                _notificationSupressed = true;
                return;
            }
            base.OnCollectionChanged(e);
        }

        public void AddRange(IEnumerable<T> list)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            SupressNotification = true;

            foreach (T item in list)
            {
                Add(item);
            }
            SupressNotification = false;
        }

        protected override void ClearItems()
        {
            SupressNotification = true;
            base.ClearItems();
            SupressNotification = false;
        }
    }
}
