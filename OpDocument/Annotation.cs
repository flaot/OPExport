using System.Collections.Generic;

namespace OpDocument
{
    public class Annotation
    {
        public string funcName;
        public string funcAnnotation;
        public string returnAnnotation;
        public string example;
        public List<ArgDesc> argDescs;
        public override string ToString() => string.Format("{0}", funcName);
    }

    public class ArgDesc
    {
        public string name;
        public string annotation;

        public override string ToString() => string.Format("{0}", name);
    }
}
