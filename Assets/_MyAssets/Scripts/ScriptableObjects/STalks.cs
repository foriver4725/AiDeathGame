namespace MyScripts
{
    [CreateAssetMenu(fileName = "SData", menuName = "ScriptableObjects/SData")]
    public sealed class SData : AAutoLoadedScriptableObject<SData>
    {
        [SerializeField]
        private Data[] datas;

        public Data GetData(SceneId sceneId) => sceneId switch
        {
            SceneId.Stage_1 => datas[0],
            SceneId.Stage_2 => datas[1],
            SceneId.Stage_3 => datas[2],
            _ => null,
        };

        [Serializable]
        public sealed class Data
        {
            [SerializeField, TextArea(1, 1000)]
            private string question;
            public string Question => question;

            [SerializeField, TextArea(1, 1000)]
            private string[] answers;
            private ReadOnlyCollection<string> AnswersCached = null;
            public ReadOnlyCollection<string> Answers => AnswersCached ??= new(answers);
        }
    }
}
