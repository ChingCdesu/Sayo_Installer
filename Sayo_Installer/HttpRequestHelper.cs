using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Sayo_Installer
{
    class HttpRequestHelper
    {
        /// <summary>
        /// Http 请求方法枚举。
        /// </summary>
        public enum HttpReqMode
        {
            GET,
            POST,
        }

        private string url;
        private HttpReqMode mode;

        public HttpRequestHelper(string url, HttpReqMode mode)
        {
            this.url = url;
            this.mode = mode;
        }

        public delegate void OnRequestDone(byte[] data);
        public delegate void OnPacketReceive(long maxSize, long currentSize);
        public event OnRequestDone OnRequestDoneEvent;
        public event OnPacketReceive OnPacketReceiveEvent;

        public void DoRequest(byte[] reqBody = null, Dictionary<string, string> headers = null)
        {
            byte[] data = reqBody;

            // 创建一个 Http 请求
            HttpWebRequest request = WebRequest.Create(this.url) as HttpWebRequest;

            // string contentType = string.Empty;
            // request.ContentType =
            //     headers != null && headers.TryGetValue("ContentType", out contentType) ?
            //     "*/*" : contentType;
            // 设置 Content-Type

            if (headers != null)
            {
                request.Headers = new WebHeaderCollection();
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
            }

            request.Method = this.mode.ToString();
            // 设置请求方法

            request.KeepAlive = false;

            if (this.mode == HttpReqMode.POST)
            {
                request.ContentLength = data.Length;
                // 设置请求长度

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                    // 写入数据
                }
            }

            // 获取当前Http请求的响应实例
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException(response.StatusCode, response.Headers);
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                byte[] datas = { };
                // 文件类型
                if (response.ContentType != "application/json")
                {
                    long contentLength = response.ContentLength;
                    datas = new byte[contentLength];
                    int size = 0, realsize = 0;
                    // 64kb 缓存（一般是CPU 一级缓存的值）
                    byte[] buffer = new byte[1024 * 64];
                    while ((size = responseStream.Read(buffer, 0, 1024 * 64)) > 0)
                    {
                        Array.Copy(buffer, 0, datas, realsize, size);
                        realsize += size;
                        OnPacketReceiveEvent?.Invoke(contentLength, realsize);
                    }
                }
                // json类型
                else
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        responseStream.CopyTo(ms);
                        datas = ms.GetBuffer();
                        ms.Close();
                    }
                }
                responseStream.Close();
                response.Close();
                OnRequestDoneEvent?.Invoke(datas);
            }
        }

        public string GetUrl()
        {
            return this.url;
        }
    }
    class HttpRequestException : Exception
    {
        public HttpRequestException(HttpStatusCode code, WebHeaderCollection headers)
        {
            this.statusCode = code;
            this.headers = headers;
        }

        public string What()
        {
            return this.statusCode.ToString();
        }

        public HttpStatusCode statusCode { get; private set; }
        public WebHeaderCollection headers { get; private set; }
    }
}