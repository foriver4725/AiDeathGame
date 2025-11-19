using MyScripts.Interface;

namespace MyScripts.Component
{
    public sealed class Presenter : MonoBehaviour, IPresenter
    {
        [SerializeField] private View _view;
        [SerializeField] private Model _model;

        public IView View => _view;
        public IModel Model => _model;

        public IModelData ViewToModel(IViewData input) { return default; }
        public IViewData ModelToView(IModelData input) { return default; }

        public void OnSendFromView(IViewData input)
        {
            var modelData = ViewToModel(input);
            _model.OnSendFromPresenter(modelData);
        }

        public void OnSendFromModel(IModelData input)
        {
            var viewData = ModelToView(input);
            _view.OnSendFromPresenter(viewData);
        }
    }
}
