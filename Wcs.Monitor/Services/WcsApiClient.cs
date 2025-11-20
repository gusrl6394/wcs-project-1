using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Wcs.Monitor.Models;

namespace Wcs.Monitor.Services
{
    public class WcsApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        // 실제 API 라우팅에 맞게 수정
        private const string StatusEndpoint = "/api/equipment-status";
        private const string TemperatureEndpoint = "/api/temperature";

        public WcsApiClient(string baseAddress)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseAddress)
            };
        }

        public async Task<IReadOnlyList<EquipmentStatusDto>> GetEquipmentStatusesAsync(
            CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetFromJsonAsync<List<EquipmentStatusDto>>(
                StatusEndpoint, cancellationToken);
            return result ?? new List<EquipmentStatusDto>();
        }

        public async Task<IReadOnlyList<TemperatureReadingDto>> GetTemperatureReadingsAsync(
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await _httpClient.GetFromJsonAsync<List<TemperatureReadingDto>>(
                $"{TemperatureEndpoint}?count={count}", cancellationToken);
            return result ?? new List<TemperatureReadingDto>();
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
