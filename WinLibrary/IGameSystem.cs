using Library;

namespace WinLibrary
{
    public interface IGameSystem<TWording> where TWording : IWording
    {
        TWording wording { get; }

        FormGame formGame { get; }
    }
}
