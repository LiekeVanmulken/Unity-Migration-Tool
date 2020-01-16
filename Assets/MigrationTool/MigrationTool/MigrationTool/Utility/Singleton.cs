#if UNITY_EDITOR || UNITY_EDITOR_BETA
namespace migrationtool.utility
{
    /// <summary>
    /// Generic singleton
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> where T : class, new()
    {
        private static T instance = null;

        private static readonly object padlock = new object();

        protected Singleton()
        {
        }

        public static T Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }

                    return instance;
                }
            }
        }
    }
}
#endif