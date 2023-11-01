using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpExport
{
    public class CodeWriter
    {
        private readonly string _blockStart;
        private readonly string _blockEnd;
        private readonly bool _blockFromNewLine; //起始块是否换行
        private readonly bool _usingTabs;        //使用tab缩进还是空格缩进
        private readonly string _endOfLine;      //换行符
        private readonly string _fileMark;       //文件说明

        private readonly string _indentStr;
        private StringBuilder _temp = new StringBuilder();
        private List<string> _lines = new List<string>();
        private int _indentNumber = 0;
        public CodeWriter(string blockStart = "{", string blockEnd = "}",
            bool blockFromNewLine = true, bool usingTabs = false, string endOfLine = "\n",
            string fileMark = "/**\n" +
            "This is an automatically generated class by OpExport. Please do not modify it.\n" +
            "License：https://github.com/WallBreaker2/op/blob/master/LICENSE \n" +
            "**/")
        {
            _blockStart = blockStart;
            _blockEnd = blockEnd;
            _blockFromNewLine = blockFromNewLine;
            _usingTabs = usingTabs;
            _endOfLine = endOfLine;
            _fileMark = fileMark;

            _indentStr = _usingTabs ? "\t" : "    ";
            WriteMark();
        }

        /// <summary>
        /// 写入文件说明(不被缩进影响)
        /// </summary>
        public void WriteMark()
        {
            if (string.IsNullOrEmpty(_fileMark))
                return;
            _lines.Add(_fileMark);
            _lines.Add("");
        }

        /// <summary>
        /// 写入一行
        /// </summary>
        /// <param name="format">格式文本</param>
        /// <param name="args">参数</param>
        public CodeWriter Writeln(string format = "", params object[] args)
        {
            if (args.Length == 0)
                _Writeln(format);
            else
                _Writeln(string.Format(format, args));
            return this;
        }
        /// <summary>
        /// 写入一行(无自动插入instert)
        /// </summary>
        /// <param name="format">格式文本</param>
        /// <param name="args">参数</param>
        public CodeWriter Rawln(string format = "", params object[] args)
        {
            if (args.Length == 0)
                _Writeln(format, false);
            else
                _Writeln(string.Format(format, args), false);
            return this;
        }
        private void _Writeln(string txt, bool instert = true)
        {
            if (string.IsNullOrEmpty(txt))
            {
                _lines.Add(txt);
                return;
            }

            _temp.Clear();
            if (instert)
                for (int i = 0; i < _indentNumber; ++i)
                    _temp.Append(_indentStr);
            _temp.Append(txt);
            _lines.Add(_temp.ToString());
        }
        /// <summary>
        /// 开始块
        /// </summary>
        public CodeWriter StartBlock()
        {
            if (_blockFromNewLine || this._lines.Count == 0)
            {
                _Writeln(_blockStart);
            }
            else
            {
                var str = _lines[_lines.Count - 1];
                _lines[_lines.Count - 1] = str + _blockStart;
            }
            ++_indentNumber;
            return this;
        }

        /// <summary>
        /// 结束块
        /// </summary>
        public CodeWriter EndBlock()
        {
            --_indentNumber;
            _Writeln(_blockEnd);
            return this;
        }

        /// <summary>
        /// 增加一级缩进
        /// </summary>
        public CodeWriter IncIndent()
        {
            ++_indentNumber;
            return this;
        }

        /// <summary>
        /// 减少一级缩进
        /// </summary>
        public CodeWriter DecIndent()
        {
            --_indentNumber;
            return this;
        }

        /// <summary>
        /// 重置文本
        /// </summary>
        public void Reset()
        {
            _lines.Clear();
            _indentNumber = 0;
            WriteMark();
        }

        /// <summary>
        /// 保存至文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="forced">强制写入;非强制将判断文件内容是否一致</param>
        public bool Save(string filePath, bool forced = false, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.Default;
            string context = this.ToString();
            if (!forced && File.Exists(filePath) && File.ReadAllText(filePath, encoding) == context)
                return false;
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, context, encoding);
            return true;
        }

        public override string ToString()
        {
            return string.Join(_endOfLine, _lines);
        }
        public override int GetHashCode()
        {
            return _lines.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return _lines.Equals(obj);
        }
    }
}
