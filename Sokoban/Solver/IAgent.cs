using System.Threading.Tasks;

namespace Sokoban.Solver
{
    public interface IAgent
    {
        Task SolveAsync();
    }
}
