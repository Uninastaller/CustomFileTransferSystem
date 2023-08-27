using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace Common.Model
{
    public class AutoRefreshingCollection<T> : IList<T>, INotifyCollectionChanged
    {
        private readonly List<T> _items = new List<T>();
        private readonly Timer _refreshTimer = new Timer();

        private readonly Dispatcher _dispatcher;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        // Interval for auto-refresh in milliseconds
        public double RefreshInterval
        {
            get => _refreshTimer.Interval;
            set => _refreshTimer.Interval = value;
        }

        public AutoRefreshingCollection(double refreshInterval = 1000)
        {
            RefreshInterval = refreshInterval;

            _dispatcher = Dispatcher.CurrentDispatcher;

            // Set up the timer
            _refreshTimer.Elapsed += (sender, e) => _dispatcher.Invoke(Refresh);
            _refreshTimer.AutoReset = true;
            _refreshTimer.Enabled = true;
        }

        private void Refresh()
        {
            // Trigger the CollectionChanged event to notify listeners of a refresh
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #region IList<T> Implementation

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => _items[index];
            set => _items[index] = value;
        }

        public void Add(T item)
        {
            _items.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            _items.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _items.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public bool Remove(T item)
        {
            bool removed = _items.Remove(item);
            if (removed)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return removed;
        }

        public void RemoveAt(int index)
        {
            T removedItem = _items[index];
            _items.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
