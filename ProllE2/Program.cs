using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using NDesk.Options;

namespace ProllE2
{
    class Program
    {
        static void Main(string[] args)
        {
            bool showHelp = false;
            Backend backend = Backend.Interpreter;

            var p = new OptionSet()
            {
                { "h|?|help", "show help", v => { showHelp = v != null; } },
                { "b|backend=", "the {BACKEND} to use, [i|interp|interpreter]|[r|recomp|recompiler]", v => { backend = ParseBackend(v).Value; } },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("ProllE2: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try ProllE2 --help' for more information.");
                return;
            }
            catch (InvalidOperationException)
            {
                Console.Write("ProllE2: ");
                Console.WriteLine("Invalid backend");
                Console.WriteLine("Try ProllE2 --help' for more information.");
                return;
            }

            if (showHelp)
            {
                ShowHelp(p);
                return;
            }

            Cpu cpu;

            if (backend == Backend.Recompiler)
            {
                cpu = new Recompiler();
            }
            else
            {
                cpu = new Interpreter();
            }

            string path = AppDomain.CurrentDomain.BaseDirectory + "\\" + string.Join("", extra.ToArray());

            try
            {
                cpu.LoadProgram(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("ProllE2:");
                Console.WriteLine(e.Message);
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            bool res = cpu.Run();

            sw.Stop();

            if (res)
            {
                Console.WriteLine("Program executed successfully in " + sw.ElapsedTicks + " ticks");
            }
            else
            {
                Console.WriteLine("shit broke");
            }
        }

        static Backend? ParseBackend(string input)
        {
            string str = input.ToLower();
            if (str == "i" || str == "interp" || str == "interpreter")
            {
                return Backend.Interpreter;
            }
            else if (str == "r" || str == "recomp" || str == "recompiler")
            {
                return Backend.Recompiler;
            }
            return null;
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: ProllE2 [OPTIONS] [PATH]");
            Console.WriteLine("If no backend is specified, interpreter is used.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }

        enum Backend
        {
            Interpreter,
            Recompiler,
        }
    }
}
