using System;
using System.Collections.Generic;

namespace AspNetExample.Persistency;

public interface IStorage<T>
    where T : IEntity
{
    public IEnumerable<T> GetAll();
    public T? Get(long id);
    public long Add(T item);
    public long Update(Func<T, bool> predicate, Action<T> updateAction);
    public long Delete(long id);
}
