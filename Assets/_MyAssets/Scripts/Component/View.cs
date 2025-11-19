using MyScripts.Interface;

namespace MyScripts.Component
{
    public sealed class View : MonoBehaviour, IView
    {
        [SerializeField] private Presenter _presenter;
        public IPresenter Presenter => _presenter;

        public void OnSendFromPresenter(IViewData input)
        {
            // 返答を受け取った時に実行される
            // TODO
        }

        private IViewData GetViewData()
        {
            // TODO
            return null;
        }

        private void OnSendToPresenter()
        {
            // プレイヤーの操作を送りたい時に実行する

            IViewData viewData = GetViewData();
            Presenter.OnSendFromView(viewData);
        }
    }
}
