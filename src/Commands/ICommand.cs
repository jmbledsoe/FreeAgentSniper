using System.Threading.Tasks;

namespace FreeAgentSniper
{
    public interface ICommand
    {
        Task<int> Run();
    }
}