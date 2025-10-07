using System.Collections.Generic;
using System.Linq;
using Relay.Core.Caching.Invalidation;

namespace Relay.Caching.Example
{
    /// <summary>
    /// Simple implementation of ICacheKeyTracker for demonstration purposes.
    /// </summary>
    public class SimpleCacheKeyTracker : ICacheKeyTracker
    {
        private readonly Dictionary<string, HashSet<string>> _keysByTag = new();
        private readonly Dictionary<string, HashSet<string>> _keysByDependency = new();
        private readonly HashSet<string> _allKeys = new();

        public void AddKey(string key, IEnumerable<string>? tags = null, IEnumerable<string>? dependencies = null)
        {
            _allKeys.Add(key);
            
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    if (!_keysByTag.ContainsKey(tag))
                        _keysByTag[tag] = new HashSet<string>();
                    _keysByTag[tag].Add(key);
                }
            }
            
            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    if (!_keysByDependency.ContainsKey(dependency))
                        _keysByDependency[dependency] = new HashSet<string>();
                    _keysByDependency[dependency].Add(key);
                }
            }
        }

        public void RemoveKey(string key)
        {
            _allKeys.Remove(key);
            
            foreach (var tagKeys in _keysByTag.Values)
                tagKeys.Remove(key);
                
            foreach (var depKeys in _keysByDependency.Values)
                depKeys.Remove(key);
        }

        public IList<string> GetKeysByPattern(string pattern)
        {
            var result = new List<string>();
            foreach (var key in _allKeys)
            {
                if (key.Contains(pattern))
                    result.Add(key);
            }
            return result;
        }

        public IList<string> GetKeysByTag(string tag)
        {
            return _keysByTag.ContainsKey(tag) ? _keysByTag[tag].ToList() : new List<string>();
        }

        public IList<string> GetKeysByDependency(string dependencyKey)
        {
            return _keysByDependency.ContainsKey(dependencyKey) ? _keysByDependency[dependencyKey].ToList() : new List<string>();
        }

        public IList<string> GetAllKeys()
        {
            return _allKeys.ToList();
        }

        public void ClearAll()
        {
            _allKeys.Clear();
            _keysByTag.Clear();
            _keysByDependency.Clear();
        }
    }
}