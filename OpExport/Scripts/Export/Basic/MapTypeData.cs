using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpExport.Export
{
    public class MapTypeData : ICollection<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<string, string>>
    {
        public delegate string MapTyperison(string orgName, string switchName, Reference refer);
  
        private List<KeyValuePair<string, string>> _maps = new List<KeyValuePair<string, string>>();
        private MapTyperison _mapTypeSwitch;
  
        public MapTypeData(MapTyperison iMapType)
        {
            _mapTypeSwitch = iMapType;
        }

        /// <summary>
        /// 添加一个类型映射
        /// </summary>
        /// <param name="orgType">C++变量类型</param>
        /// <param name="switchType">转换后类型</param>
        public void Add(string orgType, string switchType)
        {
            _maps.Add(new KeyValuePair<string, string>(orgType, switchType));
        }

        /// <summary>
        /// 获取返回值类型
        /// </summary>
        /// <param name="type">C++类型</param>
        /// <returns>返回值类型</returns>
        public string OutRtn(string type)
        {
            string ortType = type;
            string rtnType = type;
            foreach (var item in _maps)
            {
                if (item.Key != type)
                    continue;
                rtnType = item.Value;
                break;
            }
            if (_mapTypeSwitch != null)
                rtnType = _mapTypeSwitch(ortType, rtnType, Reference.Ret);
            return rtnType;
        }

        /// <summary>
        /// 获取参数类型
        /// </summary>
        /// <param name="type">C++类型</param>
        /// <returns>参数类型</returns>
        public string OutArg(string type, Reference reference)
        {
            string ortType = type;
            string argType = type;
            foreach (var item in _maps)
            {
                if (item.Key != type)
                    continue;
                argType = item.Value;
                break;
            }
            if (_mapTypeSwitch != null)
                argType = _mapTypeSwitch(ortType, argType, reference);
            return argType;
        }

        public int Count => _maps.Count;
        public bool IsReadOnly => false;
        public void Add(KeyValuePair<string, string> item) => _maps.Add(item);
        public void Clear() => _maps.Clear();
        public bool Contains(KeyValuePair<string, string> item) => _maps.Contains(item);
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotImplementedException();
        public bool Remove(KeyValuePair<string, string> item) => _maps.Remove(item);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _maps.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _maps.GetEnumerator();
    }
}
