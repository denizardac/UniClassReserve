using System.Net.Http;
using System.Text.Json;

namespace UniClassReserve.Data
{
    public class HolidayService : IHolidayService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        // Nager.Date API: https://date.nager.at/swagger/index.html
        public async Task<bool> IsHolidayAsync(DateTime date)
        {
            return await IsHolidayAsync(date, "TR");
        }
        public async Task<bool> IsHolidayAsync(DateTime date, string countryCode)
        {
            var year = date.Year;
            var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) throw new Exception("API error");
                var json = await response.Content.ReadAsStringAsync();
                var holidays = JsonSerializer.Deserialize<List<HolidayDto>>(json);
                return holidays?.Any(h => DateTime.Parse(h.date).Date == date.Date) ?? false;
            }
            catch
            {
                // Fallback: Yerel sabit tatil listesi (Türkiye için)
                var staticHolidays = new List<DateTime>
                {
                    new DateTime(year, 1, 1),   // Yılbaşı
                    new DateTime(year, 4, 23),  // 23 Nisan
                    new DateTime(year, 5, 1),   // 1 Mayıs
                    new DateTime(year, 5, 19),  // 19 Mayıs
                    new DateTime(year, 7, 15),  // 15 Temmuz
                    new DateTime(year, 8, 30),  // 30 Ağustos
                    new DateTime(year, 10, 29), // 29 Ekim
                };
                return staticHolidays.Any(d => d.Date == date.Date);
            }
        }
        private class HolidayDto
        {
            public string date { get; set; } = string.Empty;
            public string localName { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
        }
    }
} 