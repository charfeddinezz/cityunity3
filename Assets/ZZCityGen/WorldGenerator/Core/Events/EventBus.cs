using System;
using System.Collections.Generic;

namespace ZZCityGen.WorldGenerator.Core.Events
{
    public static class EventBus
    {
        private static readonly Dictionary<string, Action<object>> listeners = new Dictionary<string, Action<object>>();

        public static void Subscribe(string topic, Action<object> handler)
        {
            if (!listeners.ContainsKey(topic)) listeners[topic] = null;
            listeners[topic] += handler;
        }

        public static void Unsubscribe(string topic, Action<object> handler)
        {
            if (!listeners.ContainsKey(topic)) return;
            listeners[topic] -= handler;
            if (listeners[topic] == null) listeners.Remove(topic);
        }

        public static void Publish(string topic, object payload = null)
        {
            if (listeners.TryGetValue(topic, out var handlers)) handlers?.Invoke(payload);
        }
    }
}