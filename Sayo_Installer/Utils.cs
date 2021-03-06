﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sayo_Installer
{
    static class Utils
    {
        public static string GetClientLocation()
        {
            RegistryKey lmrk = Registry.LocalMachine;
            RegistryKey usrk = Registry.CurrentUser;
            // HKEY_CURRENT_USER\Software\osu!
            RegistryKey osuKey = usrk.OpenSubKey(@"Software\osu!", false);
            string ret = string.Empty;
            if (null != osuKey)
            {
                string osuID = osuKey.GetValue("UninstallID").ToString();

                RegistryKey swrk = lmrk.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{" + osuID + "}", false);
                if (swrk != null)
                {
                    object pathoj = swrk.GetValue("DisplayIcon");
                    swrk.Close();
                    if (null != pathoj)
                    {
                        string FilePath = pathoj.ToString();
                        string DicPath = FilePath.Substring(0, FilePath.LastIndexOf('\\'));
                        if (System.IO.Directory.Exists(DicPath))
                            ret = DicPath;
                    }
                }
                osuKey.Close();
            }
            usrk.Close();
            lmrk.Close();
            return ret;
        }

        public static bool RemoteFileContains(DataStructure.FileSystem[] fs, string fileName)
        {
            foreach (var f in fs)
            {
                if (f.type != "file") continue;
                if (f.Name == fileName)
                    return true;
            }
            return false;
        }
    }
}
