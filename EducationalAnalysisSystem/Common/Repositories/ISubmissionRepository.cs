using Common.DTOs;
using Common.Enums;
using Common.Models;

namespace Common.Repositories
{
    public interface ISubmissionRepository
    {
        Task InsertAsync(SubmittedWork work);
        Task<List<SubmittedWork>> GetAllAsync();
        Task<List<SubmittedWork>> GetByStudentIdAsync(Guid studentId);
        Task DeleteByIdAsync(Guid id);
        Task UpdateStatusByIdAsync(Guid id, WorkStatus newStatus);
        Task<int> CountByStudentSinceAsync(Guid studentId, DateTime sinceUtc);

    }
}
