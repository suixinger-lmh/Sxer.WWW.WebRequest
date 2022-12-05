using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Sxer.WWW.WebRequest
{
    public static class RequestUtility
    {
        public const string ErrorMsg = "请求异常!";

        //头信息
        public const string Content_Length = "Content-Length";
        public const string Content_Type = "Content-Type";
        public const string Range = "Range";

        #region Post

        /// <summary>
        /// 带表单数据的post
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData">
        ///  List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        ///  formData.Add(new MultipartFormDataSection("key", "value"));
        /// </param>
        /// <param name="doCallBack"></param>
        /// <returns></returns>
        public static IEnumerator Post_Data(string url, List<IMultipartFormSection> postData, Action<string> doCallBack)
        {
            using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, postData))
            {
                yield return unityWebRequest.SendWebRequest();

                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    Debug.Log(unityWebRequest.downloadHandler.text);
                    if (doCallBack != null)
                        doCallBack(unityWebRequest.downloadHandler.text);
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
                Debug.Log(unityWebRequest.GetRequestHeader(Content_Type));
            }
        }

        /// <summary>
        /// 直接传字符串的Post，采用传byte数据的方式
        /// (直接使用UnityWebRequest.Post(url, "data");data会被URL编码)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="addStr"></param>
        /// <param name="doCallBack"></param>
        /// <returns></returns>
        public static IEnumerator Post_AddStr(string url, string addStr, Action<string> doCallBack)
        {
            //UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, "data");
            //data会被URL编码
            //所以采样传字节数据的方式来post

            using (UnityWebRequest unityWebRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                //postParams是传参的对象，通过对json字符串用UTF8编码
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(addStr);
                // Debug.Log(JsonUtility.ToJson(jsonStr));

                unityWebRequest.uploadHandler = new UploadHandlerRaw(postBytes);
                unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
                //设置Content-Type类型
                unityWebRequest.SetRequestHeader(Content_Type, "application/json;charset=utf-8");
                yield return unityWebRequest.SendWebRequest();
                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    Debug.Log(unityWebRequest.downloadHandler.text);
                    if (doCallBack != null)
                        doCallBack(unityWebRequest.downloadHandler.text);
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
            }
              
        }


        public static IEnumerator Post_UploadFile(string url, string fileName,string localPath,Action<string> doCallBack)
        {
            //url中补充上传的文件名
            using (UnityWebRequest unityWebRequest = new UnityWebRequest(url + fileName, UnityWebRequest.kHttpVerbPOST))
            {
                unityWebRequest.uploadHandler = new UploadHandlerFile(localPath);
                yield return unityWebRequest.SendWebRequest();

                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    Debug.Log("upload success");
                    if (doCallBack != null)
                        doCallBack("upload success");
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
            }
         
        }



        #endregion

        #region Get

        /// <summary>
        /// Get方式获取服务端文字
        /// </summary>
        /// <param name="url"></param>
        /// <param name="doCallBack"></param>
        /// <returns></returns>
        public static IEnumerator Get(string url, Action<string> doCallBack)
        {
            using (UnityWebRequest unityWebRequest = UnityWebRequest.Get(url))
            {
                yield return unityWebRequest.SendWebRequest();
                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    Debug.Log(unityWebRequest.downloadHandler.text);
                    if (doCallBack != null)
                        doCallBack(unityWebRequest.downloadHandler.text);
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
            }
        }


        //下载流程：
        //1.检测有效信息，获取文件长度
        //2.本地文件检测，续传检测
        //3.开启下载，文件写入
        public static IEnumerator Get_Download(string url, string filePath,Action<float> doProgress, Action doCallBack)
        {
            UnityWebRequest huwr = UnityWebRequest.Head(url); //使用Head方法可以获取到文件的全部长度
            yield return huwr.SendWebRequest();//发送信息请求
                                               //判断请求或系统是否出错
            if (huwr.isNetworkError || huwr.isHttpError)
            {
                Debug.LogError(huwr.error); //出现错误 输出错误信息
            }
            else
            {
                long totalLength = long.Parse(huwr.GetResponseHeader(Content_Length)); //首先拿到文件的全部长度
                string dirPath = Path.GetDirectoryName(filePath);//获取文件的上一级目录
                if (!Directory.Exists(dirPath)) //判断路径是否存在
                {
                    Directory.CreateDirectory(dirPath);//不存在创建
                }

                /*作用：创建一个文件流，指定路径为filePath,模式为打开或创建，访问为写入
                 * 使用using(){}方法原因： 当同一个cs引用了不同的命名空间，但这些命名控件都包括了一个相同名字的类型的时候,可以使用using关键字来创建别名，这样会使代码更简洁。注意：并不是说两个名字重复，给其中一个用了别名，另外一个就不需要用别名了，如果两个都要使用，则两个都需要用using来定义别名的
                 * using(类){} 括号中的类必须是继承了IDisposable接口才能使用否则报错
                 * 这里没有出现不同命名空间出现相同名字的类属性可以不用using(){}
                 */
                using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    long nowFileLength = fs.Length; //当前文件长度,断点前已经下载的文件长度。
                    //Debug.Log(fs.Length);
                    //判断当前文件是否小于要下载文件的长度，即文件是否下载完成
                    if (nowFileLength < totalLength)
                    {
                        //Debug.Log("还没下载完成");

                        /*使用Seek方法 可以随机读写文件
                         * Seek()  ----------有两个参数 第一参数规定文件指针以字节为单位移动的距离。第二个参数规定开始计算的位置
                         * 第二个参数SeekOrigin 有三个值：Begin  Current   End
                         * fs.Seek(8,SeekOrigin.Begin);表示 将文件指针从开头位置移动到文件的第8个字节
                         * fs.Seek(8,SeekOrigin.Current);表示 将文件指针从当前位置移动到文件的第8个字节
                         * fs.Seek(8,SeekOrigin.End);表示 将文件指针从最后位置移动到文件的第8个字节
                         */
                        fs.Seek(nowFileLength, SeekOrigin.Begin);  //从开头位置，移动到当前已下载的子节位置

                        UnityWebRequest uwr = UnityWebRequest.Get(url); //创建UnityWebRequest对象，将Url传入
                        uwr.SetRequestHeader(Range, "bytes=" + nowFileLength + "-" + totalLength);//修改请求头从n-m之间
                        uwr.SendWebRequest();                      //开始请求
                        if (uwr.isNetworkError || uwr.isHttpError) //如果出错
                        {
                            Debug.Log(uwr.error); //输出 错误信息
                        }
                        else
                        {
                            long index = 0;     //从该索引处继续下载
                            while (nowFileLength < totalLength) //只要下载没有完成，一直执行此循环
                            {
                                //if (_isStop) break;//如果停止跳出循环
                                yield return null;
                                byte[] data = uwr.downloadHandler.data;
                                if (data != null)
                                {
                                    long length = data.Length - index;
                                    fs.Write(data, (int)index, (int)length); //写入文件
                                    index += length;
                                    nowFileLength += length;
                                    if (doProgress != null)
                                        doProgress((float)nowFileLength / totalLength);
                                    //ProgressBar.value = (float)nowFileLength / totalLength;
                                    //SliderValue.text = Math.Floor((float)nowFileLength / totalLength * 100) + "%";
                                    if (nowFileLength >= totalLength) //如果下载完成了
                                    {
                                        //ProgressBar.value = 1; //改变Slider的值
                                        //SliderValue.text = 100 + "%";
                                        /*这句话的作用是：如果callBack方法不为空则执行Invoke
                                         * 注意：
                                         * 1.这里的Invoke可不是Unity的Invoke延迟调用的用法，参考文章：https://blog.csdn.net/liujiejieliu1234/article/details/45312141 从文章中我们可以看到，C#中的Invoke是为了防止winform中子主线程刚开始创建对象时，子线程与主线程并发修改主线程尚未创建的对象属性。
                                         * 因为unity这里只有主线程没有用到子线程可以直接写callBack();
                                         */
                                        //callBack?.Invoke();
                                        if (doCallBack != null)
                                            doCallBack();
                                        yield break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("文件已经存在"); //输出 错误信息
                    }
                }
            }
        }






        #endregion


        #region Put

        public static IEnumerator Put_str(string url,string str, Action<string> doCallBack)
        {
            byte[] myData = System.Text.Encoding.UTF8.GetBytes(str);
            using (UnityWebRequest unityWebRequest = UnityWebRequest.Put(url, myData))
            {
                yield return unityWebRequest.SendWebRequest();

                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    //Debug.Log(unityWebRequest.downloadHandler.text);
                    if (doCallBack != null)
                        doCallBack(unityWebRequest.downloadHandler.text);
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
            }
        }

        #endregion



        #region Head

        /// <summary>
        /// 测试地址有效性
        /// </summary>
        /// <param name="url"></param>
        /// <param name="doCallBack"></param>
        /// <returns></returns>
        public static IEnumerator Head_Only(string url, Action<string> doCallBack)
        {
            using (UnityWebRequest unityWebRequest = UnityWebRequest.Head(url))
            {
                yield return unityWebRequest.SendWebRequest();
                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    //Debug.Log(unityWebRequest.downloadHandler.text);
                    if (doCallBack != null)
                        doCallBack("");
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
            }
        }

        /// <summary>
        /// 获取文件长度
        /// </summary>
        /// <param name="url"></param>
        /// <param name="doCallBack"></param>
        /// <returns></returns>
        public static IEnumerator Head_GetFileLength(string url, Action<string> doCallBack)
        {
            using (UnityWebRequest unityWebRequest = UnityWebRequest.Head(url))
            {
                yield return unityWebRequest.SendWebRequest();
                if (!unityWebRequest.isNetworkError && !unityWebRequest.isHttpError)
                {
                    //Debug.Log(unityWebRequest.downloadHandler.text);
                    //long fileLength = long.Parse();
                    if (doCallBack != null)
                        doCallBack(unityWebRequest.GetResponseHeader(Content_Length));
                }
                else
                {
                    Debug.LogError(unityWebRequest.error);
                    if (doCallBack != null)
                        doCallBack(ErrorMsg);
                }
            }
        }




        #endregion



    }
}

