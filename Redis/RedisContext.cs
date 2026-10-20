using StackExchange.Redis;

namespace url_shortener.Redis;

public class RedisContext
{
    private readonly ConnectionMultiplexer _connection;

    public RedisContext(string connectionString)
    {
        _connection = ConnectionMultiplexer.Connect(connectionString);
    }

    public IDatabase Database => _connection.GetDatabase();

    public bool IsConnected
    {
        get
        {
            try
            {
                return _connection.IsConnected;
            }
            catch
            {
                return false;
            }
        }
    }
}
