using Library;

namespace WinLibrary
{
    public interface IGameSystem
    {
        IWording wording { get; }

        FormGame formGame { get; }
    }
}
