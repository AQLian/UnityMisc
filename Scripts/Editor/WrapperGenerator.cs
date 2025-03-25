using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Text;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

public static class WrapperGenerator
{
    [MenuItem("Tools/Generate Wrappers")]
    public static void GenerateWrappers()
    {
        Assembly assembly = Assembly.Load("Assembly-CSharp");
        var allTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(MonoBehaviour)) && !t.IsAbstract)
            .ToList();

        foreach (Type type in allTypes)
        {
            GenerateWrapperClass(type);
        }

        AssetDatabase.Refresh();
    }

    private static void GenerateWrapperClass(Type targetType)
    {
        StringBuilder sb = new StringBuilder();

        HashSet<string> requiredNamespaces = new HashSet<string>();

        // Collect all required namespaces
        CollectNamespaces(targetType, requiredNamespaces);

        // Add using directives
        foreach (string ns in requiredNamespaces.OrderBy(n => n))
        {
            sb.AppendLine($"using {ns};");
        }
        if (requiredNamespaces.Count > 0) sb.AppendLine();

        // Add namespace
        if (!string.IsNullOrEmpty(targetType.Namespace))
        {
            sb.AppendLine($"namespace {targetType.Namespace} {{");
        }

        sb.AppendLine($"public class {targetType.Name}Wrapper : UnityEngine.MonoBehaviour {{");
        sb.AppendLine($"    private {nameof(InvocationMapping)} _targetMapping;");
        sb.AppendLine();
        //sb.AppendLine($"    public {targetType.Name}Wrapper({typeof(System.Object).FullName} target) {{");
        //sb.AppendLine($"        _targetMapping = new {nameof(InvocationMapping)}(target);");
        //sb.AppendLine("    }");
        //sb.AppendLine();


        // SetTarget method instead of constructor
        sb.AppendLine($"    public void SetTarget({typeof(System.Object).FullName} target) {{");
        sb.AppendLine($"        _targetMapping = new {nameof(InvocationMapping)}(target);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate method wrappers
        foreach (MethodInfo method in targetType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName))
        {
            if (method.IsGenericMethod 
                || HasOutOrRefParameters(method) 
                || IsDeclaredInUntiyMono(method))
            {
                continue;
            }
            GenerateMethodWrapper(sb, method);
        }

        // Generate property wrappers
        foreach (PropertyInfo property in targetType.GetProperties(
            BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsDeclaredInUntiyMono(property))
            {
                continue;
            }
            GeneratePropertyWrapper(sb, property);
        }

        // Generate field wrappers
        foreach (FieldInfo field in targetType.GetFields(
            BindingFlags.Public | BindingFlags.Instance))
        {
            if(IsDeclaredInUntiyMono(field))
            {
                continue;
            }
            GenerateFieldWrapper(sb, field);
        }

        sb.AppendLine("}");

        if (!string.IsNullOrEmpty(targetType.Namespace))
        {
            sb.AppendLine("}");
        }

        SaveToFile(targetType.Name + "Wrapper.cs", sb.ToString());
    }

    private static bool HasOutOrRefParameters(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        foreach (ParameterInfo parameter in parameters)
        {
            if (parameter.ParameterType.IsByRef && (parameter.IsOut || parameter.IsIn))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsDeclaredInUntiyMono(MemberInfo member)
    {
        Type declaringType = member.DeclaringType;
        return declaringType == typeof(UnityEngine.MonoBehaviour) 
            || declaringType == typeof(UnityEngine.Behaviour) 
            || declaringType == typeof(UnityEngine.Component) 
            || declaringType == typeof(UnityEngine.Object) 
            || declaringType == typeof(System.Object);
    }

    private static void CollectNamespaces(Type targetType, HashSet<string> namespaces)
    {
        string targetNamespace = targetType.Namespace;
        var alreadyChecked = new HashSet<Type>();

        // Methods
        foreach (MethodInfo method in targetType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName))
        {
            AddTypeNamespaces(method.ReturnType, namespaces, targetNamespace, alreadyChecked);
            foreach (ParameterInfo param in method.GetParameters())
            {
                AddTypeNamespaces(param.ParameterType, namespaces, targetNamespace, alreadyChecked);
            }
        }

        // Properties
        foreach (PropertyInfo property in targetType.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            AddTypeNamespaces(property.PropertyType, namespaces, targetNamespace, alreadyChecked);
        }

        // Fields
        foreach (FieldInfo field in targetType.GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            AddTypeNamespaces(field.FieldType, namespaces, targetNamespace, alreadyChecked);
        }
    }

    private static void AddTypeNamespaces(Type type, HashSet<string> namespaces, string targetNamespace, HashSet<Type> alreadyChecked)
    {
        if (type == null || alreadyChecked.Contains(type)) return;
        alreadyChecked.Add(type);

        // Handle arrays
        if (type.IsArray)
        {
            AddTypeNamespaces(type.GetElementType(), namespaces, targetNamespace, alreadyChecked);
            return;
        }

        // Handle generics
        if (type.IsGenericType)
        {
            foreach (Type genericArg in type.GetGenericArguments())
            {
                AddTypeNamespaces(genericArg, namespaces, targetNamespace, alreadyChecked);
            }
            AddTypeNamespaces(type.GetGenericTypeDefinition(), namespaces, targetNamespace, alreadyChecked);
        }

        // Add namespace if valid
        if (!string.IsNullOrEmpty(type.Namespace) &&
            type.Namespace != targetNamespace &&
            !namespaces.Contains(type.Namespace))
        {
            namespaces.Add(type.Namespace);
        }
    }


    private static void GenerateMethodWrapper(StringBuilder sb, MethodInfo method)
    {
        string returnType = GetTypeName(method.ReturnType);
        string parameters = string.Join(", ", method.GetParameters()
            .Select(p => $"{GetTypeName(p.ParameterType)} {p.Name}"));

        string arguments = string.Join(", ",
            method.GetParameters().Select(p => p.Name));

        sb.AppendLine($"    public {returnType} {method.Name}({parameters}) {{");

        var arg = !string.IsNullOrEmpty(arguments) ? $", {arguments}" : "";
        if (method.ReturnType == typeof(void))
        {
            sb.AppendLine($"        _targetMapping.InvokeMethod(\"{method.Name}\"{arg});");
        }
        else
        {
            sb.AppendLine($"        return ({returnType})_targetMapping.InvokeMethod(\"{method.Name}\"{arg});");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (!type.IsGenericType) return type.FullName;

        string name = type.Name.Split('`')[0];
        IEnumerable<string> args = type.GetGenericArguments().Select(GetTypeName);
        return $"{name}<{string.Join(", ", args)}>";
    }

    private static void GeneratePropertyWrapper(StringBuilder sb, PropertyInfo property)
    {
        sb.AppendLine($"    public {property.PropertyType.FullName} {property.Name} {{");
        if (property.CanRead)
        {
            sb.AppendLine($"        get => ({property.GetGetMethod().ReturnType.FullName})_targetMapping.GetProperty(\"{property.Name}\");");
        }
        if (property.CanWrite)
        {
            sb.AppendLine($"        set => _targetMapping.SetProperty(\"{property.Name}\", value);");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateFieldWrapper(StringBuilder sb, FieldInfo field)
    {
        sb.AppendLine($"    public {field.FieldType.FullName} {field.Name} {{");
        sb.AppendLine($"        get => ({field.FieldType.FullName})_targetMapping.GetFiled(\"{field.Name}\");");
        sb.AppendLine($"        set => _targetMapping.SetFiled(\"{field.Name}\", value);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void SaveToFile(string fileName, string content)
    {
        string path = Path.Combine(Application.dataPath, "AsmMono", "GeneratedWrappers", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }
}