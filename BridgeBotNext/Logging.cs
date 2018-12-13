using System.IO;
using System.Reflection;
using Easy.Logger;
using Easy.Logger.Interfaces;

namespace BridgeBotNext
{
    public class Logging
    {
        public static ILogService LogService = Log4NetService.Instance;
    }
}