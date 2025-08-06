namespace MyScripts
{
    public sealed class SelectButtonManager : MonoBehaviour
    {
        [SerializeField, Header("最初の問題から順に")] private Button[] startButtons;

        private void Awake()
        {
            for (int i = 0; i < startButtons.Length; i++)
            {
                int index = i; // Capture the current index
                startButtons[i].onClick.AddListener(() => index.ToStageId().LoadAsync());
            }
        }
    }
}
