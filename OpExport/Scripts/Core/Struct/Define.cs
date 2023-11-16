using System;
using System.Collections.Generic;

namespace OpExport
{
    public interface IParser<T>
    {
        static abstract T Parse(string s);
    }
    public class Method : ICloneable
    {
        /// <summary> 方法名称 </summary>
        public string name;
        /// <summary> 方法注释 </summary>
        public string annotation;
        /// <summary> 返回类型 </summary>
        public string returnType;
        /// <summary> 返回类型注释 </summary>
        public string returnAnnotation;
        /// <summary> 参数类型及参数名称 </summary>
        public List<Arg> args;
        /// <summary> 方法使用示范 </summary>
        public string example;

        public object Clone()
        {
            var method = (Method)MemberwiseClone();
            method.args = args.ConvertAll(item => (Arg)item.Clone());
            return method;
        }
        public override string ToString()
        {
            string argTxt = string.Join(", ", args);
            return string.Format("{0} {1}({2})", returnType, name, argTxt);
        }
    }
    public class Arg : ICloneable
    {
        /// <summary> 参数名称 </summary>
        public string name;
        /// <summary> 参数类型 </summary>
        public string type;
        /// <summary> 传递引用类型 </summary>
        public Reference refType;
        /// <summary> 参数注释 </summary>
        public string annotation;

        public object Clone() => MemberwiseClone();
        public override string ToString()
        {
            if (refType != Reference.None)
                return string.Format("[{0}] {1} {2}", refType, type, name);
            else
                return string.Format("{0} {1}", type, name);
        }
    }
    public enum Reference
    {
        None,
        In = 1,
        Out = 2,
        InOut = 3,
        Ret = 4,
    }
}
