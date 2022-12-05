using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChinarBreakpointRenewal : MonoBehaviour
{
    private bool _isStop;           //是否暂停

    public Slider ProgressBar;      //进度条
    public Text SliderValue;        //滑动条值
    public Button startBtn;        //开始按钮
    public Button pauseBtn;        //暂停按钮
    string Url = "https://downsc.chinaz.net/Files/DownLoad/max/202211/max7265.rar";

    /// <summary>
    /// 初始化UI界面及给按钮绑定方法
    /// </summary>
    void Start()
    {
        //初始化进度条和文本框
        ProgressBar.value = 0;
        SliderValue.text = "0.0%";

        //开始、暂停按钮事件监听
        startBtn.onClick.AddListener(OnClickStartDownload);
        pauseBtn.onClick.AddListener(OnClickStop);
    }


    //开始下载按钮监听事件
    public void OnClickStartDownload()
    {
        //开启协程 *注意真机上要用Application.persistentDataPath路径*
        //StartCoroutine(DownloadFile(Url, Application.streamingAssetsPath + "/Rar/test.rar", CallBack));

        StartCoroutine(Sxer.WWW.WebRequest.RequestUtility.Get_Download(Url, Application.streamingAssetsPath + "/Rar/test111.rar",(aa)=> {

            ProgressBar.value = aa;
            SliderValue.text = Math.Floor(aa * 100) + "%";
        }, CallBack));
    }


    /// <summary>
    /// 协程：下载文件
    /// </summary>
    /// <param name="url">请求的Web地址</param>
    /// <param name="filePath">文件保存路径</param>
    /// <param name="callBack">下载完成的回调函数</param>
    /// <returns></returns>
    IEnumerator DownloadFile(string url, string filePath, Action callBack)
    {
        UnityWebRequest huwr = UnityWebRequest.Head(url); //使用Head方法可以获取到文件的全部长度
        yield return huwr.SendWebRequest();//发送信息请求
        //判断请求或系统是否出错
        if (huwr.isNetworkError || huwr.isHttpError)
        {
            Debug.Log(huwr.error); //出现错误 输出错误信息
        }
        else
        {
            long totalLength = long.Parse(huwr.GetResponseHeader("Content-Length")); //首先拿到文件的全部长度
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
                Debug.Log(fs.Length);
                //判断当前文件是否小于要下载文件的长度，即文件是否下载完成
                if (nowFileLength < totalLength)
                {
                    Debug.Log("还没下载完成");

                    /*使用Seek方法 可以随机读写文件
                     * Seek()  ----------有两个参数 第一参数规定文件指针以字节为单位移动的距离。第二个参数规定开始计算的位置
                     * 第二个参数SeekOrigin 有三个值：Begin  Current   End
                     * fs.Seek(8,SeekOrigin.Begin);表示 将文件指针从开头位置移动到文件的第8个字节
                     * fs.Seek(8,SeekOrigin.Current);表示 将文件指针从当前位置移动到文件的第8个字节
                     * fs.Seek(8,SeekOrigin.End);表示 将文件指针从最后位置移动到文件的第8个字节
                     */
                    fs.Seek(nowFileLength, SeekOrigin.Begin);  //从开头位置，移动到当前已下载的子节位置

                    UnityWebRequest uwr = UnityWebRequest.Get(url); //创建UnityWebRequest对象，将Url传入
                    uwr.SetRequestHeader("Range", "bytes=" + nowFileLength + "-" + totalLength);//修改请求头从n-m之间
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
                            if (_isStop) break;//如果停止跳出循环
                            yield return null;
                            byte[] data = uwr.downloadHandler.data;
                            if (data != null)
                            {
                                long length = data.Length - index;
                                fs.Write(data, (int)index, (int)length); //写入文件
                                index += length;
                                nowFileLength += length;
                                ProgressBar.value = (float)nowFileLength / totalLength;
                                SliderValue.text = Math.Floor((float)nowFileLength / totalLength * 100) + "%";
                                if (nowFileLength >= totalLength) //如果下载完成了
                                {
                                    ProgressBar.value = 1; //改变Slider的值
                                    SliderValue.text = 100 + "%";
                                    /*这句话的作用是：如果callBack方法不为空则执行Invoke
                                     * 注意：
                                     * 1.这里的Invoke可不是Unity的Invoke延迟调用的用法，参考文章：https://blog.csdn.net/liujiejieliu1234/article/details/45312141 从文章中我们可以看到，C#中的Invoke是为了防止winform中子主线程刚开始创建对象时，子线程与主线程并发修改主线程尚未创建的对象属性。
                                     * 因为unity这里只有主线程没有用到子线程可以直接写callBack();
                                     */
                                    callBack?.Invoke();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 下载完成后的回调函数
    /// </summary>
    void CallBack()
    {
        Debug.Log("下载完成");
    }

    /// <summary>
    /// 暂停下载
    /// </summary>
    public void OnClickStop()
    {

        if (_isStop)
        {
            pauseBtn.GetComponentInChildren<Text>().text = "暂停下载";
            Debug.Log("继续下载");
            _isStop = !_isStop;
            OnClickStartDownload();
        }
        else
        {
            pauseBtn.GetComponentInChildren<Text>().text = "继续下载";
            Debug.Log("暂停下载");
            _isStop = !_isStop;
            StopAllCoroutines();
        }
    }
}