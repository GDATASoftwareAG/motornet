using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetExample.Persistency;

public class InMemoryStorage<T> : IStorage<T>
    where T : IEntity
{
    private readonly List<T> _contents = [];
    private long _nextId;

    public IEnumerable<T> GetAll()
    {
        return _contents;
    }

    public T? Get(long id)
    {
        return _contents.FirstOrDefault(i => i.Id == id);
    }

    public long Add(T item)
    {
        item.Id = _nextId++;
        _contents.Add(item);
        return item.Id;
    }

    public long Update(Func<T, bool> predicate, Action<T> updateAction)
    {
        var matches = _contents.Where(predicate).ToList();
        matches.ForEach(updateAction);
        return matches.Count;
    }

    public long Delete(long id)
    {
        return _contents.RemoveAll(i => i.Id == id);
    }
}
