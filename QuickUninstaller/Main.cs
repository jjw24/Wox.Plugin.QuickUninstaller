using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Win32;
using Flow.Launcher.Plugin;

namespace QuickUninstaller
{
    public struct ProgramInfo
    {
        public string DisplayName, UninstallerPath, Version, UninstallerArgument;
        public ProgramInfo(string displayName, string uninstallerPath, string version = null)
        {
            DisplayName = displayName;
            UninstallerPath = uninstallerPath;
            Version = version;
            UninstallerArgument = null;
        }

        //The lexicographical comparison algorithm
        public static bool operator < (ProgramInfo a, ProgramInfo b)
        {
            int alen = a.DisplayName.Length;
            int blen = b.DisplayName.Length;
            bool isALonger = alen > blen;
            int maxlen = isALonger ? alen : blen;
            for(int cur = 0; cur < maxlen; ++cur)
            {
                if(isALonger)
                {
                    if(cur < blen)
                    {
                        if (a.DisplayName[cur] < b.DisplayName[cur]) return true;
                        else if (a.DisplayName[cur] > b.DisplayName[cur]) return false;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if(cur < alen)
                    {
                        if (a.DisplayName[cur] < b.DisplayName[cur]) return true;
                        else if (a.DisplayName[cur] > b.DisplayName[cur]) return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool operator > (ProgramInfo a, ProgramInfo b)
        {
            int alen = a.DisplayName.Length;
            int blen = b.DisplayName.Length;
            bool isALonger = alen > blen;
            int maxlen = isALonger ? alen : blen;
            for (int cur = 0; cur < maxlen; ++cur)
            {
                if (isALonger)
                {
                    if (cur < blen)
                    {
                        if (a.DisplayName[cur] > b.DisplayName[cur]) return true;
                        else if (a.DisplayName[cur] < b.DisplayName[cur]) return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    if (cur < alen)
                    {
                        if (a.DisplayName[cur] > b.DisplayName[cur]) return true;
                        else if (a.DisplayName[cur] < b.DisplayName[cur]) return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }
    public class Main : IPlugin
    {
        private List<ProgramInfo> programInfos = new List<ProgramInfo>();
        public void Init(PluginInitContext context)
        {
            programInfos.Clear();
            RegistryKey lmKey, uninstallKey;
            lmKey = Registry.LocalMachine;

            //Get programs' information (it works on all platforms)
            uninstallKey = lmKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            string[] programKeys = uninstallKey.GetSubKeyNames();
            MakeList(ref programKeys, ref uninstallKey);
            uninstallKey.Close();

            //For 64-bit operating system
            if (Environment.Is64BitOperatingSystem)
            {
                uninstallKey = lmKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
                string[] programKeys64 = uninstallKey.GetSubKeyNames();
                MakeList(ref programKeys64, ref uninstallKey);
                uninstallKey.Close();
            }

            lmKey.Close();

            //For programs that are installed for current user only
            try
            {
                lmKey = Registry.CurrentUser;
                uninstallKey = lmKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                string[] programKeysUser = uninstallKey.GetSubKeyNames();
                MakeList(ref programKeysUser, ref uninstallKey);
                uninstallKey.Close();
            }
            catch { }

            lmKey.Close();

            programInfos.Sort((left, right) => //Sort the list
            {
                if (left < right)
                    return -1;
                else if (left.DisplayName == right.DisplayName)
                    return 0;
                else return 1;
            });
        }
        public List<Result> Query(Query query)
        {
            List<Result> results = new List<Result>();

            /*results.Add(new Result()
            {
                Title = "DEBUG",
                SubTitle = '"' + query.Search + '"'
            });*/

            if (query.Search == "") //When there is no search request
            {
                Init(null); //Update list

                programInfos.ForEach(p =>
                {
                    results.Add(new Result()
                    {
                        Title = "Uninstall " + p.DisplayName,
                        SubTitle = "Version: " + p.Version,
                        IcoPath = "Images\\UninstallerIcon.png",  //相对于插件目录的相对路径
                        Action = e =>
                        {
                            // 处理用户选择之后的操作
                            try
                            { Process process = Process.Start(p.UninstallerPath, p.UninstallerArgument); }
                            catch { }
                            //返回false告诉Wox不要隐藏查询窗体，返回true则会自动隐藏Wox查询窗口
                            return false;
                        }
                    });
                }); //Add all items in 'programInfos' to the results

                results.Add(new Result() //Add refresh message
                {
                    Title = "Refreshed Successfully",
                    SubTitle = "At " + DateTime.Now.ToLongTimeString(),
                    IcoPath = "Images\\RefreshIcon.png"  //相对于插件目录的相对路径
                });
            }
            else //Try to query
            {
                string lowerSearch = query.Search.ToLower(); //Ignore case
                List<string> keywords = lowerSearch.Split(' ').ToList(); //Get keywords
                
                /*keywords.ForEach(p =>
                {
                    results.Add(new Result()
                    {
                        Title = "DEBUG",
                        SubTitle = '"' + p + '"'
                    });
                });*/

                if(keywords.Count == 1 || keywords.Count >= 8)
                {
                    programInfos.ForEach(p =>
                    {
                        if (p.DisplayName.ToLower().Contains(lowerSearch))
                        {
                            results.Add(new Result()
                            {
                                Title = "Uninstall " + p.DisplayName,
                                SubTitle = "Version: " + p.Version,
                                IcoPath = "Images\\UninstallerIcon.png",  //相对于插件目录的相对路径
                                Action = e =>
                                {
                                    // 处理用户选择之后的操作
                                    try
                                    { Process process = Process.Start(p.UninstallerPath, p.UninstallerArgument); }
                                    catch {}
                                    //返回false告诉Wox不要隐藏查询窗体，返回true则会自动隐藏Wox查询窗口
                                    return false;
                                }
                            });
                        }
                    });
                } //Direct query
                else //Multiple-keywords query
                {
                    //To ensure efficient performance, the following three steps are used to search for the results
                    List<ProgramInfo> currentprogramInfos = new List<ProgramInfo>(); //Cache
                    int upBound = keywords.Count - 1;

                    //1. Search by the first keyword and add results to cache
                    programInfos.ForEach(p =>
                    {
                        if (p.DisplayName.ToLower().Contains(keywords[0]))
                        {
                            currentprogramInfos.Add(p);
                        }
                    });

                    //2. Remove all the results that do not contain the remaining keywords (except the last one)
                    for (int i = 1; i < upBound; ++i)
                    {
                        for(int j = 0; j < currentprogramInfos.Count; ++j)
                        {
                            if (!currentprogramInfos[j].DisplayName.ToLower().Contains(keywords[i]))
                                currentprogramInfos.RemoveAt(j--);
                        }
                    }

                    //3. Search by the last keyword in cache and add them to the final results
                    currentprogramInfos.ForEach(p =>
                    {
                        if(p.DisplayName.ToLower().Contains(keywords[upBound]))
                        {
                            results.Add(new Result()
                            {
                                Title = "Uninstall " + p.DisplayName,
                                SubTitle = "Version: " + p.Version,
                                IcoPath = "Images\\UninstallerIcon.png",  //相对于插件目录的相对路径
                                Action = e =>
                                {
                                    // 处理用户选择之后的操作
                                    Process process = Process.Start(p.UninstallerPath, p.UninstallerArgument);
                                    //返回false告诉Wox不要隐藏查询窗体，返回true则会自动隐藏Wox查询窗口
                                    return false;
                                }
                            });
                        }
                    });
                }
            }
            return results;
        }
        void GetStartInfo(ref string raw, ref string argument)
        {
            int cur = 0, length = raw.Length;
            bool willDel = false;
            StringBuilder pathBuilder = new StringBuilder();
            StringBuilder argumentBuilder = new StringBuilder();

            //Ignore '"'
            if (raw[cur] == '"')
            {
                willDel = true;
                ++cur;
            }

            //Get the path of the uninstaller
            for (; cur < length; ++cur)
            {
                char c = raw[cur];
                if ((c == ' ' || c == '"') && pathBuilder.ToString().EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) break; //The path must end with ".exe"
                pathBuilder.Append(c);
            }

            if (willDel) ++cur; //If skip a '"' before

            ++cur; //Ignore ' '

            //Get the argument for starting the uninstaller
            for (; cur < length; ++cur)
            {
                argumentBuilder.Append(raw[cur]);
            }

            //Set both
            raw = pathBuilder.ToString();
            argument = argumentBuilder.ToString();
        }
        void MakeList(ref string[] programKeys, ref RegistryKey uninstallKey)
        {
            //I didn't leave any comments here because I'm toooooo lazy
            RegistryKey programKey;
            foreach (string keyName in programKeys)
            {
                programKey = uninstallKey.OpenSubKey(keyName);
                string programName, uninstallerPath, version;

                try
                {
                    programName = programKey.GetValue("DisplayName").ToString();
                    uninstallerPath = programKey.GetValue("UninstallString").ToString();
                }
                catch
                {
                    continue;
                }

                if (programName == null) continue;

                try
                {
                    version = programKey.GetValue("DisplayVersion").ToString();
                }
                catch
                {
                    version = "Unknown";
                }

                ProgramInfo currentInfo = new ProgramInfo(programName, uninstallerPath, version);
                GetStartInfo(ref currentInfo.UninstallerPath, ref currentInfo.UninstallerArgument);
                programInfos.Add(currentInfo);
                programKey.Close();
            }
        }
    }
}
