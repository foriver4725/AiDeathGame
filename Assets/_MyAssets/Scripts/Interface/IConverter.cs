namespace MyScripts.Interface
{
    public interface IConverter
    {
        IModelData ViewToModel(IViewData input);
        IViewData ModelToView(IModelData input);
    }
}
