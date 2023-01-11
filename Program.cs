using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;

namespace RhinoPluginRegistryKeyCleaning
{
    internal class Program
    {
        public struct TargetRegistryPair
        {
            public RegistryKey TargetRegistryKey;
            public string TargetSubKeyName;
            public string FullName { get { return TargetRegistryKey.ToString() + "\\" + TargetSubKeyName.ToString(); } }
        }

        private static List<TargetRegistryPair> foundRegistryKeyList = new List<TargetRegistryPair>();

        private static List<RegistryKey> possibleRegistryKeyPath = new List<RegistryKey>();

        private static List<String> defaultPluginIdList = new List<string>
        {
            "e9da8058-8baf-44c3-846d-7c174337ab3e",
            "e0f34b71-f012-441a-9f37-18939447e90e"
        };

        private static string defaultRegistryKeyUserSubName = "S-1-5-21";

        private static string inputPluginId;

        private static List<string> checkingIdList = new List<string>();

        private static string deleteInput;

        static void Main(string[] args)
        {
            InitPossibleRegistryKeyPath();
            Console.WriteLine("RhinoPluginRegistryKeyCleaning by Tuesday_Hu");
            Console.WriteLine("Please input plugin ID. Will use default keys if input is null.");
            inputPluginId = Console.ReadLine();
            if (inputPluginId != "")
            {
                checkingIdList.Add(inputPluginId);
            }
            else
            {
                checkingIdList = defaultPluginIdList;
            }

            foreach (RegistryKey possibleRootKey in possibleRegistryKeyPath)
            {
                foreach (string id in checkingIdList)
                {
                    SearchRegistryKey(id, possibleRootKey);
                }
            }

            Console.WriteLine("Found {0} paths, listed as follow:", foundRegistryKeyList.Count);
            foreach (TargetRegistryPair trp in foundRegistryKeyList)
            {
                Console.WriteLine(trp.FullName);
            }
            Console.WriteLine();

            Console.WriteLine("Input \"y\" for deleting these keys; or input others to exit:");
            deleteInput = Console.ReadLine();

            if (deleteInput.ToLower() == "y")
            {
                foreach (TargetRegistryPair trp in foundRegistryKeyList)
                {
                    if (trp.TargetRegistryKey.OpenSubKey(trp.TargetSubKeyName) != null)
                    {
                        if (trp.TargetRegistryKey.OpenSubKey(trp.TargetSubKeyName).SubKeyCount == 0)
                        {
                            trp.TargetRegistryKey.DeleteSubKey(trp.TargetSubKeyName);
                        }
                        else
                        {
                            trp.TargetRegistryKey.DeleteSubKeyTree(trp.TargetSubKeyName);
                        }
                    }
                    trp.TargetRegistryKey.Close();
                }
                Console.WriteLine("Finish");
                Console.ReadLine();
            }
            else
            {
                return;
            }
        }

        static void InitPossibleRegistryKeyPath()
        {
            //查找HKEY_CURRENT_USER和HKEY_LOCAL_MACHINE中的地址
            possibleRegistryKeyPath.Add(Registry.CurrentUser.OpenSubKey(@"Software\McNeel\Rhinoceros", true));
            possibleRegistryKeyPath.Add(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\McNeel\Rhinoceros"));

            //查找HKY_USER中的地址
            RegistryKey registryKeyUsers = Registry.Users;
            string[] registryKeyUsersSubList = Registry.Users.GetSubKeyNames();
            foreach (string subName in registryKeyUsersSubList)
            {
                if (subName.Contains(defaultRegistryKeyUserSubName))
                {
                    string possiblePath = subName + @"\Software\McNeel\Rhinoceros";
                    RegistryKey possibleRegistryKey = registryKeyUsers.OpenSubKey(possiblePath, true);
                    if (possibleRegistryKey != null)
                    {
                        possibleRegistryKeyPath.Add(possibleRegistryKey);
                    }
                }
            }

            Console.WriteLine("PossibleRegistryKeyPath been inited, with {0} pathes.", possibleRegistryKeyPath.Count);
            foreach (RegistryKey rk in possibleRegistryKeyPath)
            {
                Console.WriteLine(rk.ToString());
            }
            Console.WriteLine();
        }

        static void SearchRegistryKey(string pluginId, RegistryKey searchRegistryKey)
        {
            string[] subKeyNames = searchRegistryKey.GetSubKeyNames();

            foreach (string subKeyName in subKeyNames)
            {
                RegistryKey subKey = searchRegistryKey.OpenSubKey(subKeyName, true);
                if (subKey.SubKeyCount > 0)
                {
                    SearchRegistryKey(pluginId, subKey);
                }
                if (pluginId.ToUpper().Equals(subKeyName.ToUpper()))
                {
                    TargetRegistryPair targetRegistryPair = new TargetRegistryPair();
                    targetRegistryPair.TargetRegistryKey = searchRegistryKey;
                    targetRegistryPair.TargetSubKeyName = subKeyName;
                    foundRegistryKeyList.Add(targetRegistryPair);
                }
            }
        }
    }
}
