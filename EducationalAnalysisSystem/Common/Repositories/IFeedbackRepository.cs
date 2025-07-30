using Common.DTOs;

namespace Common.Repositories
{
    public interface IFeedbackRepository
    {
        Task InsertAsync(FeedbackDto feedback);
        Task<List<FeedbackDto>> GetAllAsync();
        Task<List<FeedbackDto>> GetFeedbacksByStudentIdAsync(Guid studentId);
        Task DeleteAllAsync();
        Task UpdateAsync(Guid id, FeedbackDto updatedFeedback);
    }
}
