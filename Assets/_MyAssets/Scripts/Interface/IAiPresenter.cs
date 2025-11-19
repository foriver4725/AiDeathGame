namespace MyScripts.Interface
{
    public interface IAiPresenter
    {
        void Send(string prompt);
        void Receive(string response);
    }
}
