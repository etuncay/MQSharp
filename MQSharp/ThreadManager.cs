using System.Threading;

namespace MQSharp
{

    public class ThreadManager
    {
        public static void StartThread(ThreadStart threadStart)
        {
            new Thread(threadStart).Start();
        }

        public static void SleepThread(int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
        }
    }
}
