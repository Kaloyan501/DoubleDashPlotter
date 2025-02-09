using System;
using Microsoft.Win32;

namespace DoubleDashPlotter
{
    internal class InstallDepCheck
    {
        // Check if VC++ Redistributable is installed
        public bool IsVcRedistInstalled()
        {
            const string registryKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
            {
                if (key != null)
                {
                    object installedValue = key.GetValue("Installed");
                    if (installedValue != null && (int)installedValue == 1)
                    {
                        Console.WriteLine("VsRedist is installed.");
                        return true;
                    }
                }
            }
            Console.WriteLine("VcRedist is not installed.");
            return false;
        }

    }


}
