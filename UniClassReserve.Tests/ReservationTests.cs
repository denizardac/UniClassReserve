using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using UniClassReserve.Data;

namespace UniClassReserve.Tests
{
    public class ReservationTests
    {
        [Fact]
        public void Reservation_Conflict_Detected()
        {
            // Arrange
            var reservations = new List<Reservation>
            {
                new Reservation
                {
                    Id = 1,
                    ClassroomId = 1,
                    TermId = 1,
                    DayOfWeek = 2, // Tuesday
                    StartTime = new DateTime(2025, 5, 6, 10, 0, 0),
                    EndTime = new DateTime(2025, 5, 6, 12, 0, 0),
                    Status = "Approved"
                }
            };
            var newReservation = new Reservation
            {
                Id = 2,
                ClassroomId = 1,
                TermId = 1,
                DayOfWeek = 2,
                StartTime = new DateTime(2025, 5, 6, 11, 0, 0),
                EndTime = new DateTime(2025, 5, 6, 13, 0, 0),
                Status = "Pending"
            };
            // Act
            bool isConflict = reservations.Any(r =>
                r.ClassroomId == newReservation.ClassroomId &&
                r.TermId == newReservation.TermId &&
                r.DayOfWeek == newReservation.DayOfWeek &&
                r.Status == "Approved" &&
                r.StartTime < newReservation.EndTime &&
                r.EndTime > newReservation.StartTime
            );
            // Assert
            Assert.True(isConflict);
        }

        [Fact]
        public void Reservation_No_Conflict_When_Times_Do_Not_Overlap()
        {
            // Arrange
            var reservations = new List<Reservation>
            {
                new Reservation
                {
                    Id = 1,
                    ClassroomId = 1,
                    TermId = 1,
                    DayOfWeek = 2,
                    StartTime = new DateTime(2025, 5, 6, 8, 0, 0),
                    EndTime = new DateTime(2025, 5, 6, 10, 0, 0),
                    Status = "Approved"
                }
            };
            var newReservation = new Reservation
            {
                Id = 2,
                ClassroomId = 1,
                TermId = 1,
                DayOfWeek = 2,
                StartTime = new DateTime(2025, 5, 6, 10, 0, 0),
                EndTime = new DateTime(2025, 5, 6, 12, 0, 0),
                Status = "Pending"
            };
            // Act
            bool isConflict = reservations.Any(r =>
                r.ClassroomId == newReservation.ClassroomId &&
                r.TermId == newReservation.TermId &&
                r.DayOfWeek == newReservation.DayOfWeek &&
                r.Status == "Approved" &&
                r.StartTime < newReservation.EndTime &&
                r.EndTime > newReservation.StartTime
            );
            // Assert
            Assert.False(isConflict);
        }

        [Fact]
        public void Reservation_Fails_On_StaticHoliday()
        {
            // Arrange
            var holidayService = new HolidayServiceMock();
            var date = new DateTime(2025, 5, 1); // 1 Mayıs
            // Act
            bool isHoliday = holidayService.IsHoliday(date);
            // Assert
            Assert.True(isHoliday);
        }
    }

    // Basit bir mock holiday service
    public class HolidayServiceMock
    {
        public bool IsHoliday(DateTime date)
        {
            var staticHolidays = new List<DateTime>
            {
                new DateTime(date.Year, 1, 1),
                new DateTime(date.Year, 5, 1),
                new DateTime(date.Year, 10, 29)
            };
            return staticHolidays.Any(d => d.Date == date.Date);
        }
    }
} 