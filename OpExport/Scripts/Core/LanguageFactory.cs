using OpExport.Export;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OpExport
{
    public class LanguageFactory
    {
        /// <summary>
        /// 获取导出文件的存放位置
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string ExportPath(string file)
        {
            string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string folder = Path.GetDirectoryName(path);
            return Path.Combine(folder, file);
        }

        /// <summary>
        /// 根据语言创建对应的对象
        /// </summary>
        /// <param name="languageFlags"></param>
        /// <returns></returns>
        public static List<IExport> Create(LanguageFlags languageFlags)
        {
            List<IExport> exportList = new List<IExport>();
            foreach (LanguageFlags item in Enum.GetValues(typeof(LanguageFlags)))
            {
                if ((languageFlags & item) != 0)
                    exportList.Add(CreateObj(item));
            }
            return exportList;
        }
        private static IExport CreateObj(LanguageFlags language)
        {
            var dicTypeByLang = GetAllTypesWithLangAttribute();
            if (!dicTypeByLang.TryGetValue(language, out var classType))
                throw new KeyNotFoundException(language.ToString());
            object obj = Activator.CreateInstance(classType);
            if (obj is not IExport)
                throw new Exception("define attr by class not inherit IExport " + classType.ToString());
            return (IExport)obj;

        }
        private static Dictionary<LanguageFlags, Type> GetAllTypesWithLangAttribute()
        {
            Dictionary<LanguageFlags, Type> dicTypeByLang = new Dictionary<LanguageFlags, Type>();
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttribute<LanguageAttribute>();
                if (attr == null)
                    continue;
                dicTypeByLang.Add(attr.Language, type);
            }
            return dicTypeByLang;
        }
    }
}
