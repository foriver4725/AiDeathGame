namespace MyScripts.Interface
{
    public interface IModel : IAiPresenter
    {
        IPresenter Presenter { get; }

        void OnSendFromPresenter(IModelData input);
    }
}
