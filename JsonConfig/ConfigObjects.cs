using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Newtonsoft.Json;

namespace JsonConfig
{
    public class ConfigObject : DynamicObject, IDictionary<string, object>
    {
        internal Dictionary<string, object> Members = new Dictionary<string, object>();
        public static ConfigObject FromExpando(ExpandoObject e)
        {
            var edict = e as IDictionary<string, object>;
            var c = new ConfigObject();
            var cdict = (IDictionary<string, object>)c;

            // this is not complete. It will, however work for ExpandoObjects
            // which consits only of primitive types, ExpandoObject or ExpandoObject [] 
            // but won't work for generic ExpandoObjects which might include collections etc.
            foreach (var kvp in edict)
            {
                // recursively convert and add ExpandoObjects
                if (kvp.Value is ExpandoObject)
                {
                    cdict.Add(kvp.Key, FromExpando((ExpandoObject)kvp.Value));
                }
                else if (kvp.Value is ExpandoObject[])
                {
                    var configObjects = new List<ConfigObject>();
                    foreach (var ex in ((ExpandoObject[])kvp.Value))
                    {
                        configObjects.Add(FromExpando(ex));
                    }
                    cdict.Add(kvp.Key, configObjects.ToArray());
                }
                else
                    cdict.Add(kvp.Key, kvp.Value);
            }
            return c;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (Members.ContainsKey(binder.Name))
                result = Members[binder.Name];
            else
                result = new NullExceptionPreventer();

            return true;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (Members.ContainsKey(binder.Name))
                Members[binder.Name] = value;
            else
                Members.Add(binder.Name, value);
            return true;
        }
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            // some special methods that should be in our dynamic object
            if (binder.Name == "ApplyJsonFromFile" && args.Length == 1 && args[0] is string)
            {
                result = Config.ApplyJsonFromFileInfo(new FileInfo((string)args[0]), this);
                return true;
            }
            if (binder.Name == "ApplyJsonFromFile" && args.Length == 1 && args[0] is FileInfo)
            {
                result = Config.ApplyJsonFromFileInfo((FileInfo)args[0], this);
                return true;
            }
            if (binder.Name == "Clone")
            {
                result = Clone();
                return true;
            }
            if (binder.Name == "Exists" && args.Length == 1 && args[0] is string)
            {
                result = Members.ContainsKey((string)args[0]);
                return true;
            }

            // no other methods availabe, error
            result = null;
            return false;

        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(Members,Formatting.Indented);
        }
        public void ApplyJson(string json)
        {
            ConfigObject result = Config.ApplyJson(json, this);
            // replace myself's members with the new ones
            Members = result.Members;
        }
        public static implicit operator ConfigObject(ExpandoObject exp)
        {
            return FromExpando(exp);
        }
        #region IEnumerable implementation
        public System.Collections.IEnumerator GetEnumerator()
        {
            return Members.GetEnumerator();
        }
        #endregion

        #region IEnumerable implementation
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return Members.GetEnumerator();
        }
        #endregion

        #region ICollection implementation
        public void Add(KeyValuePair<string, object> item)
        {
            Members.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Members.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return Members.ContainsKey(item.Key) && Members[item.Key] == item.Value;
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return Members.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region IDictionary implementation
        public void Add(string key, object value)
        {
            Members.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return Members.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return Members.Remove(key);
        }

        public object this[string key]
        {
            get
            {
                return Members[key];
            }
            set
            {
                Members[key] = value;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return Members.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                return Members.Values;
            }
        }
        public bool TryGetValue(string key, out object value)
        {
            return Members.TryGetValue(key, out value);
        }

        #region ICloneable implementation

        object Clone()
        {
            return Merger.Merge(new ConfigObject(), this);
        }

        #endregion
        #endregion

        #region casts
        public static implicit operator bool(ConfigObject c)
        {
            // we want to test for a member:
            // if (config.SomeMember) { ... }
            //
            // instead of:
            // if (config.SomeMember != null) { ... }

            // we return always true, because a NullExceptionPreventer is returned when member
            // does not exist
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Null exception preventer. This allows for hassle-free usage of configuration values that are not
    /// defined in the config file. I.e. we can do Config.Scope.This.Field.Does.Not.Exist.Ever, and it will
    /// not throw an NullPointer exception, but return te NullExceptionPreventer object instead.
    /// 
    /// The NullExceptionPreventer can be cast to everything, and will then return default/empty value of
    /// that datatype.
    /// </summary>
    public class NullExceptionPreventer : DynamicObject
    {
        // all member access to a NullExceptionPreventer will return a new NullExceptionPreventer
        // this allows for infinite nesting levels: var s = Obj1.foo.bar.bla.blubb; is perfectly valid
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new NullExceptionPreventer();
            return true;
        }
        // Add all kinds of datatypes we can cast it to, and return default values
        // cast to string will be null
        public static implicit operator string(NullExceptionPreventer nep)
        {
            return null;
        }
        public override string ToString() => null;

        public static implicit operator string[] (NullExceptionPreventer nep)
        {
            return new string[] { };
        }
        // cast to bool will always be false
        public static implicit operator bool(NullExceptionPreventer nep)
        {
            return false;
        }
        public static implicit operator bool[] (NullExceptionPreventer nep)
        {
            return new bool[] { };
        }
        public static implicit operator int[] (NullExceptionPreventer nep)
        {
            return new int[] { };
        }
        public static implicit operator long[] (NullExceptionPreventer nep)
        {
            return new long[] { };
        }
        public static implicit operator int(NullExceptionPreventer nep)
        {
            return 0;
        }
        public static implicit operator long(NullExceptionPreventer nep)
        {
            return 0;
        }
        // nullable types always return null
        public static implicit operator bool? (NullExceptionPreventer nep)
        {
            return null;
        }
        public static implicit operator int? (NullExceptionPreventer nep)
        {
            return null;
        }
        public static implicit operator long? (NullExceptionPreventer nep)
        {
            return null;
        }
    }
}
