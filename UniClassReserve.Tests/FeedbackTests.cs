using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using UniClassReserve.Data;

namespace UniClassReserve.Tests
{
    public class FeedbackTests
    {
        [Fact]
        public void Can_Leave_Feedback_If_Reservation_Approved()
        {
            // Arrange
            var userId = "user1";
            var classroomId = 1;
            var termId = 1;
            var reservations = new List<Reservation>
            {
                new Reservation { UserId = userId, ClassroomId = classroomId, TermId = termId, Status = "Approved" }
            };
            var feedbacks = new List<Feedback>();
            // Act
            bool hasReservation = reservations.Any(r => r.UserId == userId && r.ClassroomId == classroomId && r.TermId == termId && r.Status == "Approved");
            bool alreadyLeft = feedbacks.Any(f => f.UserId == userId && f.ClassroomId == classroomId && f.TermId == termId);
            bool canLeave = hasReservation && !alreadyLeft;
            // Assert
            Assert.True(canLeave);
        }

        [Fact]
        public void Cannot_Leave_Feedback_Twice()
        {
            // Arrange
            var userId = "user1";
            var classroomId = 1;
            var termId = 1;
            var reservations = new List<Reservation>
            {
                new Reservation { UserId = userId, ClassroomId = classroomId, TermId = termId, Status = "Approved" }
            };
            var feedbacks = new List<Feedback>
            {
                new Feedback { UserId = userId, ClassroomId = classroomId, TermId = termId, Rating = 5, Comment = "Great!" }
            };
            // Act
            bool hasReservation = reservations.Any(r => r.UserId == userId && r.ClassroomId == classroomId && r.TermId == termId && r.Status == "Approved");
            bool alreadyLeft = feedbacks.Any(f => f.UserId == userId && f.ClassroomId == classroomId && f.TermId == termId);
            bool canLeave = hasReservation && !alreadyLeft;
            // Assert
            Assert.False(canLeave);
        }

        [Fact]
        public void Cannot_Leave_Feedback_Without_Reservation()
        {
            // Arrange
            var userId = "user1";
            var classroomId = 1;
            var termId = 1;
            var reservations = new List<Reservation>(); // No reservation
            var feedbacks = new List<Feedback>();
            // Act
            bool hasReservation = reservations.Any(r => r.UserId == userId && r.ClassroomId == classroomId && r.TermId == termId && r.Status == "Approved");
            bool alreadyLeft = feedbacks.Any(f => f.UserId == userId && f.ClassroomId == classroomId && f.TermId == termId);
            bool canLeave = hasReservation && !alreadyLeft;
            // Assert
            Assert.False(canLeave);
        }
    }
} 