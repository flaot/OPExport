using System;

namespace OpExport
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class LanguageAttribute : Attribute
    {
        public LanguageFlags Language { get; private set; }
        public LanguageAttribute(LanguageFlags language)
        {
            Language = language;
        }
    }
}
