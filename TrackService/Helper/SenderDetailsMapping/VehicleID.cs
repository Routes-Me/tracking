using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TrackService.Helper
{
    public class VehicleID<T>
    {
        private readonly Dictionary<T, string> _connections = new Dictionary<T, string>();

        public int Count
        {
            get
            {
                return _connections.Count;
            }
        }

        public bool HasKey(T key)
        {
            return _connections.ContainsKey(key);
        }

        public void Add(T key, string value)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(key))
                    _connections.Add(key, value);
            }
        }

        public List<T> GetAllData(string value)
        {
            List<T> keys = _connections.Where(x => x.Value == value).Select(x => x.Key).ToList();
            return keys;
        }

        public string GetVehicleId(T key)
        {
            string value;
            if (_connections.TryGetValue(key, out value))
                return value;

            return null;
        }

        public void Remove(T key)
        {
            lock (_connections)
            {
                if (!_connections.ContainsKey(key))
                    return;

                _connections.Remove(key);
            }
        }
    }
}
