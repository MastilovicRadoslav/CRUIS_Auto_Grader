using Common.Configurations;
using Common.Models;
using Common.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace UserService.Data
{
    public class UserMongoRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserMongoRepository(UserDbSettings settings)
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _users = database.GetCollection<User>(settings.CollectionName);
        }

        public async Task InsertUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task DeleteByIdAsync(Guid id)
        {
            await _users.DeleteOneAsync(u => u.Id == id);
        }

        public async Task UpdateAsync(User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            await _users.ReplaceOneAsync(filter, user);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _users.Find(Builders<User>.Filter.Empty).ToListAsync();
        }


    }
}
