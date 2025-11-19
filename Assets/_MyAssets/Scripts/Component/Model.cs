using MyScripts.Interface;

namespace MyScripts.Component
{
    public sealed class Model : MonoBehaviour, IModel
    {
        [SerializeField] private Presenter _presenter;
        public IPresenter Presenter => _presenter;

        public void Send(string prompt)
        {
            // TODO
        }

        public void Receive(string response)
        {
            // AIから受け取った時に実行される

            IModelData modelData = ConvertFromAiData(response);
            Presenter.OnSendFromModel(modelData);
        }

        public void OnSendFromPresenter(IModelData input)
        {
            // AIに送りたい時に実行される

            string prompt = ConvertToAiData(input);
            Send(prompt);
        }

        private static IModelData ConvertFromAiData(string input)
        {
            // TODO
            return null;
        }

        private static string ConvertToAiData(IModelData input)
        {
            // TODO
            return null;
        }
    }
}
