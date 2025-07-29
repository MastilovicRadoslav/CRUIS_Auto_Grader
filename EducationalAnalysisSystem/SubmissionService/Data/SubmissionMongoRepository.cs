using Common.Configurations;
using Common.Enums;
using Common.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
public class SubmissionMongoRepository
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
        => await _collection.Find(w => w.StudentId == studentId).ToListAsync();
}
