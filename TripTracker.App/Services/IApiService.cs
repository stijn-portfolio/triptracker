namespace TripTracker.App.Services
{
    // Generieke interface voor API communicatie
    // Zoals in SafariSnap (Les 3 - DataServices)
    public interface IApiService<T>
    {
        Task<T> GetAsync(int id);
        Task<List<T>> GetAllAsync();
        Task PostAsync(T data);
        Task PutAsync(int id, T data);
        Task DeleteAsync(int id);
    }
}
