using Hocon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGame
{
    public struct HoconSerializer
    {
        public static T Deserialize<T>(Stream stream) where T : new()
        {
            T obj = new T();

            TypeInfo metaInfo = TypeInfo.GetTypeInfo(typeof(T));
            Hocon.HoconRoot root = Parse(stream);
            Console.WriteLine(root.PrettyPrint(2));
            LoadObject(obj, metaInfo, root.Value.GetObject());

            return obj;
        }

        public static Hocon.HoconRoot Parse(Stream stream, HoconIncludeCallbackAsync includeCallback = null)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                var text = sr.ReadToEnd();
                return Hocon.Parser.Parse(text, includeCallback);

            }
        }

        static void LoadObject(object obj, TypeInfo metaInfo, Hocon.HoconObject hObj)
        {
            var it = hObj.GetEnumerator();
            while(it.MoveNext())
            {
                var key = it.Current.Key;
                var field = it.Current.Value;

                var prop = metaInfo.Get(key);
                if(prop == null)
                {
                    return;
                }
                
                switch(field.Type)
                {
                    case HoconType.Array:
                        LoadArray(obj, prop, field);
                        break;
                    case HoconType.Object:
                        LoadObject(obj, prop, field);
                        break;
                    case HoconType.Literal:
                        LoadLiteral(obj, prop, field.GetString());
                        break;
                }

            }           

        }

        static void LoadArray(object obj, IPropertyAccessor prop, HoconField field)
        {
            var val = Activator.CreateInstance(prop.PropertyType);

            //LoadObject(val, MetaInfo.GetMetaInfo(prop.type), field.GetObject());

            prop.Set(obj, val);
        }

        static void LoadObject(object obj, IPropertyAccessor prop, HoconField field)
        {
            var val = Activator.CreateInstance(prop.PropertyType);

            if(prop.PropertyType.IsGenericType)
            {
                var genericType = prop.PropertyType.GetGenericTypeDefinition();
                if (genericType == typeof(List<>))
                {
                    var elementType = prop.PropertyType.GenericTypeArguments[0];
                    var elmentVal = Activator.CreateInstance(elementType);
                 
                }
                else if(genericType == typeof(Dictionary<,>))
                {

                }
            }
            
            else
            {
                LoadObject(val, TypeInfo.GetTypeInfo(prop.PropertyType), field.GetObject());
            }

            prop.Set(obj, val);
        }

        static void LoadLiteral(object obj, IPropertyAccessor prop, string val)
        {
            if (prop.PropertyType.IsEnum)
            {
                var enumValues = prop.PropertyType.GetEnumValues();
                foreach(var enumVal in enumValues)
                {
                    //todo: parse flags
                    if(prop.PropertyType.GetEnumName(enumVal) == val)
                    {
                        prop.Set(obj, enumVal);
                    }
                }
            }
            else if (prop.PropertyType == typeof(string))
            {
                prop.Set(obj, val);
            }
            else if (prop.PropertyType == typeof(bool))
            {
                bool boolVal = String.Compare(val, "true", true) == 0;
                prop.Set(obj, boolVal);
            }
            else if (prop.PropertyType == typeof(int))
            {
                if(int.TryParse(val, out var intVal))
                {
                    prop.Set(obj, intVal);
                }
            }
            else if (prop.PropertyType == typeof(long))
            {
                if (long.TryParse(val, out var intVal))
                {
                    prop.Set(obj, intVal);
                }
            }
            //....

            else if (prop.PropertyType == typeof(float)
                || prop.PropertyType == typeof(double))
            {
                if (double.TryParse(val, out var intVal))
                {
                    prop.Set(obj, intVal);
                }

            }
        }

    }
}
