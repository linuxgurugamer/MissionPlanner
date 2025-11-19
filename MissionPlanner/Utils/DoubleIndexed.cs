using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MissionPlanner.Utils
{
    public class DoubleIndexed
    {
        /// <summary>
        /// Represents an item with an integer ID, a string name, and an associated object.
        /// </summary>
        public class ItemEntry<T>
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public T Data { get; set; }

            public ItemEntry(int id, string name, T data)
            {
                Id = id;
                Name = name;
                Data = data;
            }

            public override string ToString() => $"{Id}: {Name} ({typeof(T).Name})";
        }

        /// <summary>
        /// Stores and retrieves ItemEntry<T> by either int ID or string name.
        /// </summary>
        public class ItemRegistry<T>
        {
            private readonly Dictionary<int, ItemEntry<T>> _byId = new Dictionary<int, ItemEntry<T>>();
            private readonly Dictionary<string, ItemEntry<T>> _byName =
                new Dictionary<string, ItemEntry<T>>(StringComparer.OrdinalIgnoreCase);

            public void Add(ItemEntry<T> entry)
            {
                if (entry == null) return;

                _byId[entry.Id] = entry;

                if (!string.IsNullOrEmpty(entry.Name))
                    _byName[entry.Name] = entry;
            }

            public ItemEntry<T> Get(object key)
            {
                if (key is int id && _byId.TryGetValue(id, out var e1))
                    return e1;

                if (key is string name && _byName.TryGetValue(name, out var e2))
                    return e2;

                return null;
            }

            public ItemEntry<T> GetById(int id) =>
                _byId.TryGetValue(id, out var e) ? e : null;

            public ItemEntry<T> GetByName(string name) =>
                _byName.TryGetValue(name, out var e) ? e : null;
        }
    }


}
