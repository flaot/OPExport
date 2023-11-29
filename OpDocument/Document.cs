using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace OpDocument
{
    /// <summary>
    /// 获取OP的远端文档
    /// </summary>
    public class Document
    {
        /// <summary> OP文档主页 </summary>
        public const string HOME_URL = "https://github.com/WallBreaker2/op/wiki";

        /// <summary> 缓存网页内容的目录 </summary>
        public string cacheFolder = Path.Combine(Path.GetTempPath(), "OPWifi");

        /// <summary> 缓存网页内容的有效时间(24小时) </summary>
        public TimeSpan cacheintervalTime = new TimeSpan(24, 0, 0);

        /// <summary>
        /// 根据网址获取OP文档
        /// </summary>
        /// <param name="homeUrl">OP的API网址首页</param>
        /// <returns></returns>
        public static List<Annotation> Generate()
        {
            Document document = new Document();
            return document._Generate(HOME_URL);
        }

        private List<Annotation> _Generate(string homeUrl)
        {
            var stopWatch = Stopwatch.StartNew();
            List<Annotation> annotations = new List<Annotation>();

            //访问主页
            Console.WriteLine("parse home url:" + homeUrl);
            List<string> jumpUrl = ParseHomeJumpPage(homeUrl);

            //访问主页中'文档目录'相关子页面
            for (int i = 0; i < jumpUrl.Count; i++)
            {
                string url = jumpUrl[i];
                if (url.StartsWith("wiki/./"))
                    url = url.Substring("wiki/./".Length);
                url = homeUrl + "/" + url;
                Console.WriteLine("parse sub url:" + url);
                List<Annotation> sub = ParseSubPage(url);
                annotations.AddRange(sub);
            }
            stopWatch.Stop();
            Console.WriteLine($"parse url finished! Time taken: {(double)stopWatch.ElapsedMilliseconds / 1000:0.000} secs");

            //排个序(方法名
            annotations.Sort((l, r) => l.funcName.CompareTo(r.funcName));
            return annotations;
        }

        private List<string> ParseHomeJumpPage(string homeUrl)
        {
            string context = GetWebContext(homeUrl);

            //得到'文档目录'文本段
            int index = context.IndexOf("href=\"#文档目录\"");
            int startIndex = context.IndexOf("<ul>", index);
            int endIndex = context.IndexOf("</ul>", startIndex);
            string jumpText = context.Substring(startIndex, endIndex - startIndex);

            //解析跳转Url
            List<string> subUrls = new List<string>();
            startIndex = 0;
            while (startIndex >= 0)
            {
                startIndex = jumpText.IndexOf("<a href=\"", startIndex);
                if (startIndex < 0) continue;
                endIndex = jumpText.IndexOf("</a>", startIndex);
                if (endIndex < 0) continue;

                string tempBlock = jumpText.Substring(startIndex, endIndex - startIndex);
                index = tempBlock.IndexOf('\"') + 1;
                tempBlock = tempBlock.Substring(index, tempBlock.IndexOf('\"', index) - index);
                subUrls.Add(tempBlock);

                startIndex = endIndex;
            }
            return subUrls;
        }
        private List<Annotation> ParseSubPage(string subUrl)
        {
            string context = GetWebContext(subUrl);
            int index = context.IndexOf("href=\"#接口目录\"");
            if (index < 0)
                return new List<Annotation>();
            int startIndex = context.IndexOf("<ul>", index);
            int endIndex = context.IndexOf("</ul>", startIndex);
            index = endIndex;

            //方法跳转超链接
            List<string> jumpContext = new List<string>();
            string functionDefine = context.Substring(startIndex, endIndex - startIndex);
            startIndex = 0;
            while (startIndex >= 0)
            {
                startIndex = functionDefine.IndexOf("href=\"", startIndex);
                if (startIndex < 0) continue;
                endIndex = functionDefine.IndexOf("\">", startIndex);
                if (endIndex < 0) continue;
                startIndex += "href=\"".Length;

                string tempBlock = functionDefine.Substring(startIndex, functionDefine.IndexOf('\"', startIndex) - startIndex);
                jumpContext.Add(tempBlock);

                startIndex = endIndex;
            }

            //方法详细说明
            List<Annotation> annotations = new List<Annotation>();
            string contextText = context.Substring(index);
            foreach (var href in jumpContext)
            {
                index = contextText.IndexOf(href);
                if (index < 0)
                {
                    Console.WriteLine("找不到的超链接：" + href);
                    continue;
                }
                index = 0;
                Annotation annotation = new Annotation();
                functionDefine = SubBlockString(contextText, href, "<h3", ref index); //方法的说明块
                if (index < 0)
                    functionDefine = contextText.Substring(contextText.IndexOf(href));

                index = 0;
                annotation.funcName = SubBlockString(functionDefine, "</a>", "</h3>", ref index); index = 0;
                annotation.funcAnnotation = SubBlockString(functionDefine, "<p>", "</p>", ref index); index = 0;
                annotation.example = string.Empty;
                annotation.returnAnnotation = string.Empty;
                annotation.argDescs = new List<ArgDesc>();
                index = functionDefine.IndexOf("<strong>返回值</strong>");
                if (index >= 0)
                {
                    index = 0;
                    annotation.returnAnnotation = SubBlockString(functionDefine, "</code></p>\n<p>", "</p>", ref index);
                    if (index < 0)
                    {
                        index = 0;
                        annotation.returnAnnotation = SubBlockString(functionDefine, "</code></p>\n<ul>", "</ul>", ref index);
                    }
                    foreach (var item in new string[] { "<ul>", "</ul>", "<li>", "</li>" })
                        annotation.returnAnnotation = annotation.returnAnnotation.Replace(item, string.Empty);
                    annotation.returnAnnotation = annotation.returnAnnotation.Trim();
                }

                //参数说明
                index = functionDefine.IndexOf("<tbody>");
                endIndex = functionDefine.IndexOf("</tbody>", index + 1);
                while (index >= 0 && functionDefine.IndexOf("<td>", index) < endIndex)
                {
                    ArgDesc argDesc = new ArgDesc();
                    argDesc.name = SubBlockString(functionDefine, "<td>", "</td>", ref index);
                    if (index < 0) break;
                    _ = SubBlockString(functionDefine, "<td>", "</td>", ref index);
                    if (index < 0) break;
                    argDesc.annotation = SubBlockString(functionDefine, "<td>", "</td>", ref index);
                    if (index < 0) break;
                    annotation.argDescs.Add(argDesc);
                }
                //参数的详细选项说明
                foreach (var item in annotation.argDescs)
                {
                    if (index < 0) break;
                    int blockIndex = index;
                    blockIndex = functionDefine.IndexOf(string.Format("<p><strong>{0}</strong></p>", item.name), blockIndex, StringComparison.OrdinalIgnoreCase);
                    if (blockIndex < 0) continue;
                    blockIndex = functionDefine.IndexOf("<tbody>", blockIndex);
                    endIndex = functionDefine.IndexOf("</tbody>", blockIndex + 1);
                    while (blockIndex >= 0 && functionDefine.IndexOf("<td>", blockIndex) < endIndex)
                    {
                        string key = SubBlockString(functionDefine, "<td>", "</td>", ref blockIndex);
                        if (blockIndex < 0) break;
                        string value = SubBlockString(functionDefine, "<td>", "</td>", ref blockIndex);
                        if (blockIndex < 0) break;
                        item.annotation += string.Format("\n{0}:{1}", key, value);
                    }
                }


                annotations.Add(annotation);
            }
            return annotations;
        }
        /// <summary>
        /// 从文本中取出文本块
        /// </summary>
        /// <param name="context"></param>
        /// <param name="head"></param>
        /// <param name="end"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        private string SubBlockString(string context, string head, string end, ref int startIndex)
        {
            startIndex = context.IndexOf(head, startIndex);
            if (startIndex < 0) return string.Empty;
            startIndex += head.Length;
            int endIndex = context.IndexOf(end, startIndex);
            if (endIndex < 0)
            {
                startIndex = -1;
                return string.Empty;
            }
            string reault = context.Substring(startIndex, endIndex - startIndex);
            startIndex = endIndex + end.Length;
            return reault;
        }
        /// <summary>
        /// 下载url中的内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetWebContext(string url)
        {
            string folder = cacheFolder;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string cacheFile = Path.Combine(folder, Path.GetFileName(url)) + ".xml";
            if (File.Exists(cacheFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(cacheFile) < cacheintervalTime)
                return File.ReadAllText(cacheFile);

            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(url);
            Req.UserAgent = "Mozilla/4.0(compatible;MSIE 6.0;Windows NT 5.0; .NET CLR 1.1.4322)";
            Req.Timeout = 30000;
            StreamReader responseReader = new StreamReader(Req.GetResponse().GetResponseStream(), Encoding.Default);
            string responseData = responseReader.ReadToEnd();
            responseReader.Close();
            File.WriteAllText(cacheFile, responseData);
            return responseData;
        }
    }
}
