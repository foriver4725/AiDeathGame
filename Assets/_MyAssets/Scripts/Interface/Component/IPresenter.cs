namespace MyScripts.Interface
{
    public interface IPresenter : IConverter
    {
        IView View { get; }
        IModel Model { get; }

        void OnSendFromView(IViewData input);
        void OnSendFromModel(IModelData input);
    }
}
