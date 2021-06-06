using System.Collections.Generic;

namespace AsyncNetworking {
    public class LockedList<T> : List<T> {
        private readonly object locker = new object();
        public void TryAdd(T obj) { lock (locker) { this.Add(obj); } }
        public bool TryRemove(T obj) { lock(locker) { return this.Remove(obj); } }
        public int TryFindIndex(T obj) { lock(locker) { return this.FindIndex(x => x.Equals(obj)); } }
        public LockedList() : base() { }
    }
}
