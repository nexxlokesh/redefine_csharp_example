using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hex
{
    public static class Patterns
    {
        public static long read = 0xd2;
        public static long write = 0x9e;
        public static string[] scanPatterns = new string[]
        {
            "13 40 00 00 F0 3F 00 00 80 3F 01 00 00 00 00 00 00 00 ?? ?? ?? ?? 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? 00 EE",
            "00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 00 00 00 00 00 00 00 00 F0 41",
            "00 C0 3F 00 00 00 3F 00 00 80 3F 00 00 00 40",
            "00 0A 81 EE 10 0A 10 EE 10 8C BD E8 00 00 7A 44 F0 48 2D E9 10 B0 8D E2 02 8B 2D ED 08 D0", // no
            "B4 C8 D6 3F 00 00 80 3F 00 00 80 3F 0A D7 A3 3D 00 00 00 00 00 00 5C 43 00 00 90 42 00 00 B4 42 96 00 00 00 00 00 00 00 00 00 00 3F 00 00 80 3E 00 00 00 00 04 00 00 00 00 00 80 3F", //no
            "14 01 EB 00 00 00 EA 00 60 A0 E3", // no
            "00 00 20 42 00 00 40 40 00 00 70 42 00 00 00 00 00 00 C0 3F",
            "00 00 00 00 00 00 80 3F 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 00 00 80 3F",
            "3f ae 47 81 3f 00 1a b7 ee dc 3a 9f ed 30 00 4f e2 43 2a b0 ee ef 0a 60 f4 43 6a f0 ee 1c 00 8a e2 43 5a f0 ee 8f",
            "FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A5 43",
            "01 00 00 00 02 2b 07 3d"
        };

        public static string[] replacePatterns = new string[]
        {
            "13 40 00 00 F0 3F 00 00 80 4F 01",
            "00 60 40 CD CC 8C 3F 8F C2 F5 3C CD CC CC 3D 06 00 00 00 00 00 F0 FF 00 00",
            "00 C0 30 00 00 00 3F 00 00 80 3F 00 00 00 40",
            "00 0A 81 EE 10 0A 10 EE 10 8C BD E8 00 00 00 00 F0 48",
            "B4 C8 D6 3F 00 00 80 3F 00 00 80 3F 0A D7 A3 3D 00 00 00 00 00 00 5C 43 00 00 90 42 00 00 B4 42 96 00 00 00 00 00 00 00 00 00 00 3C 00 00 80 3C 00 00 00 00 04 00 00 00 00 00 80 3F",
            "14 01 EB 00 00 00 EA 00 60 A0 F3",
            "00 00 20 42 00 00 40 40 00 00 FF FF",
            "00 00 00 00 22 8E C3 40 00 00 00 00 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 80 BF 00 00 00 00 00 00 00 00 00 00 80 3F",
            "bf ae 47 81 3f 00 1a b7 ee dc 3a 9f ed 30 00 4f e2 43 2a b0 ee ef 0a 60 f4 43 6a f0 ee 1c 00 8a e2 43 5a f0 ee 8f",
            "01 00 00 00 92 e4 6f 3d"
        };

        public static string[] scanPatternss = new string[]
        {
            "13 40 00 00 F0 3F 00 00 80 3F 01"
        };
        public static string[] speedx = new string[]
        {
            "01 00 00 00 92 e4 0f 3d",
            "01 00 00 00 92 e4 1f 3d",
            "01 00 00 00 92 e4 2f 3d",
            "01 00 00 00 92 e4 3f 3d",
            "01 00 00 00 92 e4 4f 3d",
            "01 00 00 00 92 e4 5f 3d",
            "01 00 00 00 92 e4 6f 3d"
       };

    }
}
