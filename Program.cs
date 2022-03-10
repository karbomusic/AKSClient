using System;
using System.Net;
using System.Net.Http;

namespace AKSTest // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static HttpClient? client;
        static String testURL= String.Empty;
        static int numTests = 100;
        static int delay = 1000;
        static String logFilePath = "AKSTestLog-" + DateTime.Now.Ticks + ".log";
        static object logLock = new object();

        static void Main(string[] args)
        {
           PrintBanner();

            if (args.Count() > 0)
            {
                testURL = args[0];
                numTests = Convert.ToInt32(args[1]);
                delay = Convert.ToInt32(args[2]);
            }
            else
            {
                testURL = "https://bing.com";
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\r\nStarting test: URL{0}\r\nPassess={1}\r\nTotal Requests={2}\r\nDelay={3}", testURL, numTests, numTests*numTests, delay);
            Console.ForegroundColor = ConsoleColor.Gray;
            RunConnectivityTest2(testURL, numTests, delay*1000);
            Console.ReadKey();
        }

        // Make i iterations of r requests so numPasses = 10 = 10 passes (i) of 10 request blocks (r) each = 100 requests
        static async void RunConnectivityTest(string URL, int numPasses, int waitDelay)
        { 
            for(int i=0; i< numPasses; i++)
            {
                for (int r = 0; r < numPasses; r++)
                {
                    try
                    {
                        client = new HttpClient();
                        var result =  await client.GetAsync(URL);
                        Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt"), result.StatusCode);
                        AppendLog(i + " " + DateTime.Now.ToLongTimeString() + ": " + result.StatusCode + "\r\n");
                        if (client != null)
                        {
                            client.Dispose();
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, DateTime.Now.ToString("MM/dd/yy hh: mm:ss.fffffff tt"), "ERROR: " + ex.Message);
                        AppendLog(i + " " + DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt") + ": " + "ERROR: " + ex.Message + "\r\n");
                    }

                    System.Threading.Thread.Sleep(200);
                }
                System.Threading.Thread.Sleep(waitDelay);
            }
        }

        // Make i iterations of r requests so numPasses = 10 = 10 passes (i) of 10 request blocks (r) each = 100 requests
        static void RunConnectivityTest2(string URL, int numPasses, int waitDelay)
        {
            for (int i = 0; i < numPasses; i++)
            {
                for (int r = 0; r < numPasses; r++)
                {
                    Thread t = new Thread(() => MakeRequest(URL, numPasses, waitDelay, i, r));
                    t.Start();
                    System.Threading.Thread.Sleep(100);
                }
                System.Threading.Thread.Sleep(waitDelay);
            }
        }

        static void MakeRequest(string URL, int numPasses, int waitDelay, int i, int r)
        {
            try
            {
                client = new HttpClient();
                var result = client.GetAsync(URL);
                Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, GetTimeStamp(), result.Result.StatusCode);
                lock (logLock)
                {
                    AppendLog(i + " " + GetTimeStamp() + ": " + result.Result.StatusCode + "\r\n");
                }
                if (client != null)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Passes:{0}/{1} - {2}: {3}", i, r, GetTimeStamp(), "ERROR: " + ex.Message);
                lock (logLock)
                {
                    AppendLog(i + " " + GetTimeStamp() + ": " + "ERROR: " + ex.Message + "\r\n");
                }
            }
        }

        static String GetTimeStamp()
        {
            return DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt");
        }
        static async void AppendLog(string msg)
        {
            try
            {
                await System.IO.File.AppendAllTextAsync(logFilePath, msg);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error writing to log {0} \r\n {1}", logFilePath, ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        static void PrintBanner()
        {
            Console.WriteLine("====================================================================");
            Console.WriteLine("            AKS Pod Test Client         \r\n");
            Console.WriteLine("Usage: AKSRequestClient.exe [<URL> <NumRequets> <Delay>]            ");
            Console.WriteLine("If all args are ommited, https://bing.com is used with 100 requests.");
            Console.WriteLine("====================================================================");
        }
    }
}