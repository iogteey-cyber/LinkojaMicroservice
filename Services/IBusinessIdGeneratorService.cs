using System.Threading.Tasks;

namespace LinkojaMicroservice.Services
{
    public interface IBusinessIdGeneratorService
    {
        // Generates (or deterministically reuses the location codes to build) a permanent
        // Linkoja BusinessId in the format: LNK + 2-digit area code + 2-digit road code
        // + 2-digit street code + 4-digit sequence, e.g. "LNK0101001001".
        Task<string> GenerateBusinessId(string area, string road, string street);
    }
}
