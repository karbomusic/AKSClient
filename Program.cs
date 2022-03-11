using System;
using System.Net;
using System.Net.Http;

/// <summary>
/// This app functions as a web client that an send multiple asynchronus requests to a remote enpoint.
/// It uses a nested loop to send blocks of spewed requests.
/// Usage: AKSClientTest.exe <URL> <NumRequests> <DelayBetweenBlocks> <DelayBetweenRequests> 
///  
/// URL: The remote endpoint. If using the sister server app ?delay= will tell the endpoint to hold
/// the request for a random number of seconds between 0 and the delay value.
/// 
/// NumRequests: Number of requests * number of requests. So 10 = 10 blocks of 10 or 100 requests total.
/// 
/// DelayBetweenRequests: The micro delay between every request.
/// 
/// DelayBetweenBlocks: This is how long to pause per block of requests so if NumRequests = 10 and DelayBetwenBLocks = 1
///                     then there will be a 1 second pause every 10 requests.
///                
/// </summary>
namespace AKSTest 
{
    internal class Program
    {
        static String testURL = String.Empty;
        static int numTests = 100;
        static int delay = 1000;
        static int microDelay = 50;
        static String logFilePath = "AKSClientTestLog-" + DateTime.Now.Ticks + ".log";
        private static readonly object logLock = new object();
        private static readonly object rndLock = new object();

        static void Main(string[] args)
        {
            PrintBanner();

            if (args.Count() > 3)
            {
                testURL = args[0];
                numTests = Convert.ToInt32(args[1]);
                delay = Convert.ToInt32(args[2]);
                microDelay = Convert.ToInt32(args[3]);
            }
            else
            {
                Console.WriteLine("Not enough arguments!");
                Console.WriteLine("Usage: AKSClientTest.exe <URL> <NumRequests> <DelayBetweenBlocks> <DelayBetweenRequests>");
                Console.ReadKey();
                Environment.Exit(0);    
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\r\nStarting test: URL{0}\r\nPassess={1}\r\nTotal Requests={2}\r\nDelay={3}", testURL, numTests, numTests * numTests, delay);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Log file={0}", logFilePath);
            Console.ForegroundColor = ConsoleColor.Gray;

            RunConnectivityTest(testURL, numTests, delay * 1000); // thread.start()
            Console.ReadKey();
        }

        // Make i iterations of r requests so numPasses = 10 = 10 passes (i) of 10 request blocks (r) each = 100 requests
        static void RunConnectivityTest(string URL, int numPasses, int waitDelay)
        {
            for (int i = 0; i < numPasses; i++)
            {
                for (int r = 0; r < numPasses; r++)
                {
                    Thread t = new Thread(() => MakeRequest(URL, numPasses, waitDelay, i, r));
                    t.Start();
                    System.Threading.Thread.Sleep(microDelay);
                }
                System.Threading.Thread.Sleep(waitDelay);
            }
            Console.WriteLine("\r\nTest Completed.");
        }

        // The actual request happens here.
        static void MakeRequest(string URL, int numPasses, int waitDelay, int i, int r)
        {
            string requestID = String.Empty;
            requestID = GetRandomHexString();

            // were forcing all args at this point so this check isn't really needed.
            // but appending the reqid is needed.
            URL = URL.Contains("?delay=") ? URL += "&reqid=" + requestID : URL += "?reqid=" + requestID;

            try
            {
                HttpWebRequest client = (HttpWebRequest)WebRequest.Create(URL);
                client.KeepAlive = false;
                HttpWebResponse response = (HttpWebResponse)client.GetResponse();
                Console.WriteLine("Passes:{0}/{1} - {2}: {3} {4}", i, r, GetTimeStamp(), requestID, response.StatusDescription);
                AppendLog(i + "/" + r + " " + GetTimeStamp() + ": ReqID: " + requestID + "-" + response.StatusDescription + "\r\n");
            }
            catch (Exception ex)
            {
                string message = String.Empty;
                if (ex.InnerException != null && ex.InnerException.HResult == -2147467259)
                {
                    // the connection timed out or similar which is the same as the repro.
                    // we can't be 100% sure this isn't a false positive though.
                    message = "Repro? --> " + ex.InnerException.Message;
                }
                else
                {
                    // non-repro failed request
                    message = ex.Message;
                }
                Console.WriteLine("Passes:{0}/{1} - {2}: {3} {4}", i, r, GetTimeStamp(), requestID, "ERROR: " + message);
                AppendLog(i + "/" + r + " " + GetTimeStamp() + ": ReqID: " + requestID  + "-" + message + "\r\n");
            }

        }

        static String GetTimeStamp()
        {
            return DateTime.Now.ToString("MM/dd/yy hh:mm:ss.fffffff tt");
        }
        static void AppendLog(string msg)
        {
            lock (logLock)
            {
                try
                {
                    StreamWriter SW;
                    SW = File.AppendText(logFilePath);
                    SW.WriteLine(msg);
                    SW.Close();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error writing to log {0} \r\n {1}", logFilePath, ex.Message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
        }

        static Random r = new Random();
        static String GetRandomHexString()
        {
            lock (rndLock)
            {
                int A = r.Next(10000, 1500000);
                return A.ToString("X");
            }
        }
        
        static void PrintBanner()
        {
            Console.WriteLine("====================================================================    ");
            Console.WriteLine("                     Web Request Test Client                            ");
            Console.WriteLine("====================================================================\r\n");
        }
    }
}