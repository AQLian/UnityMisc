using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;

public class InvocationMapping
{
    public InvocationMapping(object instance) 
    {
        _methodTable = new Dictionary<string, MethodInfo>();
        _fieldTable = new Dictionary<string, FieldInfo>();
        _propertyTable = new Dictionary<string, PropertyInfo>();
        _instance = instance;
    }

    public static InvocationMapping CreateMapping(object target)
    {
        InvocationMapping mapping = new InvocationMapping(target);
        return mapping;
    }

    public object InvokeMethod(string method, params object[] args)
    {
        var m = GetMethodInfo(method);
        return m?.Invoke(_instance, args);
    }

    public object GetFiled(string field)
    {
        var f = GetFieldInfo(field);
        return f?.GetValue(_instance);
    }

    public void SetFiled(string field, object value)
    {
        var f = GetFieldInfo(field);
        f?.SetValue(_instance, value);
    }

    public void SetProperty(string property, object value)
    {
        var p = GetPropertyInfo(property);
        p?.SetValue(_instance, value);
    }

    public object GetProperty(string property)
    {
        var p = GetPropertyInfo(property);
        return p?.GetValue(_instance);
    }

    private MethodInfo GetMethodInfo(string method)
    {
        if (!_methodTable.TryGetValue(method, out var m))
        {
            m = _instance.GetType().GetMethod(method);
            _methodTable.Add(method, m);
        }

        return m;
    }

    private FieldInfo GetFieldInfo(string field)
    {
        if (!_fieldTable.TryGetValue(field, out var f))
        {
            f = _instance.GetType().GetField(field);
            _fieldTable.Add(field, f);
        }

        return f;
    }

    private PropertyInfo GetPropertyInfo(string property)
    {
        if (!_propertyTable.TryGetValue(property, out var p))
        {
            p = _instance.GetType().GetProperty(property);
            _propertyTable.Add(property, p);
        }

        return p;
    }


    private Dictionary<string, MethodInfo> _methodTable;
    private Dictionary<string, FieldInfo> _fieldTable;
    private Dictionary<string, PropertyInfo> _propertyTable;
    private object _instance;
}