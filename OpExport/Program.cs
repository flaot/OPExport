using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace OpExport
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;
            var exitCode = 0;

            var stopWatch = Stopwatch.StartNew();
            try
            {
                await OpExport.Main.Work(args);
            }
            catch (AggregateException ae)
            {
                exitCode = -1;
                Console.WriteLine("[Error] One or more exceptions occurred:");

                foreach (var exception in ae.Flatten().InnerExceptions)
                {
                    Console.WriteLine("[Error] " + exception.ToString());
                }
            }
            catch (Exception ex)
            {
                exitCode = -1;
                Console.WriteLine("[Error] " + $"Exception occurred: {ex}");
            }
            finally
            {
                stopWatch.Stop();
            }

            Environment.ExitCode = exitCode;
        }
    }
}
