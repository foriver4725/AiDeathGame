namespace MyScripts.Interface
{
    public interface IView
    {
        IPresenter Presenter { get; }

        void OnSendFromPresenter(IViewData input);
    }
}
