using Newtonsoft.Json;
using System.Net.Http.Json;

namespace TripTracker.App.Services
{
    /// <summary>
    /// Abstract base class voor API communicatie.
    /// Zoals in SafariSnap (Les 3 - DataServices).
    /// </summary>
    public abstract class ApiService<T> : IApiService<T>
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════

        // Base URL voor de API (Ngrok voor Android, localhost voor Windows)
        protected static readonly string BASE_URL = "https://mao-subtympanitic-pauletta.ngrok-free.dev/api";

        // Statische HttpClient met timeout (hergebruik voor performance)
        protected static readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(60) };

        // Abstract property - elke specifieke dataservice definieert zijn eigen endpoint
        protected abstract string EndPoint { get; }

        // ═══════════════════════════════════════════════════════════
        // HTTP METHODS
        // ═══════════════════════════════════════════════════════════

        public virtual async Task<T> GetAsync(int id)
        {
            var response = await client.GetAsync($"{BASE_URL}/{EndPoint}/{id}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonData)!;
            }
            throw new Exception($"GetAsync request failed with status code {response.StatusCode}");
        }

        public virtual async Task<List<T>> GetAllAsync()
        {
            var response = await client.GetAsync($"{BASE_URL}/{EndPoint}");
            if (response.IsSuccessStatusCode)
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<T>>(jsonData)!;
            }
            throw new Exception($"GetAllAsync request failed with status code {response.StatusCode}");
        }

        public virtual async Task PostAsync(T data)
        {
            var response = await client.PostAsJsonAsync($"{BASE_URL}/{EndPoint}", data);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception($"PostAsync request failed with status code {response.StatusCode}");
            }
        }

        public virtual async Task PutAsync(int id, T data)
        {
            var response = await client.PutAsJsonAsync($"{BASE_URL}/{EndPoint}/{id}", data);
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception($"PutAsync request failed with status code {response.StatusCode}");
            }
        }

        public virtual async Task DeleteAsync(int id)
        {
            var response = await client.DeleteAsync($"{BASE_URL}/{EndPoint}/{id}");
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception($"DeleteAsync request failed with status code {response.StatusCode}");
            }
        }
    }
}
