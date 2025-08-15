using Common.Configurations;
using Common.DTOs;
using Common.Repositories;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace EvaluationService.Data
{
    public class FeedbackMongoRepository : IFeedbackRepository
    {
        private readonly IMongoCollection<FeedbackDto> _collection;

        public FeedbackMongoRepository(UserDbSettings settings)
        {
            // Registruje serializer za GUID kako bi se ispravno čuvali u MongoDB-u kao standardni UUID
            BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _collection = database.GetCollection<FeedbackDto>("Feedbacks");
        }

        /// <summary>
        /// Ubacuje novi feedback dokument u kolekciju.
        /// </summary>
        public async Task InsertAsync(FeedbackDto feedback)
        {
            await _collection.InsertOneAsync(feedback);
        }

        /// <summary>
        /// Vraća sve feedback dokumente iz kolekcije.
        /// </summary>
        public async Task<List<FeedbackDto>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        /// <summary>
        /// Vraća sve feedback dokumente za određenog studenta.
        /// </summary>
        public async Task<List<FeedbackDto>> GetFeedbacksByStudentIdAsync(Guid studentId)
        {
            var filter = Builders<FeedbackDto>.Filter.Eq(f => f.StudentId, studentId);
            return await _collection.Find(filter).ToListAsync();
        }

        /// <summary>
        /// Briše sve feedback-ove (korisno za testiranje ili reset).
        /// </summary>
        public async Task DeleteAllAsync()
        {
            await _collection.DeleteManyAsync(_ => true);
        }

        /// <summary>
        /// Ažurira jedan feedback dokument (po Id-u).
        /// </summary>
        public async Task UpdateAsync(Guid id, FeedbackDto updatedFeedback)
        {
            var filter = Builders<FeedbackDto>.Filter.Eq(f => f.WorkId, id);
            await _collection.ReplaceOneAsync(filter, updatedFeedback);
        }

        // EvaluationService/FeedbackMongoRepository.cs
        public async Task<int> DeleteManyByStudentIdAsync(Guid studentId)
        {
            var filter = Builders<FeedbackDto>.Filter.Eq(x => x.StudentId, studentId);
            var res = await _collection.DeleteManyAsync(filter);
            return (int)res.DeletedCount;
        }

        public async Task<bool> DeleteAsync(Guid workId)
        {
            var filter = Builders<FeedbackDto>.Filter.Eq(x => x.WorkId, workId);
            var res = await _collection.DeleteOneAsync(filter);
            return res.DeletedCount > 0;
        }

        public async Task<List<FeedbackDto>> GetByStudentIdAsync(Guid studentId)
         => await _collection.Find(f => f.StudentId == studentId).ToListAsync();
    }
}
