namespace MyScripts
{
    public enum SceneId : byte
    {
        Select,
        Right,
        Wrong,
        Stage_1,
        Stage_2,
        Stage_3,
    }

    public static class SceneManager
    {
        private static readonly Dictionary<SceneId, string> sceneNames = new()
        {
            { SceneId.Select, "Select" },
            { SceneId.Right, "Right" },
            { SceneId.Wrong, "Wrong" },
            { SceneId.Stage_1, "Stage_1" },
            { SceneId.Stage_2, "Stage_2" },
            { SceneId.Stage_3, "Stage_3" },
        };

        public static SceneId ToStageId(this int sceneIndex) => sceneIndex switch
        {
            0 => SceneId.Stage_1,
            1 => SceneId.Stage_2,
            2 => SceneId.Stage_3,
            _ => throw new ArgumentOutOfRangeException(nameof(sceneIndex), "Invalid scene index")
        };

        public static SceneId Now => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name switch
        {
            "Select" => SceneId.Select,
            "Right" => SceneId.Right,
            "Wrong" => SceneId.Wrong,
            "Stage_1" => SceneId.Stage_1,
            "Stage_2" => SceneId.Stage_2,
            "Stage_3" => SceneId.Stage_3,
            _ => throw new InvalidOperationException("Current scene is not recognized.")
        };

        public static void LoadAsync(this SceneId sceneId)
        {
            if (!sceneNames.TryGetValue(sceneId, out string sceneName))
            {
                Debug.LogError($"SceneId {sceneId} does not have a corresponding scene name.");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
