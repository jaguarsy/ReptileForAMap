using Reptile.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Reptile
{
    public class ReptileCore
    {
        //目标地址，初始地址
        private string targetUrl;
        //负责显示消息的Watcher
        private IWatcher watcher;
        //爬虫线程
        private Thread reptileThread;
        private Regex regex;

        //Http请求操作类
        private HttpHelper httpHelper = new HttpHelper();

        //构造函数，传入目标地址
        public ReptileCore(string url)
        {
            this.targetUrl = url;
        }

        //向爬虫类注册负责更新消息的watcher
        public void Register(IWatcher watcher)
        {
            this.watcher = watcher;
        }

        //启动爬虫线程
        public void BeginCatch()
        {
            reptileThread = new Thread(new ThreadStart(process));
            reptileThread.IsBackground = true;
            reptileThread.Start();
        }

        //关闭爬虫线程
        public void AbortCatch()
        {
            if (reptileThread == null) return;
            if (reptileThread.IsAlive)
            {
                reptileThread.Abort();
            }
        }

        private DateTime? lastUpdateTime, currentUpdateTime;
        //爬虫线程主方法
        private void process()
        {
            lastUpdateTime = null;
            while (true)
            {
                watcher.Log("开始抓取数据，当前抓取地址为:" + targetUrl);

                //获取网页内容
                var html = getHtml(targetUrl);
                //获取当前更新时间
                currentUpdateTime = getUpdateTime(html);
                //判断数据是否已更新
                if (!lastUpdateTime.Equals(currentUpdateTime))
                {
                    watcher.Log("当前更新时间为：" + currentUpdateTime.ToString());
                    getData(html);
                }
                else
                {
                    watcher.Log("数据仍未更新");
                }
                lastUpdateTime = currentUpdateTime;
                watcher.Log("等待一分钟");
                sleep();
            }
        }

        private void save()
        {
            try
            {
                //dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                watcher.Log(ex.Message);
            }
        }

        //获取交通数据，全部道路1-10，高速道路31-40，普通道路61-70
        private DataTable getData(string html)
        {
            DataTable result = new DataTable();

            int[] way = { 0, 30, 60 };
            int wayIndex, tmpIndex;

            //页面正则
            regex = new Regex(@"td_road[\s\S]*?h3\>([^\<]+)[\s\S]*?span[^\>]+([^\<]+)[\s\S]*?center"">([^\<]+)[\s\S]*?center"">([^\<]+)[\s\S]*?center"">([^\<]+)");
            //获取所有匹配历史
            var matches = regex.Matches(html);

            for (var i = 0; i < 3; i++)
            {
                wayIndex = way[i];
                for (var j = 0; j < 10; j++)
                {
                    tmpIndex = wayIndex + j;
                    var match = matches[tmpIndex];
                    watcher.Log((tmpIndex + 1) + ":" + match.Groups[1].Value + " " + match.Groups[3].Value + " " +
                        match.Groups[4].Value + " " + match.Groups[5].Value + " " + match.Groups[6].Value);
                }

            }

            return result;
        }

        //获取更新时间
        private DateTime getUpdateTime(string html)
        {
            //页面正则
            regex = new Regex(@"更新时间：([^\<]+)");
            //获取所有匹配历史
            var matches = regex.Matches(html);

            //返回所匹配的第一条
            return DateTime.Parse(matches[0].Groups[1].Value);
        }

        //使用Get方法获取html
        private string getHtml(string url)
        {
            return httpHelper.GetHtml(
                new HttpItem()
                {
                    URL = url,
                    UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.65 Safari/537.36"
                }).Html;
        }

        private void sleep()
        {
            Thread.Sleep(60000);
        }
    }
}
