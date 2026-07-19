using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register<T>(T service)
    {
        _services[typeof(T)] = service;
    }

    public static T Get<T>()
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        Debug.LogError($"ServiceLocator: Сервис типа {typeof(T).Name} не найден! Убедитесь, что он зарегистрирован в Bootstrap.");
        return default;
    }

    public static void Clear()
    {
        _services.Clear();
    }
}
