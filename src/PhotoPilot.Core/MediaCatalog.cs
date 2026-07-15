namespace PhotoPilot.Core;

public sealed class MediaCatalog
{
    private readonly object _syncRoot = new();
    private readonly List<MediaItem> _items = [];

    public IReadOnlyList<MediaItem> Items
    {
        get
        {
            lock (_syncRoot)
            {
                return _items.ToArray();
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _items.Count;
            }
        }
    }

    public void Replace(IEnumerable<MediaItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        lock (_syncRoot)
        {
            _items.Clear();
            _items.AddRange(items);
        }
    }

    public void Add(MediaItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (_syncRoot)
        {
            _items.Add(item);
        }
    }

    public bool TryUpdate(MediaItem updatedItem)
    {
        ArgumentNullException.ThrowIfNull(updatedItem);

        lock (_syncRoot)
        {
            int index = _items.FindIndex(
                item => item.Id == updatedItem.Id);

            if (index < 0)
            {
                return false;
            }

            _items[index] = updatedItem;
            return true;
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            _items.Clear();
        }
    }
}