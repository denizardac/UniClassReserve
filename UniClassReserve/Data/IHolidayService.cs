using System;
using System.Threading.Tasks;

namespace UniClassReserve.Data
{
    public interface IHolidayService
    {
        Task<bool> IsHolidayAsync(DateTime date);
    }
} 