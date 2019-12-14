using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sayo_Installer.DataStructure
{
    // json文件信息类
    class FileSystem
    {
        public string MD5 { get; set; }
        public string type { get; set; }
        public long size { get; set; }
        public string Name { get; set; }
        public long mtime { get; set; }
    }
}
