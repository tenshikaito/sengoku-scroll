using System;
using System.Collections.Generic;

namespace Library
{
    public class TreeObject<T> where T : TreeObject<T>, IDisposable
    {
        protected List<T> children = new List<T>();

        public T parent { get; private set; }

        public bool isRoot => parent == null;

        public TreeObject() { }

        public TreeObject(T parent) => moveTo(parent);

        public void moveTo(T go)
        {
            parent = go ?? throw new ArgumentNullException(nameof(go));

            if (!isRoot) parent.children.Remove((T)this);

            parent.children.Add((T)this);
        }

        public virtual void Dispose()
        {
            parent = null;

            children.ForEach(o => Dispose());
            children.Clear();
        }
    }
}
