using Common.Configurations;
using Common.DTOs;
using Common.Enums;
using Common.Models;
using Common.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
public class SubmissionMongoRepository : ISubmissionRepository
{
    private readonly IMongoCollection<SubmittedWork> _collection;

    public SubmissionMongoRepository(UserDbSettings settings)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        var client = new MongoClient(settings.ConnectionString);
        var db = client.GetDatabase(settings.DatabaseName);
        _collection = db.GetCollection<SubmittedWork>("Submissions");
    }

    public async Task DeleteByIdAsync(Guid id)
    {
        await _collection.DeleteOneAsync(s => s.Id == id);
    }

    public async Task<List<SubmittedWork>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task UpdateStatusByIdAsync(Guid id, WorkStatus newStatus)
    {
        var filter = Builders<SubmittedWork>.Filter.Eq(w => w.Id, id);
        var update = Builders<SubmittedWork>.Update.Set(w => w.Status, newStatus);

        await _collection.UpdateOneAsync(filter, update);
    }


    public async Task InsertAsync(SubmittedWork work)
    {
        await _collection.InsertOneAsync(work);
    }

    public async Task<List<SubmittedWork>> GetByStudentIdAsync(Guid studentId)
    {
        // Pronalazi sve radove koje je poslao student sa datim ID-jem
        var filter = Builders<SubmittedWork>.Filter.Eq(w => w.StudentId, studentId);

        // Vraća listu radova iz Mongo kolekcije
        var result = await _collection.Find(filter).ToListAsync();

        return result;
    }

    public Task<int> CountByStudentSinceAsync(Guid studentId, DateTime sinceUtc)
    {
        var filter = Builders<SubmittedWork>.Filter.And(
            Builders<SubmittedWork>.Filter.Eq(x => x.StudentId, studentId),
            Builders<SubmittedWork>.Filter.Gte(x => x.SubmittedAt, sinceUtc)
        );
        return _collection.CountDocumentsAsync(filter).ContinueWith(t => (int)t.Result);
    }

    // SubmissionService/SubmissionMongoRepository.cs
    public async Task<int> DeleteManyByStudentIdAsync(Guid studentId)
    {
        var filter = Builders<SubmittedWork>.Filter.Eq(x => x.StudentId, studentId);
        var res = await _collection.DeleteManyAsync(filter);
        return (int)res.DeletedCount;
    }

}
