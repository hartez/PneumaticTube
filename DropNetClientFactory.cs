using System.IO;
using System.Reflection;
using DropNetRT;

namespace PneumaticTube
{
    internal static class DropNetClientFactory
    {
        public static DropNetClient CreateDropNetClient()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using(var stream = assembly.GetManifestResourceStream("PneumaticTube.apikeys.txt"))
            {
                using(var textStreamReader = new StreamReader(stream))
                {
                    var key = textStreamReader.ReadLine();
                    var secret = textStreamReader.ReadLine();

                    return new DropNetClient(key, secret);
                }
            }
        }
    }
}