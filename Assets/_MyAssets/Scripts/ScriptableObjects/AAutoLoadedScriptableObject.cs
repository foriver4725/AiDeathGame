namespace MyScripts
{
    public abstract class AAutoLoadedScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance = null;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<T>(typeof(T).Name);
                    if (instance == null)
                    {
                        Debug.LogError($"Failed to load {typeof(T).Name} from Resources.");
                    }
                }
                return instance;
            }
        }
    }
}