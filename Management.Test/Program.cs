using AydenIO.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AydenIO.Management.Test {
    class Program {
        static void Main(string[] args) {
            Win32_OperatingSystem os = ManagementSession.GetFactory<Win32_OperatingSystem>().GetInstances().OfType<Win32_OperatingSystem>().FirstOrDefault();

            DateTime lastBootUpTime = os.LastBootUpTime;

            ManualResetEventSlim exitEv = new ManualResetEventSlim(false);

            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => {
                exitEv.Set();
            };

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => {
                e.Cancel = true;

                exitEv.Set();
            };

            int writeAtX = -1;
            int writeAtY = -1;
            bool writeAtSet = false;

            while (!exitEv.IsSet) {
                int currX = Console.CursorLeft;
                int currY = Console.CursorTop;
                bool cursorVisible = Console.CursorVisible;

                Console.CursorVisible = false;

                if (!writeAtSet) {
                    writeAtX = currX;
                    writeAtY = currY;
                }

                Console.CursorLeft = writeAtX;
                Console.CursorTop = writeAtY;

                Console.Write("\r" + new string(' ', Console.BufferWidth - 1));
                Console.Write("\rSystem uptime: " + (DateTime.Now - lastBootUpTime).ToString());

                if (!writeAtSet) {
                    Console.WriteLine();

                    currX = Console.CursorLeft;
                    currY = Console.CursorTop;

                    writeAtSet = true;
                }

                Console.CursorLeft = currX;
                Console.CursorTop = currY;
                Console.CursorVisible = cursorVisible;

                exitEv.Wait(1000 / 10);
            }

            Console.Write("Press any key to continue...");
            Console.ReadKey(true);
            Console.WriteLine();
        }
    }
}
