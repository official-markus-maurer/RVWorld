using System;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Extensions
{
    public class ExtHelper
    {
        public static void AddIns(Form mainForm, Func<bool> IsWorking, Action UpdateDats, Action UpdateMIA)
        {
        }

        public static void DatVaultRightClick()
        {
        }

        public static void SendErrorMessage(string message, int vMajor, int vMinor, int vBuild)
        {
        }

        public static void StartUpChecks(Version Version)
        {
        }

        public static void writeSettings(XDocument cfgXML)
        {
        }
        public static void readSettings(XDocument cfgXML)
        {
        }
        
        public static bool ValidateKey(string key)
        {
            return true;
        }
    }
}
