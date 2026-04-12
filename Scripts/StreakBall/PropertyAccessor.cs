using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public static class PropertyAccessors<T>
        where T : global::ProtoBuf.IExtensible
    {
        private static readonly Dictionary<string, Delegate> _cache
            = new();

        public static Func<T, TValue> CreateGetter<TValue>(string propertyName)
        {
            var property = typeof(T).GetProperty(propertyName);
            if (!_cache.TryGetValue(property.Name, out var getter))
            {
                getter = (Func<T, TValue>)
                     Delegate.CreateDelegate(
                        typeof(Func<T, TValue>),
                        property.GetGetMethod());

                _cache[property.Name] = getter;
            }
            return (Func<T, TValue>) getter;
        }
    }
}
