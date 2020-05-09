using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace AydenIO.Management {
    public class ManagementClassCollection<T> : ICollection where T : ManagementClassBase {
        private ManagementObjectCollection _collection;

        public ManagementClassCollection(ManagementObjectCollection objectCollection) {
            this._collection = objectCollection;
        }

        public virtual int Count => this._collection.Count;
        public virtual bool IsSynchronized => this._collection.IsSynchronized;
        public virtual object SyncRoot => this;

        public virtual void CopyTo(Array array, int index) {
            this._collection.CopyTo(array, index);

            for (int i = 0; i < array.Length; i++) {
                array.SetValue(ManagementSession.GetFactory<T>().CreateInstance((ManagementBaseObject)array.GetValue(i)), i);
            }
        }

        public virtual IEnumerator GetEnumerator() => new ManagementClassEnumerator<T>(this._collection.GetEnumerator());
    }
}
