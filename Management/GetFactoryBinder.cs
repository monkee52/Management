using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AydenIO.Management {
    public class GetFactoryBinder : Binder {
        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers) {
            return match.First(m => m.IsGenericMethod && m.GetGenericArguments().Length == 1);
        }

        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, Object value, CultureInfo culture) => throw new NotImplementedException();
        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] names, out Object state) => throw new NotImplementedException();
        public override Object ChangeType(Object value, Type type, CultureInfo culture) => throw new NotImplementedException();
        public override void ReorderArgumentArray(ref Object[] args, Object state) => throw new NotImplementedException();
        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers) => throw new NotImplementedException();
    }
}
