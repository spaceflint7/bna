
namespace System.Diagnostics
{

    public static class Debug
    {
        public static void WriteLine(string message)
        {
            Microsoft.Xna.Framework.GameRunner.Log(message);
        }
    }

}
