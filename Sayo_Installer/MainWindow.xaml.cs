using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;
using Sayo_Installer.DataStructure;
using Sayo_Installer;
using System.Threading.Tasks;

namespace Sayo_Installer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        SolidColorBrush black = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        SolidColorBrush red = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        SolidColorBrush green = new SolidColorBrush(Color.FromRgb(0, 191, 96));

        // 安装路径，默认为当前安装程序路径。
        private string installPath = System.AppDomain.CurrentDomain.BaseDirectory;
        private CountDown cd;
        private const string url = "https://txy1.sayobot.cn/client/";
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 添加一条信息
        /// </summary>
        /// <param name="msg">文字</param>
        /// <param name="color">颜色</param>
        private void AddMessage(string msg, Brush color)
        {
            TextBlock label = new TextBlock
            {
                Text = msg,
                Foreground = color
            };

            label.MouseRightButtonDown += Label_MouseRightButtonDown;

            this.messages.Dispatcher.BeginInvoke(new Action(() =>
            {
                messages.Children.Add(label);
                messages.Children.Add(new Separator());

            }));

        }

        /// <summary>
        /// 右键信息复制
        /// </summary>
        private void Label_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            System.Windows.Clipboard.SetText(tb.Text);
            this.AddMessage("Copied to clipboard!", green);
        }

        /// <summary>
        /// 选择安装路径
        /// </summary>
        private void path_change_label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            cd.Pause();
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Please select the install path.";
            DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                cd.Start();
                return;
            }

            // issue: 在有原安装的情况下，修改路径，仍会仅下载差别文件
            // 说明：理论上不允许在有原安装的情况下修改路径，一台电脑仅允许有一个osu!
            installPath = new System.IO.DirectoryInfo(dialog.SelectedPath.Trim()).FullName;
            dialog.Dispose();
            install_path.Content = installPath;
            cd.Done();
        }

        private void Update(Dictionary<string, string> files)
        {
            Thread thread = new Thread(new ThreadStart(() =>
            {
                this.install_path.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.install_path.Content = installPath;
                }));

                cd = new CountDown(10);
                cd.OnTickEvent += (uint t) =>
                {
                    this.counter_label.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.counter_label.Content = string.Format("Installation will begin in {0}s...", t);
                    }));
                };
                cd.Start();
                cd.OnTimerDoneEvent += () =>
                {
                    this.path_change_label.Dispatcher.Invoke(new Action(() =>
                    {
                        this.path_change_label.Visibility = Visibility.Collapsed;
                    }));
                    this.counter_label.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.counter_label.Content = string.Format("Downloading files...\r\n 0/{0} completed",
                            files.Count);
                    }));

                    Thread[] threads = new Thread[files.Count];
                    Dictionary<string, double> progress = new Dictionary<string, double>();
                    int index = 0;
                    foreach (var file in files)
                    {
                        string fileUrl = url + file.Key;
                        string fileSavePath = installPath + '\\' + file.Key;
                        HttpRequestHelper req = new HttpRequestHelper(fileUrl, HttpRequestHelper.HttpReqMode.GET);
                        req.OnRequestDoneEvent += (byte[] data) =>
                        {
                            //this.counter_label.Dispatcher.BeginInvoke(new Action(() =>
                            //{
                            //    this.counter_label.Content =
                            //        string.Format("Downloaded {0}.", file.Key);
                            //}));
                            if (System.IO.File.Exists(fileSavePath)) System.IO.File.Delete(fileSavePath);
                            FileStream fs = new FileStream(fileSavePath, FileMode.CreateNew);
                            fs.Write(data, 0, data.Length);
                            fs.Close();
                            fs.Dispose();

                            int completedThreadCount = 1;
                            foreach (var t in threads)
                            {
                                if (t.ThreadState == System.Threading.ThreadState.Stopped)
                                    ++completedThreadCount;
                                //if (!t.IsAlive)
                                //{
                                //    t.Abort();
                                //}
                            }
                            this.counter_label.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.counter_label.Content =
                                    string.Format("Downloading files...\r\n {0}/{1} completed",
                                    completedThreadCount, threads.Length);
                                if (completedThreadCount == threads.Length)
                                {
                                    // Create Desktop Shortcut
                                    if ((bool)this.create_ink_checkbox.IsChecked)
                                    {
                                        string ShortcutName = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\osu!.lnk";
                                        if (System.IO.File.Exists(ShortcutName))
                                            System.IO.File.Delete(ShortcutName);
                                        WshShell shell = new WshShell();
                                        IWshShortcut wshShortcut = shell.CreateShortcut(ShortcutName);
                                        wshShortcut.TargetPath = this.installPath + "\\osu!.exe";
                                        wshShortcut.Save();
                                    }

                                    this.counter_label.Content = "Installation completed\r\nthis program will be exited in 3 sec.";
                                    Task t = Task.Factory.StartNew(() =>
                                    {
                                        Task.Delay(3000).Wait();
                                        //string programPath = this.installPath + "\\osu!.exe";
                                        //Process p = new Process();
                                        //p.StartInfo.FileName = programPath;
                                        //p.Start();

                                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            System.Windows.Application.Current.Shutdown();
                                        });
                                    });
                                }
                            }));

                        };

                        req.OnPacketReceiveEvent += (long maxSize, long currentSize) =>
                        {
                            try
                            {
                                string url = req.GetUrl();
                                string fileName = url.Substring(url.LastIndexOf('/'));
                                if (progress.ContainsKey(fileName))
                                    progress[fileName] = currentSize * 100.0 / maxSize;
                                else
                                    progress.Add(fileName, currentSize * 100.0 / maxSize);

                                double sum = 0.0;
                                foreach (var f in progress)
                                    sum += f.Value;
                                double avg = sum / files.Count;
                                this.progress_bar.Dispatcher.Invoke(new Action(() =>
                                {
                                    this.progress_bar.Value = avg;
                                }));
                            }
                            catch (Exception)
                            {

                            }
                        };

                        threads[index] = new Thread(new ThreadStart(() =>
                        {
                            try
                            {
                                req.DoRequest();
                            }
                            catch (Exception)
                            {
                                this.counter_label.Dispatcher.Invoke(new Action(() =>
                                {
                                    string url = req.GetUrl();
                                    string filename = url.Substring(url.LastIndexOf('/'));
                                    this.counter_label.Content = string.Format("Failed to download {0}", filename);
                                }));
                            }
                        }));
                        threads[index++].Start();
                    }
                };
                // 更新
                // TODO here
            }));
            thread.Start();
        }

        private void CheckUpdateGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // 更新事件处理
            this.AddMessage("Checking for updates...", black);

            // 自己写更新事件吧（
            // TODO: here

            HttpRequestHelper req = new HttpRequestHelper(url + "/Infos.json", HttpRequestHelper.HttpReqMode.GET);

            req.OnRequestDoneEvent += (byte[] data) =>
            {
                // 在此判断是否更新
                string json = Encoding.GetEncoding("utf-8").GetString(data);

                // C# 自带类的json解析器
                JavaScriptSerializer js = new JavaScriptSerializer();
                FileSystem[] files = js.Deserialize<FileSystem[]>(json);

                MD5 md5 = new MD5CryptoServiceProvider();

                // 要下载的文件内容
                Dictionary<string, string> fileMD5 = new Dictionary<string, string>();

                string location = Utils.GetClientLocation();
                if (!string.IsNullOrEmpty(location))
                {
                    DirectoryInfo directory = new DirectoryInfo(location);
                    FileInfo[] fileInfos = directory.GetFiles();
                    this.installPath = location;
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.AddMessage(string.Format("Local installation found, computing MD5 of files..."), green);
                    }));

                    foreach (FileInfo fi in fileInfos)
                    {
                        // 过滤掉不存在于json的文件
                        if (!Utils.RemoteFileContains(files, fi.Name)) continue;

                        if (fi.Extension == ".dll" || fi.Extension == ".exe")
                        {
                            FileStream fs = new FileStream(fi.FullName, FileMode.Open);
                            byte[] b = new byte[fs.Length];
                            if (fs.Read(b, 0, (int)fs.Length) > 0)
                            {
                                byte[] hash = md5.ComputeHash(b);
                                string shash = "";
                                foreach (byte bi in hash)
                                    shash += string.Format("{0:X2}", bi);
                                fileMD5.Add(fi.Name, shash);
                            }
                            fs.Close();
                            fs.Dispose();
                        }
                    }
                }
                else if (Directory.Exists(Environment.CurrentDirectory + "\\osu!"))
                {
                    this.installPath = Environment.CurrentDirectory + "\\osu!";
                }
                // 如果该文件夹是根目录，那就在根目录下面创建新的文件夹
                // 防止文件安装在磁盘根目录
                else if (Environment.CurrentDirectory ==
                    Directory.GetDirectoryRoot(Environment.CurrentDirectory) ||
                    Directory.GetFiles(Environment.CurrentDirectory).Length
                    + Directory.GetDirectories(Environment.CurrentDirectory).Length != 1)
                {
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\osu!");
                    this.installPath = Environment.CurrentDirectory + "\\osu!";
                }
                else
                {
                    this.installPath = Environment.CurrentDirectory;
                }
                
                foreach (var f in files)
                {
                    if (f.type != "file") continue;

                    string ext = f.Name.Substring(f.Name.LastIndexOf('.'));
                    if (ext == ".dll" || ext == ".exe")
                    {
                        if (!System.IO.File.Exists(installPath + "\\" + f.Name))
                            fileMD5.Add(f.Name, f.MD5);
                        else
                        {
                            FileInfo fi = new FileInfo(Path.Combine(this.installPath, f.Name));
                            TimeSpan ts = fi.LastWriteTimeUtc - new DateTime(1970, 1, 1).ToUniversalTime();
                            if (ts.TotalSeconds >= f.mtime)
                            {
                                continue;
                            }
                            FileStream _fs = new FileStream(installPath + "\\" + f.Name, FileMode.Open);
                            byte[] hash_b = md5.ComputeHash(_fs);
                            string hash_s = "";
                            foreach (var b in hash_b)
                            {
                                hash_s += string.Format("{0:x2}", b);
                            }
                            if (hash_s != f.MD5)
                            {
                                fileMD5.Add(f.Name, f.MD5);
                            }
                        }
                    }
                }
                md5.Dispose();
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.AddMessage(
                        string.Format("Comparing Process completed, {0} files need to update!", fileMD5.Count),
                        green);
                }));

                // 其他线程必须使用 Dispatcher 来更新 UI
                this.Dispatcher.Invoke(new Action(() =>
                {
                    if (fileMD5.Count != 0)
                    {
                        this.AddMessage("Will go to update page in 3s.", green);

                        Thread thread = new Thread(new ThreadStart(() =>
                        {
                            Thread.Sleep(3000);
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.CheckUpdateGrid.Visibility = Visibility.Collapsed;
                                this.UpdateGrid.Visibility = Visibility.Visible;
                                this.Update(fileMD5);
                            }));
                        }));
                        thread.Start();
                    }
                    else
                    {
                        this.AddMessage("No updates for available. The program will exit in 3s.", red);
                        Task t = Task.Factory.StartNew(() =>
                        {
                            Task.Delay(3000).Wait();
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                System.Windows.Application.Current.Shutdown();
                            });
                        });
                    }
                }));
            };

            new Thread(new ThreadStart(() =>
            {
                try
                {
                    // 开始请求
                    req.DoRequest();
                }
                catch (Exception ex)
                {
                    // 同上
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        // this.AddMessage(ex.GetType().FullName + ":", red);
                        this.AddMessage(ex.Message, red);
                        return;
                    }));
                }
            })).Start();
        }

        // 收到退出信号强制结束
        // 可能会导致文件损坏
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 强制结束正在进行的所有线程
            System.Environment.Exit(0);
        }
    }
}