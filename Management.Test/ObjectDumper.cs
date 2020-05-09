using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    public class ObjectDumper {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;

        private ObjectDumper(int indentSize) {
            _indentSize = indentSize;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
        }

        public static string Dump(object element) {
            return ObjectDumper.Dump(element, 4);
        }

        public static string Dump(object element, int indentSize) {
            ObjectDumper dumper = new ObjectDumper(indentSize);

            return dumper.DumpElement(element);
        }

        private string DumpElement(object element) { 
            if (element == null || element is ValueType || element is string) {
                this.Write(this.FormatValue(element));
            } else {
                Type objectType = element.GetType();

                if (!typeof(IEnumerable).IsAssignableFrom(objectType)) {
                    this.Write("{{{0}}}", objectType.FullName);

                    _hashListOfFoundElements.Add(element.GetHashCode());
                    _level++;
                }

                IEnumerable enumerableElement = element as IEnumerable;

                if (enumerableElement != null) { 
                    foreach (object item in enumerableElement) { 
                        if (item is IEnumerable && !(item is string)) {
                            this._level++;

                            this.DumpElement(item);

                            this._level--;
                        } else { 
                            if (!this.AlreadyTouched(item)) {
                                this.DumpElement(item);
                            } else {
                                this.Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                            }
                        }
                    }
                } else {
                    MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);

                    foreach (MemberInfo memberInfo in members) {
                        FieldInfo fieldInfo = memberInfo as FieldInfo;
                        PropertyInfo propertyInfo = memberInfo as PropertyInfo;

                        if (fieldInfo == null && propertyInfo == null) {
                            continue;
                        }

                        Type type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;

                        object value = null;

                        try {
                            value = fieldInfo != null ? fieldInfo.GetValue(element) : propertyInfo.GetValue(element, null);
                        } catch (Exception e) {
                            value = e.InnerException.ToString();
                        }

                        if (type.IsValueType || type == typeof(string)) {
                            this.Write("{0}: {1}", memberInfo.Name, this.FormatValue(value));
                        } else {
                            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);

                            this.Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                            bool alreadyTouched = !isEnumerable && this.AlreadyTouched(value);

                            this._level++;

                            if (!alreadyTouched) {
                                this.DumpElement(value);
                            } else {
                                this.Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                            }

                            this._level--;
                        }
                    }
                }

                if (!typeof(IEnumerable).IsAssignableFrom(objectType)) {
                    this._level--;
                }
            }

            return this._stringBuilder.ToString();
        }

        private bool AlreadyTouched(object value) {
            if (value == null) {
                return false;
            }

            int hash = value.GetHashCode();

            for (int i = 0; i < this._hashListOfFoundElements.Count; i++) {
                if (this._hashListOfFoundElements[i] == hash) {
                    return true;
                }
            }

            return false;
        }

        private void Write(string value, params object[] args) {
            string space = new String(' ', this._level * this._indentSize);

            if (args != null) {
                value = String.Format(value, args);
            }

            this._stringBuilder.AppendLine(space + value);
        }

        private string FormatValue(object o) {
            if (o == null) {
                return "null";
            }
            
            if (o is DateTime) {
                return ((DateTime)o).ToShortDateString();
            }

            if (o is string) {
                return String.Format("\"{0}\"", o);
            }

            if (o is char && (char)o == '\0') {
                return String.Empty;
            }

            if (o is ValueType) {
                return o.ToString();
            }

            if (o is IEnumerable) {
                return "...";
            }

            return "{ }";
        }
    }
}
