using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RED_X_CLOUD_CONTROL_BASIC
{
    public class RenaultSniper
    {
        public static readonly string OriginalPattern = "FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 3F 33 33 13 40";
        public static readonly string PatchPattern    = "FF FF FF FF 08 00 00 00 00 00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 3E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 33 33 13 40";

        public static List<long> SniperScopeAddresses = new List<long>();
        public static List<byte[]> OriginalScopeValues = new List<byte[]>();
        public static bool IsScopeLoaded = false;
        public static bool IsScopeActive = false;

        private static byte[] HexStringToBytes(string hex)
        {
            return hex.Split(' ').Select(b => byte.Parse(b, NumberStyles.HexNumber)).ToArray();
        }

        // SNIPERSCOPELOAD (matches Renault app.py / Memory.py method)
        public static async Task<string> SniperScopeLoad()
        {
            try
            {
                SniperScopeAddresses.Clear();
                OriginalScopeValues.Clear();
                IsScopeLoaded = false;
                IsScopeActive = false;

                REDX mem = new REDX();
                if (!mem.SetProcess(new[] { "HD-Player" }))
                {
                    return "STATUS: Emulator Not Found!!";
                }

                var results = (await mem.AoBScan(OriginalPattern)).ToList();
                SniperScopeAddresses = results;

                if (SniperScopeAddresses.Count > 0)
                {
                    IsScopeLoaded = true;
                    return $"STATUS: Sniper Scope Loaded ({SniperScopeAddresses.Count} addr)";
                }
                else
                {
                    return "STATUS: Scope — 0 matches found";
                }
            }
            catch (Exception ex)
            {
                return $"STATUS: Scope Scan Error: {ex.Message}";
            }
        }

        // ACTIVATELOADEDSCOPE / Sniper_scope_on
        public static string SniperScopeOn()
        {
            if (!IsScopeLoaded || SniperScopeAddresses.Count == 0)
            {
                return "STATUS: Scope not loaded yet!";
            }

            try
            {
                REDX mem = new REDX();
                if (!mem.SetProcess(new[] { "HD-Player" }))
                {
                    return "STATUS: Emulator Not Found!!";
                }

                byte[] patchBytes = HexStringToBytes(PatchPattern);
                OriginalScopeValues.Clear();

                foreach (long addr in SniperScopeAddresses)
                {
                    byte[] currentBytes = mem.ReadBytes(addr, patchBytes.Length);
                    if (currentBytes != null)
                    {
                        OriginalScopeValues.Add(currentBytes);
                        mem.WriteBytes(addr, patchBytes);
                    }
                }

                IsScopeActive = true;
                return "STATUS: Sniper Scope Enabled";
            }
            catch (Exception ex)
            {
                return $"STATUS: Scope Enable Error: {ex.Message}";
            }
        }

        // REMOVELOADEDSCOPE / Sniper_scope_off
        public static string SniperScopeOff()
        {
            if (!IsScopeActive || SniperScopeAddresses.Count == 0)
            {
                return "STATUS: Scope not active!";
            }

            try
            {
                REDX mem = new REDX();
                if (!mem.SetProcess(new[] { "HD-Player" }))
                {
                    return "STATUS: Emulator Not Found!!";
                }

                for (int i = 0; i < SniperScopeAddresses.Count; i++)
                {
                    long addr = SniperScopeAddresses[i];
                    if (i < OriginalScopeValues.Count && OriginalScopeValues[i] != null)
                    {
                        mem.WriteBytes(addr, OriginalScopeValues[i]);
                    }
                    else
                    {
                        mem.AobReplace(addr, OriginalPattern);
                    }
                }

                IsScopeActive = false;
                return "STATUS: Sniper Scope Disabled";
            }
            catch (Exception ex)
            {
                return $"STATUS: Scope Disable Error: {ex.Message}";
            }
        }

        // Toggle helper
        public static string ToggleSniperScope()
        {
            if (IsScopeActive)
            {
                return SniperScopeOff();
            }
            else
            {
                return SniperScopeOn();
            }
        }
    }
}
