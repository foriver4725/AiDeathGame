namespace MyScripts
{
    public sealed class ResultButtonManager : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        private void Awake()
        {
            backButton.onClick.AddListener(() => SceneId.Select.LoadAsync());
        }
    }
}
