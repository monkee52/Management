using System;
using System.Collections;
using System.Collections.Generic;
using System.Management;
using System.Text;

namespace AydenIO.Management {
    public class ManagementClassEnumerator<T> : IEnumerator where T : ManagementClassBase {
        private ManagementObjectCollection.ManagementObjectEnumerator _enumerator;

        public ManagementClassEnumerator(ManagementObjectCollection.ManagementObjectEnumerator objectEnumerator) {
            this._enumerator = objectEnumerator;
        }

        public virtual object Current => ManagementSession.GetFactory<T, ManagementClassFactory<T>>().CreateInstance(this._enumerator.Current);

        public virtual bool MoveNext() => this._enumerator.MoveNext();
        public virtual void Reset() => this._enumerator.Reset();
    }
}
