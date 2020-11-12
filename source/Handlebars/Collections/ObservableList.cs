using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using HandlebarsDotNet.Runtime;

namespace HandlebarsDotNet.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    internal class ObservableList<T> : 
        IAppendOnlyList<T>, 
        IObservable<ObservableEvent<T>>, 
        IObserver<ObservableEvent<T>>
    {
        private readonly ReaderWriterLockSlim _observersLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly ReaderWriterLockSlim _itemsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        
        private readonly List<IObserver<ObservableEvent<T>>> _observers = new List<IObserver<ObservableEvent<T>>>();
        private readonly List<T> _inner;

        public ObservableList(IEnumerable<T> list = null)
        {
            _inner = list != null ? new List<T>(list) : new List<T>();
            if (list is IObservable<ObservableEvent<T>> observableList)
            {
                observableList.Subscribe(this);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            T[] array;
            using (_itemsLock.ReadLock())
            {
                array = _inner.ToArray();
            }

            for (int index = 0; index < array.Length; index++)
            {
                yield return array[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item)
        {
            using (_itemsLock.WriteLock())
            {
                _inner.Add(item);
            }
            
            Publish(new AddedObservableEvent<T>(item));
        }
        
        public int Count
        {
            get
            {
                using (_itemsLock.ReadLock())
                {
                    return _inner.Count;
                }
            }
        }
        
        public T this[int index]
        {
            get
            {
                using (_itemsLock.ReadLock())
                {
                    return _inner[index];
                }
            }
        }

        public IDisposable Subscribe(IObserver<ObservableEvent<T>> observer)
        {
            using (_observersLock.WriteLock())
            {
                _observers.Add(observer);
            }
    
            var disposableContainer = new DisposableContainer<List<IObserver<ObservableEvent<T>>>, ReaderWriterLockSlim>(
                _observers, _observersLock, (observers, @lock) =>
                {
                    using (@lock.WriteLock())
                    {
                        observers.Remove(this);
                    }
                }
            );
            
            return disposableContainer;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(ObservableEvent<T> value)
        {
            switch (value)
            {
                case AddedObservableEvent<T> addedObservableEvent:
                    Add(addedObservableEvent.Value);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
        
        private void Publish(ObservableEvent<T> @event)
        {
            using (_observersLock.ReadLock())
            {
                for (int index = 0; index < _observers.Count; index++)
                {
                    try
                    {
                        _observers[index].OnNext(@event);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
    }

    internal class AddedObservableEvent<T> : ObservableEvent<T>
    {
        public AddedObservableEvent(T value) : base(value)
        {
        }
    }
}