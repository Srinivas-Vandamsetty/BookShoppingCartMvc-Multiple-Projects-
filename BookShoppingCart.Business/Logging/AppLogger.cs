using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Logging
{
    public class AppLogger
    {
        private static readonly Lazy<AppLogger> _instance =
            new Lazy<AppLogger>(() => new AppLogger());

        private AppLogger() { }

        public static AppLogger Instance => _instance.Value;

        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
        }

        public void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
        }
    }
}
